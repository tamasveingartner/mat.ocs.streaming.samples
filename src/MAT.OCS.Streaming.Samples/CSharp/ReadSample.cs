using System;
using MAT.OCS.Streaming.IO;
using MAT.OCS.Streaming.Kafka;
using MAT.OCS.Streaming.Model;
using MAT.OCS.Streaming.Mqtt;
using MAT.OCS.Streaming.Samples.CSharp.Config;
using NLog;

namespace MAT.OCS.Streaming.Samples.CSharp
{
    public class ReadSample
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly DataFormatClient dataFormatClient = new DataFormatClient(new HttpDependencyClient(DependencyServiceConfig.Uri, "dev"));

        public void Run()
        {
            Console.WriteLine("Hit <enter> to stop");

            using (CreateStreamPipelineWrapper())
            {
                Console.ReadLine();
            }
        }

        private StreamPipelineWrapper CreateStreamPipelineWrapper()
        {
            StreamPipelineWrapper ForKafka()
            {
                var client = new KafkaStreamClient(KafkaConfigForSample.BrokerList);
                var topic = client.StreamTopic(TopicConfiguration.Topic).Into(streamId => ProcessStream(streamId, dataFormatClient));
                return new StreamPipelineWrapper(client, topic);
            }

            StreamPipelineWrapper ForMqtt()
            {
                var client = new MqttStreamClient(MqttConnectionConfigForSample.Current);
                var topic = client.StreamTopic(TopicConfiguration.Topic).Into(streamId => ProcessStream(streamId, dataFormatClient));
                return new StreamPipelineWrapper(client, topic);
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

        private IStreamInput ProcessStream(string streamId, DataFormatClient dataFormatClient)
        {
            Logger.Info($"New stream: {streamId}");
            var input = new SessionTelemetryDataInput(streamId, dataFormatClient);
            input.DataInput.BindDefaultFeed(TopicConfiguration.ParameterIdentifier).DataBuffered += PrintSamples;
            input.StreamFinished += (sender, args) => Console.WriteLine("--------");
            return input;
        }

        private void PrintSamples(object input, IO.TelemetryData.TelemetryDataFeedEventArgs e)
        {
            var data = e.Buffer.GetData();
            var time = data.TimestampsNanos;
            var vCar = data.Parameters[0].AvgValues;
            for (var i = 0; i < time.Length; i++)
            {
                var fromMilliseconds = TimeSpan.FromMilliseconds(time[i].NanosToMillis());

                Logger.Info($"{fromMilliseconds:hh\\:mm\\:ss\\.fff}, { NumberToBarString.Convert(vCar[i]) }");
            }
        }

        private class StreamPipelineWrapper : IDisposable
        {
            public IStreamPipeline StreamPipeline { get; }
            private readonly IDisposable client;

            public StreamPipelineWrapper(IDisposable client, IStreamPipeline streamPipeline)
            {
                StreamPipeline = streamPipeline;
                this.client = client;
            }

            public void Dispose()
            {
                StreamPipeline.Dispose();
                client.Dispose();
            }
        }
    }
}