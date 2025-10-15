using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Registries;
using Importer.Common.Services;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Parsley.Jobs
{
    public class QueryParsleyAPI : IJob
    {
        public Dictionary<string, object> Settings { get; set; }
        public IImporterModule ImporterModule { get; set; } = null;
        public string DataRetrieved { get; set; } = null;

        MerchandiserAPIClient merchandiserAPIClient = new MerchandiserAPIClient();

        public async Task Execute(IJobExecutionContext context)
        {
            Logger.Info("Executing QueryParsleyAPI Job...");
            try
            {
                // Retrieve the module reference
                var moduleKey = context.JobDetail.JobDataMap.GetString("ImporterModuleKey");
                var endpoint = context.JobDetail.JobDataMap.GetString("Endpoint");
                var apiKey = context.JobDetail.JobDataMap.GetString("ApiKey");

                // Validate scheduler inputs
                if (string.IsNullOrWhiteSpace(moduleKey))
                {
                    Logger.Error("QueryParsleyAPI: Missing 'ImporterModuleKey' in JobDataMap.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    Logger.Error("QueryParsleyAPI: Missing 'Endpoint' in JobDataMap.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Logger.Error("QueryParsleyAPI: Missing 'ApiKey' in JobDataMap.");
                    return;
                }

                // Set API Key
                if (merchandiserAPIClient?.APIClient == null)
                {
                    Logger.Error("QueryParsleyAPI: API client is not initialized.");
                    return;
                }
                merchandiserAPIClient.APIClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                if (ImporterModuleRegistry.Modules.TryGetValue(moduleKey, out var module))
                {
                    // Fetch data
                    var result = await merchandiserAPIClient.GetAsync(endpoint);
                    if (!string.IsNullOrEmpty(result))
                    {
                        // Pass data back to the module
                        module.ImporterTypeData = result;

                        // Trigger the process in the module
                        await module.TriggerProcess();

                        Logger.Info("QueryParsleyAPI Job executed successfully.");
                    }
                    else
                    {
                        Logger.Error("QueryParsleyAPI: Empty/null value returned from GetAsync; error retrieving data.");
                    }
                }
                else
                {
                    Logger.Error($"QueryParsleyAPI: No module found for key '{moduleKey}'.");
                }
            }
            catch (Exception ex)
            {
                // Avoid NullReferenceException when InnerException is null
                Logger.Error($"Error executing scheduled QueryParsleyAPI Job - {ex}");
            }
        }
    }
}
