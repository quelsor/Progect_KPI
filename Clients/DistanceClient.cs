using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using Progect.Models;

namespace Progect.Clients
{
    public class DistanceClient
    {
        private static string _key;
        private static string _host;
        public DistanceClient()
        {
            _key = Constants.DistanceKey;
            _host = Constants.DistanceHost;
        }

        public async Task<DistanceModel> Get(string location1, string location2)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://distance-api3.p.rapidapi.com/distance?location1={location1}&location2={location2}"),
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
                var models = JsonConvert.DeserializeObject<DistanceModel>(body);
                return models;
            }
        }
    }
}
