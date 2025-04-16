using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Interfaces
{
    public interface IImporterType
    {
        string Name { get; set; }
        Dictionary<string, object> Settings { get; set; }
        void ApplySettings(Dictionary<string, object> settings);
    }
}
