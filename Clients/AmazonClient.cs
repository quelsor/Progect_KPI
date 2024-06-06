using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Progect.Clients
{
    public class AmazonClient
    {
  
        private static string _key;
        private static string _host;  
        public AmazonClient() 
        { 
            _key = Constants.AmazonKey;
            _host = Constants.AmazonHost;
        }
        public async Task<AmazonModel> Get(string id)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://api.scrapingdog.com/amazon/product?api_key=6659d851be58b621dd588391&domain=com&asin={id}"),
                Headers =
            {
                { "X-RapidAPI-Key", _key},
                { "X-RapidAPI-Host", _host },
            },
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var models = JsonConvert.DeserializeObject<AmazonModel>(body);
                return models;
            }
        }
    }
}
