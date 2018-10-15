using System;
using System.Threading;
using MAT.OCS.Streaming.IO;
using MAT.OCS.Streaming.Kafka;
using MAT.OCS.Streaming.Model;
using MAT.OCS.Streaming.Model.AtlasConfiguration;
using MAT.OCS.Streaming.Model.DataFormat;
using MAT.OCS.Streaming.Mqtt;
using MAT.OCS.Streaming.Samples.CSharp.Config;
using NLog;

namespace MAT.OCS.Streaming.Samples.CSharp
{
    public class WriteSample
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public const double Frequency = 100.0;

        public void Run()
        {
            var dependenciesClient = new HttpDependencyClient(DependencyServiceConfig.Uri, "dev");
            var dataFormatClient = new DataFormatClient(dependenciesClient);
            var atlasConfigurationClient = new AtlasConfigurationClient(dependenciesClient);

            var dataFormatId = dataFormatClient.PutAndIdentifyDataFormat(
                DataFormat.DefineFeed().Parameter(TopicConfiguration.ParameterIdentifier).AtFrequency(Frequency).BuildFormat());

            var atlasConfigurationId = atlasConfigurationClient.PutAndIdentifyAtlasConfiguration(MakeAtlasConfiguration());

            using (var outputTopicWrapper = CreateOutputTopicWrapper())
            {
                Console.WriteLine("Hit <enter> to start generating data");
                Console.ReadLine();
                GenerateData(outputTopicWrapper.Topic, dataFormatClient, dataFormatId, atlasConfigurationId);
                Console.WriteLine();
                Console.WriteLine("Hit <enter> to exit");
                Console.ReadLine();
            }
        }

        private OutputTopicWrapper CreateOutputTopicWrapper()
        {
            OutputTopicWrapper ForKafka()
            {
                var client = new KafkaStreamClient(KafkaConfigForSample.BrokerList);
                var topic = client.OpenOutputTopic(TopicConfiguration.Topic);
                return new OutputTopicWrapper(client, topic);
            }

            OutputTopicWrapper ForMqtt()
            {
                var client = new MqttStreamClient(MqttConnectionConfigForSample.Current);
                var topic = client.OpenOutputTopic(TopicConfiguration.Topic);
                return new OutputTopicWrapper(client, topic);
            }

            switch (Configuration.SelectedTransport)
            {
                case StreamingTransport.Kafka:
                    return ForKafka();
                case StreamingTransport.Mqtt:
                    return ForMqtt();
                default:
                    throw new NotSupportedException();
            }
        }

        private void GenerateData(IOutputTopic topic, DataFormatClient dataFormatClient, string dataFormatId, string atlasConfigurationId)
        {
            const int steps = 12000;
            const int delay = (int)(1000 / Frequency);
            var random = new Random();

            var start = DateTime.UtcNow;
            var epoch = start.ToTelemetryTime();

            var output = new SessionTelemetryDataOutput(topic, dataFormatId, dataFormatClient);
            var outputFeed = output.DataOutput.BindDefaultFeed();

            // declare a session
            output.SessionOutput.AddSessionDependency(DependencyTypes.DataFormat, dataFormatId);
            output.SessionOutput.AddSessionDependency(DependencyTypes.AtlasConfiguration, atlasConfigurationId);
            output.SessionOutput.SessionState = StreamSessionState.Open;
            output.SessionOutput.SessionStart = start.Date;
            output.SessionOutput.SessionIdentifier = "random_walk";
            output.SessionOutput.SendSession();

            // generate data points            
            var data = outputFeed.MakeTelemetryData(1000000, epoch);
            var param = data.Parameters[0];
            var rangeWalker = new RandomRangeWalker(0, 1);
            for (var step = 1; step <= steps; step++)
            {
                Thread.Sleep(delay);

                for (var i = 0; i < 1000000; i++)
                {
                    // writing a single data point for simplicity, but you can send chunks of data
                    // timestamps expressed in ns since the epoch (which is the start of the session)
                    var elapsedNs = step * delay * 1000000L;
                    data.TimestampsNanos[i] = elapsedNs;
                    param.Statuses[i] = DataStatus.Sample;

                    var value = rangeWalker.GetNext();

                    param.AvgValues[i] = value;

                    output.SessionOutput.SessionDurationNanos = elapsedNs;
                    //Logger.Info(NumberToBarString.Convert(value));
                }

                outputFeed.EnqueueAndSendData(data);
            }

            output.SessionOutput.SessionState = StreamSessionState.Closed;
            output.SessionOutput.SendSession();
        }

        private AtlasConfiguration MakeAtlasConfiguration()
        {
            return new AtlasConfiguration
            {
                AppGroups =
                {
                    {
                        "app", new ApplicationGroup
                        {
                            Groups =
                            {
                                {
                                    "group", new ParameterGroup
                                    {
                                        Parameters =
                                        {
                                            {TopicConfiguration.ParameterIdentifier, new Parameter { Name = "example", Units = "kmh" }}
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private class RandomRangeWalker
        {
            private readonly double min;
            private readonly double max;
            private readonly Random random;
            private double value;

            public RandomRangeWalker(double min, double max)
            {
                this.min = min;
                this.max = max;
                this.random = new Random();
            }

            public double GetNext()
            {
                var nextChange = (random.NextDouble() - 0.5) / 4;
                if (nextChange + value < min || nextChange + value > max)
                    nextChange = -nextChange;
                value += nextChange;
                return value;
            }
        }

        private class OutputTopicWrapper : IDisposable
        {
            public IOutputTopic Topic { get; }
            private readonly IDisposable client;

            public OutputTopicWrapper(IDisposable client, IOutputTopic topic)
            {
                Topic = topic;
                this.client = client;
            }

            public void Dispose()
            {
                Topic.Dispose();
                client.Dispose();
            }
        }
    }
}