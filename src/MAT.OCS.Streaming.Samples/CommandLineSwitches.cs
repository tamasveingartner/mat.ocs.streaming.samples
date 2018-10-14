using CommandLine;

namespace MAT.OCS.Streaming.Samples
{
    internal class CommandLineSwitches
    {
        [Option('c', Default = Command.ReadSample, HelpText = "Command to run.")]
        public Command Command { get; set; }

        [Option('t', Default = StreamingTransport.Mqtt, HelpText = "Transport to use.")]
        public StreamingTransport Transport { get; set; }

        [Option('e', Default = RunningEnvironment.AzureTest, HelpText = "Environment to run against.")]
        public RunningEnvironment Environment { get; set; }

        public override string ToString()
        {
            return $"{nameof(Command)}: {Command}, {nameof(Transport)}: {Transport}, {nameof(Environment)}: {Environment}";
        }
    }
}