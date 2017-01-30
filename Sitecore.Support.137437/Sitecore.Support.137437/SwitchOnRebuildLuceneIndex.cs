using System;
using System.IO;
using System.Linq;
using System.Threading;
using Lucene.Net.Index;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.LuceneProvider.Sharding;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.Sharding;
using Sitecore.Diagnostics;
using Sitecore.ContentSearch.LuceneProvider;
using Sitecore.Support.ContentSearch.Maintenance;
using System.Reflection;
using System.Collections.Generic;

namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    /// <summary>
    /// The index that switches directories on full rebuild
    /// </summary>
    public class SwitchOnRebuildLuceneIndex : LuceneIndex
    {
        /// <summary>The switch on rebuild shard factory</summary>
        private readonly IShardFactory switchOnRebuildShardFactory = new LuceneSwitchOnRebuildShardFactory();

        /// <summary>The full rebuild lock object</summary>
        private readonly object fullRebuildLockObject = new object();

        /// <summary>The mode</summary>
        private SwitchOnRebuildMode mode = SwitchOnRebuildMode.Primary;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchOnRebuildLuceneIndex" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="propertyStore">The property store.</param>
        public SwitchOnRebuildLuceneIndex(string name, string folder, IIndexPropertyStore propertyStore)
            : base(name, folder, propertyStore)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchOnRebuildLuceneIndex"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        protected SwitchOnRebuildLuceneIndex(string name)
            : base(name)
        {
        }

        /// <summary>Gets the shard factory.</summary>
        /// <value>The shard factory.</value>
        public override IShardFactory ShardFactory
        {
            get { return switchOnRebuildShardFactory; }
        }

        /// <summary>Does the reset.</summary>
        /// <param name="context">The context.</param>
        protected override void DoReset(Sitecore.ContentSearch.IProviderUpdateContext context)
        {
            var rebuildContext = context as LuceneFullRebuildContext;
            if (rebuildContext != null)
            {
                rebuildContext.Reset();
                rebuildContext.ReinitializeWriters();
            }
            else
                base.DoReset(context);
        }

        /// <summary>Rebuilds this index</summary>
        /// <param name="context">The context.</param>
        /// <param name="indexingOptions">The indexing options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected override void DoRebuild(Sitecore.ContentSearch.IProviderUpdateContext context, Sitecore.ContentSearch.IndexingOptions indexingOptions, CancellationToken cancellationToken)
        {
            base.DoRebuild(context, indexingOptions, cancellationToken);
            this.SwitchDirectories();
        }


        protected override void InitializeShards()
        {
            CrawlingLog.Log.Debug(string.Format("[Index={0}] Creating primary and secondary directories", this.Name));

            base.InitializeShards();

            var lastReadUpdateDir = this.GetPropertyStore(IndexProperties.ReadUpdateDirectory);
            //var lastFullRebuildDir = this.PropertyStore.Get(IndexProperties.FullRebuildDirectory);

            bool primaryDirectorySettingRead = false;

            if (!string.IsNullOrEmpty(lastReadUpdateDir))
            {
                CrawlingLog.Log.Debug(string.Format("[Index={0}] Resolving directories from index property store for index '{0}'", this.Name));

                if (Enum.TryParse(lastReadUpdateDir, out mode))
                    primaryDirectorySettingRead = true;
            }

            if (!primaryDirectorySettingRead)
            {
                CrawlingLog.Log.Debug(string.Format("[Index={0}] Resolving directories by last time modified.", this.Name));

                var latestPrimaryModifiedDate = long.MinValue;
                var latestSecondaryModifiedDate = long.MinValue;

                BindingFlags flags = BindingFlags.Public |
                             BindingFlags.NonPublic |
                             BindingFlags.Static |
                             BindingFlags.Instance |
                             BindingFlags.DeclaredOnly;
                Dictionary<int, LuceneShard> newShards = (Dictionary<int, LuceneShard>)this.GetType().BaseType.GetField("shards", flags).GetValue(this);

                foreach (var shard in newShards.Values.Cast<LuceneSwitchOnRebuildShard>())
                {
                    try
                    {
                        var primModified = IndexReader.LastModified(shard.PrimaryDirectory);
                        var secModified = IndexReader.LastModified(shard.SecondaryDirectory);

                        CrawlingLog.Log.Debug(string.Format("[Index={0}, Shard={1}] Primary directory last modified = '{2}'.", this.Name, shard, primModified));
                        CrawlingLog.Log.Debug(string.Format("[Index={0}, Shard={1}] Secondary directory last modified = '{2}'.", this.Name, shard, secModified));

                        if (primModified > latestPrimaryModifiedDate)
                            latestPrimaryModifiedDate = primModified;

                        if (secModified > latestSecondaryModifiedDate)
                            latestSecondaryModifiedDate = secModified;
                    }
                    catch (FileNotFoundException ex)
                    {
                        if (!ex.Message.StartsWith("no segments"))
                            throw;

                        latestPrimaryModifiedDate = latestSecondaryModifiedDate = 0;
                    }
                }


                this.mode = latestPrimaryModifiedDate >= latestSecondaryModifiedDate ? SwitchOnRebuildMode.Primary : SwitchOnRebuildMode.Secondary;
            }

            SwitchDirectories(this.mode);
        }

        /// <summary>
        /// The switch directories.
        /// </summary>
        protected void SwitchDirectories()
        {
            var newMode = this.mode == SwitchOnRebuildMode.Primary ? SwitchOnRebuildMode.Secondary : SwitchOnRebuildMode.Primary;
            SwitchDirectories(newMode);
        }

        private void SwitchDirectories(SwitchOnRebuildMode newMode)
        {
            lock (this)
            {
                foreach (LuceneSwitchOnRebuildShard shard in this.Shards)
                {
                    shard.SwitchDirectories(newMode);
                    Assert.IsTrue(shard.Mode == newMode, "[Index={0}, Shard={1}] SwitchOnRebuildShard not set in {2} mode. Shard mode: {3}", this.Name, shard, mode, shard.Mode);
                }

                this.mode = newMode;

                this.SetPropertyStore(IndexProperties.ReadUpdateDirectory, this.mode.ToString());
                this.SetPropertyStore(IndexProperties.FullRebuildDirectory, (this.mode == SwitchOnRebuildMode.Primary ? SwitchOnRebuildMode.Secondary : SwitchOnRebuildMode.Primary).ToString());
            }
        }

        /// <summary>
        /// The create full rebuild context.
        /// </summary>
        /// <returns>
        /// The <see cref="IProviderUpdateContext"/>.
        /// </returns>
        protected override Sitecore.ContentSearch.IProviderUpdateContext CreateFullRebuildContext()
        {
            this.EnsureInitialized();

            var executor = (Sitecore.ContentSearch.ICommitPolicyExecutor)this.CommitPolicyExecutor.Clone();
            executor.Initialize(this);

            return new LuceneFullRebuildContext(this, executor);
        }

        /// <summary>Gets the full rebuild lock object.</summary>
        /// <returns>The lock object used</returns>
        protected override object GetFullRebuildLockObject()
        {
            return fullRebuildLockObject;
        }

        /// <summary>
        /// Get from property store
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual string GetPropertyStore(string key)
        {
            if (!typeof(IIndexPropertyStoreEx).IsAssignableFrom(this.PropertyStore.GetType()))
            {
                return this.PropertyStore.Get(key);
            }

            IIndexPropertyStoreEx propertyStore = this.PropertyStore as IIndexPropertyStoreEx;

            return propertyStore.Get(key, null);
        }

        /// <summary>
        /// Set value to property store
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void SetPropertyStore(string key, string value)
        {
            if (!typeof(IIndexPropertyStoreEx).IsAssignableFrom(this.PropertyStore.GetType()))
            {
                this.PropertyStore.Set(key, value);
                return;
            }

            IIndexPropertyStoreEx propertyStore = this.PropertyStore as IIndexPropertyStoreEx;
            propertyStore.Set(key, value, null);
        }
    }
}