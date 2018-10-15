using MAT.OCS.Streaming.Samples.CSharp.Config;

namespace MAT.OCS.Streaming.Samples
{
    internal class Configuration
    {
        public static StreamingTransport SelectedTransport { get; set; }

        public static RunningEnvironmentConfig RunningEnvironmentConfig { get; set; }
    }
}