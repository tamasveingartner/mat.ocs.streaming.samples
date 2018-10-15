using System;
using System.Collections.Generic;
using MAT.OCS.Streaming.IO.TelemetryData;
using MAT.OCS.Streaming.Model.AtlasConfiguration;
using MAT.OCS.Streaming.Model.DataFormat;

namespace MAT.OCS.Streaming.Samples.CSharp
{
    public class SinCosModel : KafkaTopicModeler
    {
        private const double Frequency = 100; // Hz

        /// <inheritdoc />
        public SinCosModel(string brokerList, string inputTopicName, string outputTopciName)
            : base(brokerList, inputTopicName, outputTopciName)
        {
        }

        /// <summary>
        /// Defines double vCar parameter data format.
        /// </summary>
        protected override DataFormat CreateOutputDataFormat()
        {
            return DataFormat.DefineFeed()
                .Parameters(new List<string> { "Sin(x)+Cos(2x)" })
                .AtFrequency(Frequency)
                .BuildFormat();
        }

        /// <summary>
        /// Define source data format of vCar parameter we using here.
        /// </summary>
        protected override DataFormat CreateInputDataFormat()
        {
            return DataFormat.DefineFeed()
                .Parameters(new List<string> { "Sin(x)", "Cos(x)" })
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
            var parameter1 = data.Parameters[0];
            var parameter2 = data.Parameters[1];


            // Data can generally be safely modified inline
            for (var i = 0; i < parameter1.AvgValues.Length; i++)
            {
                parameter1.AvgValues[i] = parameter1.AvgValues[i] + parameter2.AvgValues[i];
            }

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
                                                "Sin(x)+Cos(2x)", new Parameter
                                                {
                                                    Name = "Sin(x)+Cos(2x)",
                                                    PhysicalRange = new Range(-Math.PI, Math.PI)

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