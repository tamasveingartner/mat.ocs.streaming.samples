using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MAT.OCS.Streaming.IO;
using MAT.OCS.Streaming.Kafka;
using MAT.OCS.Streaming.Model;
using MAT.OCS.Streaming.Model.AtlasConfiguration;
using MAT.OCS.Streaming.Model.DataFormat;

namespace MAT.OCS.Streaming.Samples.CSharp
{
    public class SinCosGenerator
    {
        private readonly List<string> topicLists;
        private readonly string brokerList;

        public const double Frequency = 100.0;
        public const string ParameterIdentifier = "vCar:Chassis";

        public Uri DependenciesUri { get; set; } = new Uri("http://localhost:8180/api/dependencies/");

        public SinCosGenerator(List<string> topicLists, string brokerList)
        {
            this.topicLists = topicLists;
            this.brokerList = brokerList;
        }

        public void Run()
        {
            var dependenciesClient = new HttpDependencyClient(DependenciesUri, "dev");
            var dataFormatClient = new DataFormatClient(dependenciesClient);
            var atlasConfigurationClient = new AtlasConfigurationClient(dependenciesClient);

            var dataFormatId = dataFormatClient.PutAndIdentifyDataFormat(
                DataFormat.DefineFeed().Parameters(new List<string> { "Sin(x)", "Cos(x)" }).AtFrequency(Frequency).BuildFormat());

            var atlasConfigurationId = atlasConfigurationClient.PutAndIdentifyAtlasConfiguration(MakeAtlasConfiguration());

            var tasks = new List<Task>();

            using (var client = new KafkaStreamClient(brokerList))
            {
                foreach (var topicName in topicLists)
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        using (var topic = client.OpenOutputTopic(topicName))
                        {
                            GenerateData(topic, dataFormatClient, dataFormatId, atlasConfigurationId);
                            Console.WriteLine("Hit <enter> to exit");
                            Console.ReadLine();
                        }
                    }));

                Task.WaitAll(tasks.ToArray());
            }
        }

        private void GenerateData(IOutputTopic topic, DataFormatClient dataFormatClient, string dataFormatId, string atlasConfigurationId)
        {
            var foregroundColor = (ConsoleColor)(15 - this.topicLists.IndexOf(topic.TopicName));

            const int steps = 120000;
            const int delay = (int)(1000 / Frequency);

            var start = DateTime.UtcNow;
            var epoch = start.ToTelemetryTime();

            var output = new SessionTelemetryDataOutput(topic, dataFormatId, dataFormatClient);
            var outputFeed = output.DataOutput.BindDefaultFeed();

            // declare a session
            output.SessionOutput.AddSessionDependency(DependencyTypes.DataFormat, dataFormatId);
            output.SessionOutput.AddSessionDependency(DependencyTypes.AtlasConfiguration, atlasConfigurationId);
            output.SessionOutput.SessionState = StreamSessionState.Open;
            output.SessionOutput.SessionStart = start;
            output.SessionOutput.SessionIdentifier = "random_walk" + DateTime.Now.TimeOfDay;
            output.SessionOutput.SendSession();

            // generate data points            
            var data = outputFeed.MakeTelemetryData(1, epoch);
            var sinParam = data.Parameters[0];
            var cosParam = data.Parameters[1];

            for (var step = 1; step <= steps; step++)
            {
                Thread.Sleep(delay);

                // writing a single data point for simplicity, but you can send chunks of data
                // timestamps expressed in ns since the epoch (which is the start of the session)
                var elapsedNs = step * delay * 1000000L;
                data.TimestampsNanos[0] = elapsedNs;
                sinParam.Statuses[0] = DataStatus.Sample;
                sinParam.AvgValues[0] = Math.Sin(step / 50.0);

                cosParam.Statuses[0] = DataStatus.Sample;
                cosParam.AvgValues[0] = Math.Cos(step / 100.0);

                output.SessionOutput.SessionDurationNanos = elapsedNs;
                outputFeed.EnqueueAndSendData(data);

                Console.ForegroundColor = foregroundColor;
                Console.Write(">");

                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    break;
            }

            output.SessionOutput.SessionState = StreamSessionState.Closed;
            output.SessionOutput.SendSession();
        }

        private static AtlasConfiguration MakeAtlasConfiguration()
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
                                            {"Sin(x)", new Parameter
                                            {
                                                Name = "Sin(x)",
                                                Units = "kmh",
                                                PhysicalRange = new Range(-Math.PI/2, Math.PI/2)
                                            }},
                                            {"Cos(x)", new Parameter
                                            {
                                                Name = "Cos(x)",
                                                Units = "kmh",
                                                PhysicalRange = new Range(-Math.PI/2, Math.PI/2)
                                            }}
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

        }


    }
}