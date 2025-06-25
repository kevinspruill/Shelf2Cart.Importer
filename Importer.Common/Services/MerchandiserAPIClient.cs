using Importer.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Services

{
    public class MerchandiserAPIClient : IDisposable
    {
        public HttpClient APIClient;

        public MerchandiserAPIClient()
        {
            APIClient = new HttpClient();
            SetHeaders();
        }

        /// <summary>
        /// TODO Will Override SetHeaders based on the SchedulerService when we import ECRS here
        /// </summary>
        public void SetHeaders()
        {
            APIClient.DefaultRequestHeaders.Accept.Clear();
            APIClient.DefaultRequestHeaders.UserAgent.ParseAdd("Merchandiser API Client/1.0");
            APIClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        ///  Helper method to set a specific header for the API client.
        ///  To use: Example: SetHeader("X-ECRS-APIKEY", "your_api_key");
        ///  To remove a header, call SetHeader with the same key and an empty value.
        /// </summary>
        /// <param name="key"> The header key to set.</param>
        /// <param name="value"> The value for the header.</param>
        public void SetHeader(string key, string value)
        {
            if (APIClient.DefaultRequestHeaders.Contains(key))
            {
                APIClient.DefaultRequestHeaders.Remove(key);
            }

            if (!string.IsNullOrEmpty(value))
            {
                APIClient.DefaultRequestHeaders.Add(key, value);
            }
        }

        public async Task<string> GetAsync(string endpoint)
        {
            try
            {
                var response = await APIClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error fetching menu item details from {endpoint}: {ex.Message} - {ex.InnerException.Message}");
                return null;
            }
        }

        // Overloaded GetAsync method to handle query parameters
        public async Task<string> GetAsync(string endpoint, Dictionary<string, string> queryParams)
        {
            try
            {
                var query = new FormUrlEncodedContent(queryParams);
                var queryString = await query.ReadAsStringAsync();
                var response = await APIClient.GetAsync($"{endpoint}?{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Logger.Error($"HTTP Request Exception: {e.Message}", e);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error fetching data from {endpoint} with query parameters: {ex.Message}");
                return null;
            }
        }

        public async Task<string> PostAsync(string endpoint, HttpContent content)
        {
            try
            {
                var response = await APIClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error posting to {endpoint}: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            if (APIClient != null)
            {
                APIClient.Dispose();
                APIClient = null;
            }
        }
    }
}
