using System;
using System.Collections.Generic;
using System.Threading;
using CommandLine;
using MAT.OCS.Streaming.Samples.CSharp;
using MAT.OCS.Streaming.Samples.CSharp.Config;
using NLog;

namespace MAT.OCS.Streaming.Samples
{
    public static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly IDictionary<Command, Action> Commands = new Dictionary<Command, Action>
        {
            { Command.SinCosGenerator, SinCosGenerator },
            { Command.SinCosModel, SinCosModel },
            { Command.AbsModel, AbsModel },
            { Command.VCar2Generator, VCar2Generator },
            { Command.ReadSample, ReadSample },
            { Command.WriteSample, WriteSample },
        };

        public static void Main(string[] args)
        {
            SampleAppLoggingConfigurator.Configure();

            new Parser(cfg => cfg.CaseInsensitiveEnumValues = true)
                .ParseArguments<CommandLineSwitches>(args)
                .WithParsed(Run);
        }

        private static void Run(CommandLineSwitches switches)
        {
            Logger.Info($"Command line switches provided are '{switches}'");

            Configuration.SelectedTransport = switches.Transport;
            Configuration.RunningEnvironmentConfig = RunningEnvironmentConfig.For(switches.Environment);

            Commands[switches.Command]();
        }

        private static void ReadSample()
        {
            var readSample = new ReadSample();
            readSample.Run();

            Console.ReadKey();
        }

        private static void WriteSample()
        {
            var writeSample = new WriteSample();
            writeSample.Run();

            Console.ReadKey();
        }

        private static void VCar2Generator()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var vCar2Generator = new VCar2Generator("127.0.0.1:9092", "sessions");
            var task = vCar2Generator.Run(cancellationTokenSource.Token);

            Console.ReadKey();
            cancellationTokenSource.Cancel();

            task.Wait();
        }

        private static void SinCosGenerator()
        {
            var write = new SinCosGenerator(new List<string> {"SinCos"}, "127.0.0.1:9092");
            write.Run();
        }

        private static void SinCosModel()
        {
            var write = new SinCosModel("localhost:9092", "SinCos", "SinPlusCos");
            write.Run().Wait();
        }

        private static void AbsModel()
        {
            var write = new AbsModel("localhost:9092", "SinPlusCos", "Abs");
            write.Run().Wait();
        }
    }
}