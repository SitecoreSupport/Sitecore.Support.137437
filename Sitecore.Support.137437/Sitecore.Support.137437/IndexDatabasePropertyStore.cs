using System;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Abstractions;
using Sitecore.Collections;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Configuration;
using Sitecore.ContentSearch;

namespace Sitecore.Support.ContentSearch.Maintenance
{
    /// <summary>
    /// The index database property store.
    /// </summary>
    public class IndexDatabasePropertyStore : IIndexPropertyStore, IIndexPropertyStoreEx
    {
        /// <summary>
        /// The inner store.
        /// </summary>
        private Database innerStore;

        private readonly IEvent events;

        private readonly IFactory factory;

        private SafeDictionary<string, string> defaultValues = new SafeDictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexDatabasePropertyStore"/> class.
        /// </summary>
        public IndexDatabasePropertyStore()
            : this(ContentSearchManager.Locator.GetInstance<IEvent>(), ContentSearchManager.Locator.GetInstance<IFactory>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexDatabasePropertyStore"/> class.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="factory">The factory. </param>
        internal IndexDatabasePropertyStore(IEvent events, IFactory factory)
        {
            this.events = events;
            this.factory = factory;
            this.SuppressPropertyChangedEvents = true;
        }

        /// <summary>
        /// Gets the inner store.
        /// </summary>
        protected Database InnerStore
        {
            get
            {
                if (this.innerStore != null)
                {
                    return this.innerStore;
                }

                Assert.IsNotNull(this.Database, "Database is not set");
                Assert.IsTrue((this.factory ?? ContentSearchManager.Locator.GetInstance<IFactory>()).GetDatabase(this.Database) != null, "Cannot find the inner store database");
                this.innerStore = (this.factory ?? ContentSearchManager.Locator.GetInstance<IFactory>()).GetDatabase(this.Database);
                return this.innerStore;
            }
        }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Suppress PropertyChanged events
        /// </summary>
        public bool SuppressPropertyChangedEvents { get; set; }

        /// <summary>
        /// Gets the master key.
        /// </summary>
        public string MasterKey
        {
            get
            {
                return string.Format("{0}_{1}", this.Key, Settings.InstanceName);
            }
        }

        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// The add.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public void Add(string key, string value)
        {
            this.Add(key, value, (propertyKey, propertyValue) => { (this.events ?? ContentSearchManager.Locator.GetInstance<IEvent>()).RaiseEvent("indexing:propertyadd", propertyKey, propertyValue); });
        }

        /// <summary>
        /// The set.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public void Set(string key, string value)
        {
            this.Set(key, value, (propertyKey, propertyValue) => { (this.events ?? ContentSearchManager.Locator.GetInstance<IEvent>()).RaiseEvent("indexing:propertyset", propertyKey, propertyValue); });
        }

        /// <summary>
        /// The get.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string Get(string key)
        {
            return this.Get(key, ((propertyKey, propertyValue) => { (this.events ?? ContentSearchManager.Locator.GetInstance<IEvent>()).RaiseEvent("indexing:propertyget", propertyKey, propertyValue); }));
        }

        /// <summary>
        /// The clear all.
        /// </summary>
        public void ClearAll()
        {
            using (new PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties.RemovePrefix(this.MasterKey);
            }
        }

        /// <summary>
        /// The clear.
        /// </summary>
        /// <param name="prefix">
        /// The prefix.
        /// </param>
        public void Clear(string prefix)
        {
            using (new PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties.RemovePrefix(this.MasterKey + "_" + prefix);
            }
        }

        /// <summary>
        /// Add into database property Store
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="postAction"></param>
        public void Add(string key, string value, Action<string, string> postAction)
        {
            var propertyKey = this.MasterKey + "_" + key;
            var existing = this.InnerStore.Properties[propertyKey];
            using (new PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties[propertyKey] = string.IsNullOrEmpty(existing) ? value : existing + "," + value;
            }

            if (postAction != null)
            {
                postAction.Invoke(propertyKey, value);
            }
        }

        /// <summary>
        /// Set value into database property store
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="postAction"></param>
        public void Set(string key, string value, Action<string, string> postAction)
        {
            var propertyKey = this.MasterKey + "_" + key;
            using (new PropertyChangedEventDisabler(this.SuppressPropertyChangedEvents))
            {
                this.InnerStore.Properties[propertyKey] = value;
                if (this.defaultValues.ContainsKey(propertyKey)) this.defaultValues.Remove(propertyKey);
            }

            if (postAction != null)
            {
                postAction.Invoke(propertyKey, value);
            }
        }

        public string Get(string key, Action<string, string> postAction)
        {
            var propertyKey = this.MasterKey + "_" + key;
            string value;
            if (this.defaultValues.ContainsKey(propertyKey))
                value = this.defaultValues[propertyKey];
            else
            {
                value = this.InnerStore.Properties[propertyKey];
                if (string.IsNullOrEmpty(value)) this.defaultValues[propertyKey] = value;
            }

            if (postAction != null)
            {
                postAction.Invoke(propertyKey, value);
            }

            return value;
        }
    }

    /// <summary>
    /// Property changed event disabler.
    /// </summary>
    internal class PropertyChangedEventDisabler : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangedEventDisabler"/> class. 
        /// Property changed event disabler.
        /// </summary>
        public PropertyChangedEventDisabler(bool state)
        {
            EventDisablerState disablerState = state ? EventDisablerState.Enabled : EventDisablerState.Disabled;
            EventDisabler.Enter(disablerState);
        }

        /// <summary>
        /// Dispose event disabler.
        /// </summary>
        public void Dispose()
        {
            EventDisabler.Exit();
        }
    }
}