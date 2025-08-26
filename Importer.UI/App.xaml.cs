using Syncfusion.Licensing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Importer.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Register Syncfusion license
            SyncfusionLicenseProvider.RegisterLicense("MzcxOTU5OUAzMjM4MmUzMDJlMzBNV2JSV2I1dkx4T2VoaFBZNys2U0NkNjVMVTJaTUo2ZDNkelFtTGY1M2NNPQ==");
        }
    }
}
