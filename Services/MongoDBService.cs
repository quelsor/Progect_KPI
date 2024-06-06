using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoExamples.Models;
using Progect.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoExamples.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<User> _userCollection;

        public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            MongoClient client = new MongoClient(mongoDBSettings.Value.ConnectionURI);

            IMongoDatabase database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _userCollection = database.GetCollection<User>(mongoDBSettings.Value.CollectionName);
        }

        public async Task CreateAsync(User user)
        {
            await _userCollection.InsertOneAsync(user);
        }

        public async Task<List<User>> GetAsync()
        {
            return await _userCollection.Find(_ => true).ToListAsync();
        }

        public async Task AddToCartAsync(string id, string itemId)
        {
            var filter = Builders<User>.Filter.Eq("_id", ObjectId.Parse(id));
            var update = Builders<User>.Update.Push<string>("SelectedItems", itemId);
            await _userCollection.UpdateOneAsync(filter, update);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<User>.Filter.Eq("_id", ObjectId.Parse(id));
            await _userCollection.DeleteOneAsync(filter);
        }
    }
}
