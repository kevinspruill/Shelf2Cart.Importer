using Importer.Common.Interfaces;
using Importer.Common.Logging;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;

namespace Importer.Common.Helpers
{
    public static class Logger
    {
        private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private static bool _initialized = false;

        // Method to initialize the pipe service for NLog
        public static void InitializePipeService(IPipeMessageService pipeMessageService)
        {
            // Register the custom target type with NLog
            Target.Register<PipeMessageTarget>("PipeMessage");

            // Set the pipe service on the custom NLog target
            PipeMessageTarget.SetPipeService(pipeMessageService);

            // If using programmatic configuration (instead of XML)
            AddPipeTargetProgrammatically(pipeMessageService);

            _initialized = true;

            // Test log to verify it's working
            Info("Pipe logging initialized");
        }

        private static void AddPipeTargetProgrammatically(IPipeMessageService pipeMessageService)
        {

            // Get existing configuration or create new
            var config = LogManager.Configuration;
            if (config == null)
            {
                config = new LoggingConfiguration();
            }

            // Check if pipe target already exists
            var existingPipeTarget = config.FindTargetByName("pipe");
            if (existingPipeTarget != null)
            {
                config.RemoveTarget("pipe");
            }

            // Create the pipe target
            var pipeTarget = new PipeMessageTarget
            {
                Name = "pipe",
                Layout = "${longdate} |${level:uppercase=true}| ${message} ${exception:format=tostring}"
            };

            // Add target to configuration
            config.AddTarget("pipe", pipeTarget);

            // Add rule to send all logs to pipe
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, pipeTarget, "*");

            // Apply configuration
            LogManager.Configuration = config;

            // Force reconfiguration
            LogManager.ReconfigExistingLoggers();
        }

        public static void Trace(string message)
        {
            logger.Trace(message);
        }

        public static void Debug(string message)
        {
            logger.Debug(message);
        }

        public static void Info(string message)
        {
            logger.Info(message);
        }

        public static void Warn(string message)
        {
            logger.Warn(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            logger.Error(ex, message);
        }

        public static void Fatal(string message, Exception ex = null)
        {
            logger.Fatal(ex, message);
        }
    }
}