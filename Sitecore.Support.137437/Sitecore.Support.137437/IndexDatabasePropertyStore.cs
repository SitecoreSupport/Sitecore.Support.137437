using Sitecore.Abstractions;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data;
using Sitecore.Diagnostics;
using System;
using System.Runtime.CompilerServices;

namespace Sitecore.Support.ContentSearch.Maintenance
{
    public class IndexDatabasePropertyStore : IIndexPropertyStore
    {
        private SafeDictionary<string, string> defaultValues;
        private readonly IEvent events;
        private readonly IFactory factory;
        private Sitecore.Data.Database innerStore;

        public IndexDatabasePropertyStore() : this(ContentSearchManager.Locator.GetInstance<IEvent>(), ContentSearchManager.Locator.GetInstance<IFactory>())
        {
        }

        internal IndexDatabasePropertyStore(IEvent events, IFactory factory)
        {
            this.defaultValues = new SafeDictionary<string, string>();
            this.events = events;
            this.factory = factory;
            this.SuppressPropertyChangedEvents = true;
        }

        public void Add(string key, string value)
        {
            string str = this.MasterKey + "_" + key;
            string str2 = this.InnerStore.Properties[str];
            using (new PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties[str] = string.IsNullOrEmpty(str2) ? value : (str2 + "," + value);
            }
            object[] parameters = new object[] { str, value };
            (this.events ?? ContentSearchManager.Locator.GetInstance<IEvent>()).RaiseEvent("indexing:propertyadd", parameters);
        }

        public void Clear(string prefix)
        {
            using (new PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties.RemovePrefix(this.MasterKey + "_" + prefix);
            }
        }

        public void ClearAll()
        {
            using (new PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties.RemovePrefix(this.MasterKey);
            }
        }

        public string Get(string key)
        {
            string str2;
            string str = this.MasterKey + "_" + key;
            if (this.defaultValues.ContainsKey(str))
            {
                str2 = this.defaultValues[str];
            }
            else
            {
                str2 = this.InnerStore.Properties[str];
                if (string.IsNullOrEmpty(str2))
                {
                    this.defaultValues[str] = str2;
                }
            }
              return str2;
        }

        public void Set(string key, string value)
        {
            string str = this.MasterKey + "_" + key;
            using (new PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties[str] = value;
                if (this.defaultValues.ContainsKey(str))
                {
                    this.defaultValues.Remove(str);
                }
            }
         }

        public string Database { get; set; }

        protected Sitecore.Data.Database InnerStore
        {
            get
            {
                if (this.innerStore == null)
                {
                    Assert.IsNotNull(this.Database, "Database is not set");
                    Assert.IsTrue((this.factory ?? ContentSearchManager.Locator.GetInstance<IFactory>()).GetDatabase(this.Database) != null, "Cannot find the inner store database");
                    this.innerStore = (this.factory ?? ContentSearchManager.Locator.GetInstance<IFactory>()).GetDatabase(this.Database);
                }
                return this.innerStore;
            }
        }

        public string Key { get; set; }

        public string MasterKey =>
            $"{this.Key}_{Settings.InstanceName}";

        public bool SuppressPropertyChangedEvents { get; set; }
    }
}
