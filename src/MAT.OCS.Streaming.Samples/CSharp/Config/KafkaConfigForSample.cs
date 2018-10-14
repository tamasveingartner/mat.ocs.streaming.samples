namespace MAT.OCS.Streaming.Samples.CSharp.Config
{
    public static class KafkaConfigForSample
    {
        public static string BrokerList { get; } = 
            Configuration.RunningEnvironmentConfig.KafkaBrokerList;
    }
}