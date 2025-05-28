using Importer.Common.Helpers;
using Importer.Common.Services;
using Importer.Common.Interfaces;
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

namespace Importer.Common.ImporterTypes
{
    public class RestAPIMonitor : IImporterType, IJob
    {
        public string Name { get; set; } = "APIMonitor";
        public Dictionary<string, object> Settings { get; set; }
        public IImporterModule ImporterModule { get; set; } = null;

        MerchandiserAPIClient merchandiserAPIClient = new MerchandiserAPIClient();

        public void ApplySettings(Dictionary<string, object> settings)
        {
            Settings = settings; // Update the Settings property with the provided settings
            //TODO Apply any other specific settings initialization here
        }

        public RestAPIMonitor(IImporterModule importerModule)
        {
            ImporterModule = importerModule;


            if (Settings == null)
            {
                Settings = new Dictionary<string, object>();
            }

            ApplySettings(Settings);
        }

        public Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }   

        public async Task<string> QueryEndpoint() //WIP using code from ECRSImporter
        {
            string json = null;
            using (var client = merchandiserAPIClient.APIClient)
            {
                try
                {
                    client.BaseAddress = new Uri(Settings["BaseUrl"].ToString());
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Shelf 2 Cart Merchandiser API Client/1.0");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-ECRS-APIKEY", Settings["ApiKey"].ToString());

                    var query = HttpUtility.ParseQueryString(string.Empty);
                    foreach (var param in Settings["QueryParameters"] as Dictionary<string, object>)
                    {
                        query[param.Key] = param.Value.ToString();
                    }

                    string requestUri = $"api/labelDataExport?{query}";

                    requestUri = HttpUtility.UrlDecode(requestUri);

                    Logger.Debug("Sending HTTP request to ECRS API");
                    HttpResponseMessage response = await client.GetAsync(requestUri);

                    Logger.Info($"API Response Status: {response.StatusCode}");

                    string responseContent = await response.Content.ReadAsStringAsync();

                    // setup a settings loader to check if testing is enabled
                    SettingsLoader settings = new SettingsLoader();

                    // write the response content to a file if testing is enabled
                    if (true)
                    {
                        string responseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QueryDataArchive", $"response_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                        // create the directory if it doesn't exist
                        Directory.CreateDirectory(Path.GetDirectoryName(responseFilePath));

                        // only write the response content to file if its not an empty array
                        if (responseContent != "[]")
                        {
                            // write the response content to a file
                            File.WriteAllText(responseFilePath, responseContent);
                            Logger.Info($"Response content written to file: {responseFilePath}");
                        }
                        else
                        {
                            Logger.Info("Response content is an empty array. Not writing to file.");
                        }

                        // clean out old files older than 14 days
                        var files = Directory.GetFiles(Path.GetDirectoryName(responseFilePath), "response_*.json");
                        foreach (var file in files)
                        {
                            if (File.GetCreationTime(file) < DateTime.Now.AddDays(-14))
                            {
                                File.Delete(file);
                                Logger.Info($"Deleted old response file: {file}");
                            }
                        }
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Error($"API request failed. Status: {response.StatusCode}, Content: {responseContent}");
                        json = null;
                    }

                    Logger.Info("API call successful. Data received.");

                    json = responseContent;
                }
                catch (HttpRequestException e)
                {
                    Logger.Error($"HTTP Request Exception: {e.Message}", e);
                    json = null;
                }
                catch (Exception e)
                {
                    Logger.Error($"Unexpected error in QueryECRS: {e.Message}", e);
                    json = null;
                }
            }
            return json;
        }

    }
}
