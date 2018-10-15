using System;
using System.Threading;
using System.Threading.Tasks;
using MAT.OCS.Streaming.IO;
using MAT.OCS.Streaming.IO.TelemetryData;
using MAT.OCS.Streaming.Kafka;
using MAT.OCS.Streaming.Model.AtlasConfiguration;
using MAT.OCS.Streaming.Model.DataFormat;

namespace MAT.OCS.Streaming.Samples
{
    /// <summary>
    ///     Abstract base class for every model using Kafka as a data input and output.
    /// </summary>
    public abstract class KafkaTopicModeler
    {
        private string dataFormatId;
        private string atlasConfId;
        protected const string AtlasConfigurationIdSubfix = "AtlasConf";

        /// <summary>
        ///     Comma-separated list of Kafka brokers, without embedded whitespace.
        /// </summary>
        /// <value>
        ///     The broker list.
        /// </value>
        public string BrokerList { get; }

        /// <summary>
        ///     Name of the input topic for data reading from Kafka.
        /// </summary>
        /// <value>
        ///     The name of the input topic.
        /// </value>
        public string InputTopicName { get; }

        /// <summary>
        ///     Name of the output topic that would be pushed back to Kafka.
        /// </summary>
        /// <value>
        ///     The name of the output topic.
        /// </value>
        public string OutputTopicName { get; }

        /// <summary>
        ///     This string would identify consumer of data from Kafka.
        /// </summary>
        /// <value>
        ///     The consumer group.
        /// </value>
        public abstract string ConsumerGroup { get; }

        /// <summary>
        ///     Timespan allocated for draining. After this timespan drain is cancelled and the pipeline is stopped.
        /// </summary>
        /// <value>
        ///     The drain timeout.
        /// </value>
        public virtual TimeSpan DrainTimeout => new TimeSpan(0, 0, 0, 10);

        protected virtual string InputFeed => TelemetryDataInput.DefaultFeedId;
        protected virtual string OutputFeed => TelemetryDataOutput.DefaultFeedId;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaTopicModeler" /> class.
        /// </summary>
        /// <param name="brokerList">Comma-separated list of Kafka brokers, without embedded whitespace.</param>
        /// <param name="inputTopicName">Name of the input topic for data reading from Kafka.</param>
        /// <param name="outputTopicName">Name of the output topic that would be pushed back to Kafka.</param>
        protected KafkaTopicModeler(string brokerList, string inputTopicName, string outputTopicName)
        {
            BrokerList = brokerList;
            InputTopicName = inputTopicName;
            OutputTopicName = outputTopicName;
        }

        /// <summary>
        ///     Listen input topic stream in a loop and produce new data into output topic.
        /// </summary>
        public Task Run(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Factory.StartNew(() =>
            {
                // TODO: Replace this with dependency service REST API call.
                var dependencyClient = new HttpDependencyClient(new Uri("http://localhost:8180/api/dependencies/"), "dev", false);

                var dataFormatClient = new DataFormatClient(dependencyClient); // would be a web service in production
                dataFormatId = dataFormatClient.PutAndIdentifyDataFormat(CreateOutputDataFormat());

                var acClient = new AtlasConfigurationClient(dependencyClient);

                var atlasConfiguration = CreateAtlasConfiguration();
                atlasConfId = acClient.PutAndIdentifyAtlasConfiguration(atlasConfiguration);

                using (var client = new KafkaStreamClient(BrokerList))
                {
                    client.ConsumerGroup = ConsumerGroup;

                    using (var enrichTopic = client.OpenOutputTopic(OutputTopicName))
                    using (var pipeline = CreateStreamPipeline(client, dataFormatClient, enrichTopic))
                    {
                        cancellationToken.WaitHandle.WaitOne();
                        pipeline.Drain();
                        pipeline.WaitUntilStopped(DrainTimeout, default(CancellationToken));
                    }
                }
            });
        }

        /// <summary>
        ///     Define atlas configuration for output topic here.
        /// </summary>
        protected abstract AtlasConfiguration CreateAtlasConfiguration();

        /// <summary>
        ///     Define data format of output topic in this method.
        /// </summary>
        protected abstract DataFormat CreateOutputDataFormat();

        /// <summary>
        ///     Define data format of input topic in this method.
        /// </summary>
        protected abstract DataFormat CreateInputDataFormat();

        /// <summary>
        ///     Processes data here and return new one that would be pushed back to Kafka.
        /// </summary>
        /// <param name="dataBuffer">The data buffer.</param>
        /// <returns></returns>
        protected abstract TelemetryData ProcessData(TelemetryDataBuffer dataBuffer);

        private IStreamPipeline CreateStreamPipeline(
            KafkaStreamClient client,
            DataFormatClient dataFormatClient,
            IOutputTopic outputTopic)
        {
            // lambda is called for each stream/session
            return client
                .StreamTopic(InputTopicName)
                .Into(streamId => CreateStreamInput(dataFormatClient, outputTopic, streamId));
        }

        /// <summary>
        ///     This method creates stream pipeline for each child stream.
        /// </summary>
        /// <param name="dataFormatClient">The data format client.</param>
        /// <param name="outputTopic">The output topic.</param>
        /// <param name="streamId">The stream identifier.</param>
        /// <returns>Stream pipeline.</returns>
        private IStreamInput CreateStreamInput(DataFormatClient dataFormatClient, IOutputTopic outputTopic, string streamId)
        {
            // these templates provide commonly-combined data, but you can make your own
            var input = new SessionTelemetryDataInput(streamId, dataFormatClient);
            var output = new SessionTelemetryDataOutput(outputTopic, dataFormatId, dataFormatClient);

            // data is split into named feeds; use default feed if you don't need that
            var inputFeed = input.DataInput.BindFeed(InputFeed, CreateInputDataFormat());
            var outputFeed = output.DataOutput.BindFeed(OutputFeed);

            output.SessionOutput.AddSessionDependency(DependencyTypes.AtlasConfiguration, atlasConfId);
            output.SessionOutput.AddSessionDependency(DependencyTypes.DataFormat, dataFormatId);

            // automatically propagate session metadata and lifecycle
            input.LinkToOutput(output.SessionOutput, identifier => identifier + "_" + OutputTopicName);
            input.LapsInput.LapStarted += (s, e) => output.LapsOutput.SendLap(e.Lap);

            // react to data
            inputFeed.DataBuffered += (sender, e) =>
            {
                var data = ProcessData(e.Buffer);

                outputFeed.EnqueueAndSendData(data);
            };

            return input;
        }
    }

}