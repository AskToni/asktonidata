using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace asktonidata {

    public sealed class MongoHelper {
        private static readonly MongoHelper _mh = new MongoHelper();
        private readonly MongoClient _client = null;
        private readonly IMongoDatabase _db = null;

        MongoHelper() {
            try {
                _client = new MongoClient(ConfigurationManager.Config.GetSetting("Connection_String"));
                if (_client != null) {
                    _db = _client.GetDatabase("asktonidb"); 
                }        
            } catch (Exception ex) {
                throw ex;
            }   
        }

        public static MongoHelper Client {
            get 
            {
                return _mh;
            }
        }

        public IMongoCollection<Restaurant> GetRestaurants() {
            return _mh._db.GetCollection<Restaurant>("restaurants");
        }

        public async Task AddToDatabase(Restaurant e) {
            var collection = _mh._db.GetCollection<Restaurant>("restaurants");
            await collection.InsertOneAsync(e);       
        }

        public async Task AddToDatabase(Review r) {
            var collection = _mh._db.GetCollection<Review>("reviews");
            await collection.InsertOneAsync(r); 
        }
    }
}