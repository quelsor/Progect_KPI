using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Progect.Clients;
using Progect.Models;
using System.Threading.Tasks;

namespace Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModelController : ControllerBase
    {
        private readonly ILogger<ModelController> _logger;

        public ModelController(ILogger<ModelController> logger)
        {
            _logger = logger;
        }

        [HttpGet("item/{id}")]
        public async Task<string> Item(string id) // id example B008IGB0QQ
        {
            AmazonClient shopClient = new AmazonClient();
            AmazonModel model = await shopClient.Get(id);

            string itemName = $"{model.images[0]}\n{model.title} - \n{model.price}";

            return itemName;
        }

        [HttpGet("itemcart/{id}")]
        public async Task<string> ItemCart(string id) // id example B008IGB0QQ
        {
            AmazonClient shopClient = new AmazonClient();
            AmazonModel model = await shopClient.Get(id);

            string itemName = $"{model.title} ";

            return itemName;
        }

        [HttpGet("iteminfo/{id}")]
        public async Task<string> ItemInfo(string id) // id example B008IGB0QQ
        {
            AmazonClient shopClient = new AmazonClient();
            AmazonModel model = await shopClient.Get(id);

            string itemInfo = $"{model.description}\n{model.images[1]}";
            return itemInfo;
        }

        [HttpGet("distance")]
        public async Task<float> Distance(string location1, string location2)
        {
            DistanceClient shopClient = new DistanceClient();
            DistanceModel model = await shopClient.Get(location1, location2);
            return model.distance;
        }

        [HttpPost("calculatedeliverycost")]
        public double CalculateDeliveryCost([FromQuery] double distance, [FromQuery] double weight)
        {
            double baseCost = 5.0;
            double costPerKm = 0.01;
            double costPerKg = 0.5;

            double distanceCost = distance * costPerKm;
            double weightCost = weight * costPerKg;

            double totalCost = baseCost + distanceCost + weightCost;
            return Math.Round(totalCost, 2);
        }
    }
}
