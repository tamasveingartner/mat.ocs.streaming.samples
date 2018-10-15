using MAT.OCS.Streaming.Mqtt;

namespace MAT.OCS.Streaming.Samples.CSharp.Config
{
    public class MqttConnectionConfigForSample
    {
        public static MqttConnectionConfig Current { get; } = 
            new MqttConnectionConfig(
                Configuration.RunningEnvironmentConfig.MqttBroker, 
                Configuration.RunningEnvironmentConfig.MqttUsername, 
                Configuration.RunningEnvironmentConfig.MqttPassword);
    }
}