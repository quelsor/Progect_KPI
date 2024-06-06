using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoExamples.Models;
using System.Collections.Generic;

namespace Progect.Models
{
    public class User
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] 
        public string Id { get; set; }

        [BsonElement("username")] 
        public string UserName { get; set; }
        public string CartId { get; set; }
        public List<string> SelectedItems { get; set; }
        [BsonElement("items")]
        public List<string> Items { get; set; }
        public string PickupLocation { get; set; }
        public string Post { get; set; } 
    }
}
