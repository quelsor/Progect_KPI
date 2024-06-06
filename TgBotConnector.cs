using MongoExamples.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBot
{
    internal class TgBotConnector
    {
        private static string _key;
        private static string _host;
        
        public async Task<Item> Get(string id)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://localhost:5068/api"),
                
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var models = JsonConvert.DeserializeObject<Item>(body);
                return models;
            }
        }
    }
}
