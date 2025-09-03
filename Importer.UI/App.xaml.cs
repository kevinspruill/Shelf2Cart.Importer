using DryIoc;
using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.UI.ViewModels;
using Importer.UI.Views;
using Newtonsoft.Json;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Syncfusion.Licensing;
using System.Windows;

namespace Importer.UI
{
    /// <summary>
    /// Prism Bootstrapper for the Importer UI Application
    /// </summary>
    public partial class App : PrismApplication
    {
        public App()
        {
            // Register Syncfusion License 
            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cXmRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdcdnVWRGJcVU1xV0tWYEk=");
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindowView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register services

            // Register ViewModels for navigation
            containerRegistry.RegisterForNavigation<InstanceManagerView>();
            containerRegistry.RegisterForNavigation<ConsoleView, ConsoleViewModel>();
            containerRegistry.RegisterForNavigation<ImporterSettingsView>();
            containerRegistry.RegisterForNavigation<DefaultValuesView>();
            containerRegistry.RegisterForNavigation<ProcessDatabaseSettingsView>();
            containerRegistry.RegisterForNavigation<GenericSettingsView>();
            containerRegistry.RegisterForNavigation<InvafreshSettingsView>();
            containerRegistry.RegisterForNavigation<AdminSettingsView>();

            // Register dialogs
            // containerRegistry.RegisterDialog<ImporterInstanceEditDialog, ImporterInstanceEditDialogViewModel>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            // Add modules if needed
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // region navigate to initial view
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("ContentRegion", "InstanceManagerView");

        }
    }
}

