using NLog;
using NLog.Config;
using NLog.Targets;
using Importer.Common.Interfaces;
using System;

namespace Importer.Common.Logging
{
    [Target("PipeMessage")]
    public sealed class PipeMessageTarget : TargetWithLayout
    {
        private static IPipeMessageService _pipeMessageService;

        public static void SetPipeService(IPipeMessageService pipeService)
        {
            _pipeMessageService = pipeService;
        }

        public PipeMessageTarget()
        {
            // Set a default layout if none is provided
            this.Layout = "${longdate} |${level:uppercase=true}| ${message} ${exception:format=tostring}";
        }

        protected override void Write(LogEventInfo logEvent)
        {
            if (_pipeMessageService == null)
                return;

            try
            {
                string logMessage = Layout.Render(logEvent);
                _pipeMessageService.SendMessage(logMessage);
            }
            catch
            {
                // Silently ignore exceptions to prevent recursive logging issues
            }
        }
    }
}