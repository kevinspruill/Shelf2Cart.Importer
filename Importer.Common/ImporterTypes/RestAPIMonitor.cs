using Importer.Common.Helpers;
using Importer.Common.Interfaces;
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
    public class RestAPIMonitor : IImporterType
    {
        public string Name { get; set; } = "APIMonitor";
        public Dictionary<string, object> Settings { get; set; }

        public IImporterModule ImporterModule { get; set; } = null;

        public void ApplySettings(Dictionary<string, object> settings)
        {
            //TODO Apply Settings
        }

        public RestAPIMonitor(IImporterModule importerModule)
        {
            ImporterModule = importerModule;

            ApplySettings(Settings);
        }

        public async Task<string> QueryEndpoint() //WIP using code from ECRSImporter
        {
            string json = null;
            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(Settings.BaseUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Shelf 2 Cart Merchandiser ECRS Query Program/1.0");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-ECRS-APIKEY", Settings.ApiKey);

                    var query = HttpUtility.ParseQueryString(string.Empty);
                    foreach (var param in Settings.QueryParameters)
                    {
                        query[param.Key] = param.Value;
                    }

                    //// add -60 minutes to the since parameter for incremental loads
                    //if (QueryType == ECRSQueryType.Incremental)
                    //{
                    //    query["since"] = DateTime.Parse(Settings.QueryParameters["since"]).AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ss");
                    //}

                    string requestUri = $"api/labelDataExport?{query}";

                    requestUri = HttpUtility.UrlDecode(requestUri);

                    LogApiDetails(client, requestUri);

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
        private void LogApiDetails(HttpClient client, string requestUri)
        {
            Logger.Debug($"API Endpoint: {client.BaseAddress}{requestUri}");
            Logger.Debug($"API Key: {client.DefaultRequestHeaders.GetValues("X-ECRS-APIKEY").FirstOrDefault() ?? "Not set"}");
        }
    }
}
