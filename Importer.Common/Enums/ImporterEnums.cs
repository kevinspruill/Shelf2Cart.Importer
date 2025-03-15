using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common
{
    public enum ImporterType
    {
        None,
        File,
        API
    }

    public enum ImporterTrigger 
    { 
        Auto,
        Manual,
        Scheduled
    }
}
