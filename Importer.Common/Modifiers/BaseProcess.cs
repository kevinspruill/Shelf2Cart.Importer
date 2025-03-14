using Importer.Common.Interfaces;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Modifiers
{
    public class BaseProcess : ICustomerProcess
    {
        public string Name => "ECRS Base Processor - No Customer Configuration Loaded";
        public bool ForceUpdate { get; set; } = false;
        public void PreQueryRoutine()
        {
            // No pre-processing required
        }
        public tblProducts ProductProcessor(tblProducts product)
        {
            return product;
        }
        public void PostProductProcess()
        {
            // No post-processing required
        }

        public tblProducts PreProductProcess(tblProducts product = null)
        {
            return product;
        }

        public void PreProductProcess()
        {
            // No pre-processing required
        }

        public void PostQueryRoutine()
        {
            // No post-processing required
        }
    }
}
