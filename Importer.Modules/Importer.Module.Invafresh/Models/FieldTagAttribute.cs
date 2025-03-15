using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FieldTagAttribute : Attribute
    {
        public string Tag { get; }
        public bool IsRequired { get; }

        public FieldTagAttribute(string tag, bool isRequired = false)
        {
            Tag = tag;
            IsRequired = isRequired;
        }
    }
}
