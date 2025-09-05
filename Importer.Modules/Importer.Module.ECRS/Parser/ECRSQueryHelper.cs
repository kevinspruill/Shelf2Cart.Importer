using Importer.Common.Helpers;
using Importer.Module.ECRS.Enums;
using Importer.Module.ECRS.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Importer.Module.ECRS.Parser
{
    public class ECRSQueryHelper
    {
        public ECRSConfig Config;

        public ECRSQueryType QueryType = ECRSQueryType.Incremental;
        public ECRSQueryHelper()
        {
            Config = LoadConfiguration();
        }
        public ECRSConfig LoadConfiguration()
        {
            Logger.Debug("Loading ECRS configuration");
            string settingsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
            string configPath = Path.Combine(settingsFolder, "ecrs_config.json");
            if (!File.Exists(configPath))
            {
                Logger.Error($"Configuration file not found. Expected path: {configPath}");
                throw new FileNotFoundException($"Configuration file not found. Expected path: {configPath}");
            }

            string json = File.ReadAllText(configPath);

            string lastFullLoadTime = File.ReadAllText(Path.Combine(settingsFolder, "LastFullLoadTime.ini"));
            if (string.IsNullOrWhiteSpace(lastFullLoadTime))
                lastFullLoadTime = DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ss");

            string lastQueryTime = File.ReadAllText(Path.Combine(settingsFolder, "LastQueryTime.ini"));
            if (string.IsNullOrWhiteSpace(lastQueryTime))
                lastQueryTime = DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ss");

            // Parse JSON with case-insensitive property name matching
            var jObject = JObject.Parse(json, new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore,
                LineInfoHandling = LineInfoHandling.Ignore
            });

            // Log the content of the configuration file (with sensitive information redacted)
            var loggedJson = jObject.DeepClone();
            if (loggedJson["ApiKey"] != null)
            {
                loggedJson["ApiKey"] = "********"; // Redact API key
            }
            Logger.Debug($"Configuration file content: {loggedJson}");

            // Validate required properties (case-insensitive)
            var querySchedule = jObject.Properties().FirstOrDefault(p => p.Name.Equals("querySchedule", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
            var lastFullLoad = lastFullLoadTime;
            var fullLoadIntervalHours = jObject.Properties().FirstOrDefault(p => p.Name.Equals("fullLoadIntervalHours", StringComparison.OrdinalIgnoreCase)).Value.ToObject<int>();
            var baseUrl = jObject.Properties().FirstOrDefault(p => p.Name.Equals("baseUrl", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();
            var apiKey = jObject.Properties().FirstOrDefault(p => p.Name.Equals("apiKey", StringComparison.OrdinalIgnoreCase))?.Value?.ToString();

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
            {
                Logger.Error("Configuration file is missing required properties (BaseUrl or ApiKey)");
                throw new InvalidOperationException("Configuration file is missing required properties");
            }

            var queryParameters = new Dictionary<string, string>();
            // add the since parameter to the query parameters dictionary
            queryParameters.Add("since", lastQueryTime);
            var queryParamsObject = jObject["QueryParameters"] as JObject;
            if (queryParamsObject != null)
            {
                foreach (var property in queryParamsObject.Properties())
                {
                    if (property.Name.Equals("since", StringComparison.OrdinalIgnoreCase) && property.Value.Type == JTokenType.Date)
                    {
                        //queryParameters[property.Name] = DateTime.Parse(lastQueryTime).ToString("yyyy-MM-ddTHH:mm:ss");
                    }
                    else
                    {
                        queryParameters[property.Name] = property.Value?.ToString();
                    }
                }
            }
            else
            {
                Logger.Warn("QueryParameters object is null or not present in the configuration");
            }

            var config = new ECRSConfig
            {
                QuerySchedule = querySchedule,
                LastFullLoad = lastFullLoad,
                FullLoadIntervalHours = fullLoadIntervalHours,
                BaseUrl = baseUrl,
                ApiKey = apiKey,
                QueryParameters = queryParameters
            };

            // If last full load was more than FullLoadIntervalHours hours ago, perform a full load
            if (DateTime.Parse(config.LastFullLoad).AddHours(config.FullLoadIntervalHours) < DateTime.Now)
            {
                QueryType = ECRSQueryType.Full;
                // set since to datetime.minvalue for full loads
                config.QueryParameters["since"] = DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ss");
            }
            else
            {
                QueryType = ECRSQueryType.Incremental;
            }

            Logger.Debug("ECRS configuration loaded successfully");

            return config;

        }
        public async Task<string> QueryECRS()
        {
            string json = null;

            Logger.Info("Starting ECRS query");

            // Trigger a manual load of a file from archive - need to remove timestamp from filename
            if (File.Exists("response.json"))
            {
                json = File.ReadAllText("response.json");
                Logger.Info("Using local response.json file for testing");
                File.Delete("response.json");
            }
            else
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.BaseAddress = new Uri(Config.BaseUrl);
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Shelf 2 Cart Merchandiser ECRS Query Program/1.0");
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.TryAddWithoutValidation("X-ECRS-APIKEY", Config.ApiKey);

                        var query = HttpUtility.ParseQueryString(string.Empty);
                        foreach (var param in Config.QueryParameters)
                        {
                            query[param.Key] = param.Value;
                        }

                        // add -60 minutes to the since parameter for incremental loads
                        if (QueryType == ECRSQueryType.Incremental)
                        {
                            query["since"] = DateTime.Parse(Config.QueryParameters["since"]).AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ss");
                        }

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
            }

            // UpdateQueryRunTime(Config, DateTime.Now);

            return json;

        }
        private void LogApiDetails(HttpClient client, string requestUri)
        {
            Logger.Debug($"API Endpoint: {client.BaseAddress}{requestUri}");
            Logger.Debug($"API Key: {client.DefaultRequestHeaders.GetValues("X-ECRS-APIKEY").FirstOrDefault() ?? "Not set"}");
        }
        public void UpdateQueryRunTime(ECRSConfig ECRS_Config_Class, DateTime queryRunTime)
        {
            try
            {
                Logger.Trace("Updating QueryRunTime");
                // get the path to the config file
                string settingsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
                string lastFullLoadTimePath = Path.Combine(settingsFolder, "LastFullLoadTime.ini");
                string lastQueryTimePath = Path.Combine(settingsFolder, "LastQueryTime.ini");

                // if the query type is full, set the last full load time to the current time
                if (QueryType == ECRSQueryType.Full)
                {
                    Logger.Trace("Updating full load time");
                    File.WriteAllText(lastFullLoadTimePath, queryRunTime.ToString("yyyy-MM-ddTHH:mm:ss"));
                }
                File.WriteAllText(lastQueryTimePath, queryRunTime.ToString("yyyy-MM-ddTHH:mm:ss"));

            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating QueryRunTime - {ex.Message}");
            }
        }
    }
}
