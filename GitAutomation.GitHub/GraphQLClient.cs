using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GitHub
{
    public class GraphQLClient
    {
        private readonly HttpClient client;
        public GraphQLClient(HttpClient client)
        {
            this.client = client;
        }

        public async Task<JObject> Query(string query, object variables)
        {
            using (var response = await client.PostAsync("", JsonContent(new
            {
                query = query,
                variables = variables,
            })))
            {
                await EnsureSuccessStatusCode(response);
                var content = await response.Content.ReadAsStringAsync();
                var jobject = JObject.Parse(content);
                return jobject["data"] as JObject;
            }
        }

        private static HttpContent JsonContent<TModel>(TModel model)
        {
            var json = JsonConvert.SerializeObject(model);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task EnsureSuccessStatusCode(HttpResponseMessage response)
        {
            if ((int)response.StatusCode < 200 || (int)response.StatusCode > 299)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception("Status code: " + response.StatusCode)
                {
                    Data = { { "Response", JsonConvert.DeserializeObject(content) } }
                };
            }
        }
    }

}
