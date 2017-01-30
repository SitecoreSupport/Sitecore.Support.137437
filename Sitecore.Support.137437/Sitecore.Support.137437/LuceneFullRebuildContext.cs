using Sitecore.ContentSearch;
using Sitecore.ContentSearch.LuceneProvider;
using System;

namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    public class LuceneFullRebuildContext : LuceneUpdateContext
    {
        public LuceneFullRebuildContext(SwitchOnRebuildLuceneIndex index, ICommitPolicyExecutor commitPolicyExecutor)
            : base(index, commitPolicyExecutor)
        {
        }

        public void ReinitializeWriters()
        {
            base.InitializeWriters();
        }
    }
}