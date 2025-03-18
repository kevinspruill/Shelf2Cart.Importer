using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.Modes
{
    public class ServiceMode
    {
        public async Task Start()
        {
            // Create Scheduler with Quartz (if needed)
            // Create FileWatchers for ImportModules
            // Create API Clients for ImportModules
            // Create Named Pipes Service for UI

        }

        public async Task Stop()
        {
        }
    }
}
