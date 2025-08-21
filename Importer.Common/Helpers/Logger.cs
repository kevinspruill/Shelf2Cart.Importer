using NLog;
using System;
using System.Diagnostics;

namespace Importer.Common.Helpers
{
    public static class Logger
    {
        private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public static void Trace(string message) => logger.Trace(message);
        public static void Debug(string message) => logger.Debug(message);
        public static void Info(string message) => logger.Info(message);
        public static void Warn(string message) => logger.Warn(message);
        public static void Error(string message, Exception ex = null) => logger.Error(ex, message);
        public static void Fatal(string message, Exception ex = null) => logger.Fatal(ex, message);

    }
}