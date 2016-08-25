using System.Collections.Generic;
using DBBranchManager.Caching;
using DBBranchManager.Constants;
using DBBranchManager.Entities;
using DBBranchManager.Entities.Config;
using DBBranchManager.Logging;
using Mono.Options;

namespace DBBranchManager.Commands
{
    internal class DbbmGarbageCollectCommand : DbbmCommand
    {
        public override string Description
        {
            get { return "Free space used by outdated cache."; }
        }

        public override void Run(ApplicationContext appContext, IEnumerable<string> args)
        {
            var dryRun = false;
            var p = new OptionSet
            {
                { "n|dry-run", "Don't actually run actions, just print what would be done and exit.", v => dryRun = v != null }
            };

            var extra = Parse(p, args,
                CommandConstants.GarbageCollect,
                "[OPTIONS]+");
            if (extra == null)
                return;

            RunCore(new GarbageCollectContext(appContext.UserConfig.Cache, dryRun, appContext.Log));
        }

        private static void RunCore(GarbageCollectContext context)
        {
            if (context.CacheConfig.Disabled)
                return;

            var cacheManager = new CacheManager(context.CacheConfig.RootPath, true, context.CacheConfig.MaxCacheSize, false, context.DryRun, context.Log);
            cacheManager.GarbageCollect(false);
        }


        private class GarbageCollectContext
        {
            private readonly CacheConfig mCacheConfig;
            private readonly bool mDryRun;
            private readonly ILog mLog;

            public GarbageCollectContext(CacheConfig cacheConfig, bool dryRun, ILog log)
            {
                mCacheConfig = cacheConfig;
                mDryRun = dryRun;
                mLog = log;
            }

            public CacheConfig CacheConfig
            {
                get { return mCacheConfig; }
            }

            public bool DryRun
            {
                get { return mDryRun; }
            }

            public ILog Log
            {
                get { return mLog; }
            }
        }
    }
}
