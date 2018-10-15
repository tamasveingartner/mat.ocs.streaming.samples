using System;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace MAT.OCS.Streaming.Samples
{
    public static class SampleAppLoggingConfigurator
    {
        public static void Configure()
        {
            var config = new LoggingConfiguration();

            var basedirStreamingsamplesLog = "${basedir}/StreamingSamples.log";
            var fileTarget = new FileTarget("file-target")
            {
                FileName = basedirStreamingsamplesLog,
                Layout = @"${longdate} ${level} ${message} ${exception}",
            };
            config.AddTarget(fileTarget);
            config.AddRuleForAllLevels(fileTarget);

            var consoleTarget = new ColoredConsoleTarget("console-target")
            {
                Layout = @"${date:format=HH\:mm\:ss.fff} ${level} ${message} ${exception}",
            };
            config.AddTarget(consoleTarget);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);

            LogManager.Configuration = config;

            Console.WriteLine($"NLog configured. You can find logs in {basedirStreamingsamplesLog}");
        }
    }
}