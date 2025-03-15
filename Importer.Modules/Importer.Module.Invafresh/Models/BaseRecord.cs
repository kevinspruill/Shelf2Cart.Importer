using Importer.Module.Invafresh.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    public abstract class BaseRecord
    {
        public CommandCode CommandCode { get; set; }
        public int? StoreNumber { get; set; }  // SNO
        public int DepartmentNumber { get; set; }  // DNO - Required for most records
    }
}
