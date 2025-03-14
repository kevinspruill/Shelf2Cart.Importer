using NLog;
using System;
using System.Diagnostics;

namespace Importer.Common.Helpers
{
    public static class Logger
    {
        private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public static void LogInfoEvent(string message)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Shelf 2 Cart Merchandiser ECRS Query Program";
                eventLog.WriteEntry(message, EventLogEntryType.Information, 101);
            }
            logger.Info(message);
        }

        public static void LogErrorEvent(string message, Exception ex = null)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Shelf 2 Cart Merchandiser ECRS Query Program";
                eventLog.WriteEntry(message, EventLogEntryType.Error, 109);
            }
            logger.Error(ex, message);
        }

        public static void Trace(string message) => logger.Trace(message);
        public static void Debug(string message) => logger.Debug(message);
        public static void Info(string message) => logger.Info(message);
        public static void Warn(string message) => logger.Warn(message);
        public static void Error(string message, Exception ex = null) => logger.Error(ex, message);
        public static void Fatal(string message, Exception ex = null) => logger.Fatal(ex, message);

    }
}