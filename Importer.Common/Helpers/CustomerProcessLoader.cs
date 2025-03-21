using Importer.Common.Interfaces;
using Importer.Common.Modifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Helpers
{
    public static class CustomerProcessLoader
    {
        static ICustomerProcess _customerProcess;
        public static ICustomerProcess GetCustomerProcess()
        {
            // Look for *.dll files in the current directory
            var dlls = System.IO.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*CustomerProcess.dll");
            foreach (var dll in dlls)
            {
                // Load the assembly
                var assembly = System.Reflection.Assembly.LoadFile(dll);
                // Look for Type of ICustomerProcess
                var type = assembly.GetTypes().FirstOrDefault(t => typeof(ICustomerProcess).IsAssignableFrom(t));
                if (type != null)
                {
                    // Create an instance of the type
                    _customerProcess = (ICustomerProcess)Activator.CreateInstance(type);
                    break;
                }
            }

            // If no ICustomerProcess implementation was found, use the default implementation
            if (_customerProcess == null)
            {
                _customerProcess = new BaseProcess();
            }

            return _customerProcess;
        }
    }
}
