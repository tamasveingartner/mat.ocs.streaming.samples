using System;
using System.Collections.Generic;
using MAT.OCS.Streaming.IO.TelemetryData;
using MAT.OCS.Streaming.Model.AtlasConfiguration;
using MAT.OCS.Streaming.Model.DataFormat;

namespace MAT.OCS.Streaming.Samples
{
    public class VCar2Generator : KafkaTopicModeler
    {
        private const double Frequency = 100; // Hz

        /// <inheritdoc />
        public VCar2Generator(string brokerList, string inputTopicName) 
            : base(brokerList, inputTopicName, "vCar2")
        {
        }

        /// <summary>
        /// Defines double vCar parameter data format.
        /// </summary>
        protected override DataFormat CreateOutputDataFormat()
        {
            return DataFormat.DefineFeed()
                .Parameter("vCar2:Chassis")
                .AtFrequency(Frequency)
                .BuildFormat();
        }

        /// <summary>
        /// Define source data format of vCar parameter we using here.
        /// </summary>
        protected override DataFormat CreateInputDataFormat()
        {
            return DataFormat.DefineFeed()
                .Parameter("vCar:Chassis")
                .AtFrequency(Frequency)
                .BuildFormat();
        }

        /// <inheritdoc />
        /// <summary>
        /// We just double source vCar parameter values.
        /// </summary>
        /// <param name="dataBuffer">The data buffer.</param>
        /// <returns>Data for output stream.</returns>
        protected override TelemetryData ProcessData(TelemetryDataBuffer dataBuffer)
        {
            var data = dataBuffer.GetData();
            var vCar = data.Parameters[0];

            // Data can generally be safely modified inline
            for (var i = 0; i < vCar.AvgValues.Length; i++)
                vCar.AvgValues[i] = vCar.AvgValues[i] * 2;

            Console.Write(".");

            return data;
        }

        public override string ConsumerGroup => "vCar2Modeler";

        /// <summary>
        /// TODO: Create builder for this!
        /// </summary>
        protected override AtlasConfiguration CreateAtlasConfiguration()
        {
            return new AtlasConfiguration
            {
                AppGroups = new Dictionary<string, ApplicationGroup>
                {
                    {
                        "app", new ApplicationGroup
                        {
                            Groups = new Dictionary<string, ParameterGroup>
                            {
                                {
                                    "group", new ParameterGroup
                                    {
                                        Parameters = new Dictionary<string, Parameter>
                                        {
                                            {
                                                "vCar2:Chassis", new Parameter
                                                {
                                                    Name = "vCar2",
                                                    Units = "kmh",
                                                    Description = "Double speed!"
                                                }
                                            }
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