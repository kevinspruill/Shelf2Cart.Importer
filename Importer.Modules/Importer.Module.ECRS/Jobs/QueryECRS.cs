using Importer.Common.Helpers;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS.Jobs
{
    public class QueryECRS : IJob
    {
        // need to set ECRS QueryType somewhere, and pass to CustomerProcess

        public Task Execute(IJobExecutionContext context)
        {
            Logger.Info("Executing QueryECRS Job");

            return Task.CompletedTask;
        }
    }
}
