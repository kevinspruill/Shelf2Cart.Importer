using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Models
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ImportDBFieldAttribute : Attribute
    {
        public string Name { get; }
        public ImportDBFieldAttribute(string name)
        {
            Name = name;
        }
    }
}
