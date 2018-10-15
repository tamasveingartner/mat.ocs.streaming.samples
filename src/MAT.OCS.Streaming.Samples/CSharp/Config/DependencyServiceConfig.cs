using System;

namespace MAT.OCS.Streaming.Samples.CSharp.Config
{
    public static class DependencyServiceConfig
    {
        public static Uri Uri { get; } = 
            Configuration.RunningEnvironmentConfig.DependenciesUri;
    }
}