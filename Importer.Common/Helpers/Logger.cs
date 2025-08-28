using NLog;
using System;
using System.Diagnostics;
using Importer.Common.Interfaces;

namespace Importer.Common.Helpers
{
    public static class Logger
    {
        private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private static IPipeMessageService _pipeMessageService;

        // Method to initialize the pipe service
        public static void InitializePipeService(IPipeMessageService pipeMessageService)
        {
            _pipeMessageService = pipeMessageService;
        }

        public static void Trace(string message)
        {
            logger.Trace(message);
            _pipeMessageService?.SendMessage($"\n[TRACE] {message}");
        }

        public static void Debug(string message)
        {
            logger.Debug(message);
            _pipeMessageService?.SendMessage($"\n[DEBUG] {message}");
        }

        public static void Info(string message)
        {
            logger.Info(message);
            _pipeMessageService?.SendMessage($"\n[INFO] {message}");
        }

        public static void Warn(string message)
        {
            logger.Warn(message);
            _pipeMessageService?.SendMessage($"\n[WARN] {message}");
        }

        public static void Error(string message, Exception ex = null)
        {
            logger.Error(ex, message);
            var fullMessage = ex == null ? message : $"{message} - {ex.Message}";
            _pipeMessageService?.SendMessage($"\n[ERROR] {fullMessage}");
        }

        public static void Fatal(string message, Exception ex = null)
        {
            logger.Fatal(ex, message);
            var fullMessage = ex == null ? message : $"{message} - {ex.Message}";
            _pipeMessageService?.SendMessage($"\n[FATAL] {fullMessage}");
        }
    }
}