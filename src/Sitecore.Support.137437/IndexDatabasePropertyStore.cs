using Sitecore.Abstractions;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Reflection;
using System;
using System.Runtime.CompilerServices;
using Sitecore.Support.ContentSearch.Maintenance;

namespace Sitecore.Support.ContentSearch.Maintenance
{
    public class IndexDatabasePropertyStore : IIndexPropertyStore
    {
        private readonly IEvent events;
        private readonly Abstractions.IFactory factory;
        private Sitecore.Data.Database innerStore;

        public IndexDatabasePropertyStore() : this(ContentSearchManager.Locator.GetInstance<IEvent>(), ContentSearchManager.Locator.GetInstance<Abstractions.IFactory>())
        {
        }

        internal IndexDatabasePropertyStore(IEvent events, Abstractions.IFactory factory)
        {
            this.events = events;
            this.factory = factory;
            this.SuppressPropertyChangedEvents = true;
        }

        public void Add(string key, string value)
        {
            string str = this.MasterKey + "_" + key;
            string str2 = this.InnerStore.Properties[str];
            using (new Sitecore.Support.ContentSearch.Maintenance.PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties[str] = string.IsNullOrEmpty(str2) ? value : (str2 + "," + value);
            }
            (this.events ?? ContentSearchManager.Locator.GetInstance<IEvent>()).RaiseEvent("indexing:propertyadd", new object[] { str, value });
        }

        public void Clear(string prefix)
        {
            using (new Sitecore.Support.ContentSearch.Maintenance.PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties.RemovePrefix(this.MasterKey + "_" + prefix);
            }
        }

        public void ClearAll()
        {
            using (new Sitecore.Support.ContentSearch.Maintenance.PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties.RemovePrefix(this.MasterKey);
            }
        }

        public string Get(string key)
        {
            string str = this.MasterKey + "_" + key;
            string str2 = this.InnerStore.Properties[str];

            //(this.events ?? ContentSearchManager.Locator.GetInstance<IEvent>()).RaiseEvent("indexing:propertyget", new object[] { str, str2 });
            return str2;
        }

        public void Set(string key, string value)
        {
            string str = this.MasterKey + "_" + key;
            using (new Sitecore.Support.ContentSearch.Maintenance.PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties[str] = value;
            }
            //(this.events ?? ContentSearchManager.Locator.GetInstance<IEvent>()).RaiseEvent("indexing:propertyset", new object[] { str, value });
        }

        public string Database { get; set; }

        protected Sitecore.Data.Database InnerStore
        {
            get
            {
                if (this.innerStore == null)
                {
                    Assert.IsNotNull(this.Database, "Database is not set");
                    Assert.IsTrue((this.factory ?? ContentSearchManager.Locator.GetInstance<Abstractions.IFactory>()).GetDatabase(this.Database) != null, "Cannot find the inner store database");
                    this.innerStore = (this.factory ?? ContentSearchManager.Locator.GetInstance<Abstractions.IFactory>()).GetDatabase(this.Database);
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
