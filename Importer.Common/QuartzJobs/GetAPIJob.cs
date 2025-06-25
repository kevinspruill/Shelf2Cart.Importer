using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Registries;
using Importer.Common.Services;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Importer.Common.QuartzJobs
{
    public class GetAPIJob : IJob
    {
        public Dictionary<string, object> Settings { get; set; }
        public IImporterModule ImporterModule { get; set; } = null;

        public string DataRetrieved { get; set; } = null;

        MerchandiserAPIClient merchandiserAPIClient = new MerchandiserAPIClient();

        public void ApplySettings(Dictionary<string, object> settings)
        {
            Settings = settings; // Update the Settings property with the provided settings
            //TODO Apply any other specific settings initialization here
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Logger.Info("Executing GetAPIJob...");
            try
            {
                // Retrieve the module reference
                var moduleKey = context.JobDetail.JobDataMap.GetString("ImporterModuleKey");
                var endpoint = context.JobDetail.JobDataMap.GetString("Endpoint");
                var apiKey = context.JobDetail.JobDataMap.GetString("ApiKey");
                //Set API Key
                merchandiserAPIClient.APIClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                if (ImporterModuleRegistry.Modules.TryGetValue(moduleKey, out var module))
                {
                    // Fetch data
                    var result = await merchandiserAPIClient.GetAsync(endpoint);
                    if (result != null)
                    {
                        // Pass data back to the module
                        module.ImporterTypeData = result; // or call a custom method if needed

                        // Trigger the process in the module
                        module.TriggerProcess(); // Trigger the process in the module

                        Logger.Info("GetAPIJob executed successfully.");
                    }
                    else
                    {
                        Logger.Error("Null value returned from GetAsync, error retrieving data");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing scheduled GetAPIJob - {ex.Message} - {ex.InnerException.Message}");
            }
        }
    }
}
