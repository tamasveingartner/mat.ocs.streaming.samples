using System;

namespace MAT.OCS.Streaming.Samples.CSharp.Config
{
    internal abstract class RunningEnvironmentConfig
    {
        internal static RunningEnvironmentConfig For(RunningEnvironment environment)
        {
            switch (environment)
            {
                case RunningEnvironment.AzureTest:
                    return new AzureConfig();
                case RunningEnvironment.Local:
                    return new LocalConfig();
                default:
                    throw new NotSupportedException();
            }
        }

        public abstract Uri DependenciesUri { get; }
        public abstract string KafkaBrokerList { get; }
        public abstract string MqttBroker { get; }
        public abstract string MqttUsername { get; }
        public abstract string MqttPassword { get; }
    }

    internal class AzureConfig : RunningEnvironmentConfig
    {
        public override Uri DependenciesUri { get; } = new Uri("http://10.228.5.4:8180/api/dependencies/");
        public override string KafkaBrokerList { get; } = "10.228.4.22:9092";
        public override string MqttBroker { get; } = "10.228.4.22";
        public override string MqttUsername { get; } = "test";
        public override string MqttPassword { get; } = "test";
    }

    internal class LocalConfig : RunningEnvironmentConfig
    {
        public override Uri DependenciesUri { get; } = new Uri("http://10.228.5.4:8180/api/dependencies/");
        public override string KafkaBrokerList { get; } = "127.0.0.1:9092";
        public override string MqttBroker { get; } = "127.0.0.1";
        public override string MqttUsername { get; } = "guest";
        public override string MqttPassword { get; } = "guest";
    }
}