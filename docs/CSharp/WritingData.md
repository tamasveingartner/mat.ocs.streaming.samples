# ATLAS Advanced Streaming Samples

![Build Status](https://mat-ocs.visualstudio.com/Telemetry%20Analytics%20Platform/_apis/build/status/MAT.OCS.Streaming/Streaming%20Samples?branchName=develop)

Table of Contents
=================
<!--ts-->
* [Introduction - MAT.OCS.Streaming library](/README.md)
* C# samples
    * [Writing data](/docs/CSharp/WritingData.md)
    * [Reading data](/docs/CSharp/ReadingData.md)
* MATLAB samples
    * [Introduction to .NET MATLAB integration](/docs/Matlab/IntroToNetMatlabIntegration.md)
    * [Reading data from Kafka](/docs/Matlab/ReadingDataFromKafka.md)
    * [Writing data to Kafka](/docs/Matlab/WritingDataToKafka.md)
        * [ATLAS10 configuration](/docs/Matlab/Atlas10Configuration.md)
    * [Reading and writing in pipeline](/docs/Matlab/ReadingAndWritingInPipeline.md)
<!--te-->

# Writing Data

Demonstrates writing data to a stream, with ATLAS configuration.

```c#
using System;
using System.Threading;
using MAT.OCS.Streaming.IO;
using MAT.OCS.Streaming.Kafka;
using MAT.OCS.Streaming.Model;
using MAT.OCS.Streaming.Model.AtlasConfiguration;
using MAT.OCS.Streaming.Model.DataFormat;

namespace MAT.OCS.Streaming.Samples.CSharp
{
    public class WriteSample
    {

        public const double Frequency = 100.0;
        public const string ParameterIdentifier = "example:app";

        public string BrokerList { get; set; } = "localhost:9092";

        public Uri DependenciesUri { get; set; } = new Uri("http://localhost:8180/api/dependencies/");

        public void Run()
        {
            var dependenciesClient = new HttpDependencyClient(DependenciesUri, "dev");
            var dataFormatClient = new DataFormatClient(dependenciesClient);
            var atlasConfigurationClient = new AtlasConfigurationClient(dependenciesClient);

            var dataFormatId = dataFormatClient.PutAndIdentifyDataFormat(
                DataFormat.DefineFeed().Parameter(ParameterIdentifier).AtFrequency(Frequency).BuildFormat());

            var atlasConfigurationId = atlasConfigurationClient.PutAndIdentifyAtlasConfiguration(MakeAtlasConfiguration());

            using (var client = new KafkaStreamClient(BrokerList))
            using (var topic = client.OpenOutputTopic("demo2"))
            {
                Console.WriteLine("Hit ENTER to start generating data");
                Console.ReadLine();
                GenerateData(topic, dataFormatClient, dataFormatId, atlasConfigurationId);
                Console.WriteLine();
                Console.WriteLine("Hit ENTER to exit");
                Console.ReadLine();
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
            var data = outputFeed.MakeTelemetryData(1, epoch);
            var param = data.Parameters[0];
            var value = 0.0;
            for (var step = 1; step <= steps; step++)
            {
                Thread.Sleep(delay);

                // writing a single data point for simplicity, but you can send chunks of data
                // timestamps expressed in ns since the epoch (which is the start of the session)
                var elapsedNs = step * delay * 1000000L;
                data.TimestampsNanos[0] = elapsedNs;
                param.Statuses[0] = DataStatus.Sample;
                param.AvgValues[0] = value;

                output.SessionOutput.SessionDurationNanos = elapsedNs;
                outputFeed.EnqueueAndSendData(data);

                value += (random.NextDouble() - 0.5);
                Console.Write(">");
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
                                            {ParameterIdentifier, new Parameter { Name = "example", Units = "kmh" }}
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
```
