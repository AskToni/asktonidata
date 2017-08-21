using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;
using System.Linq;
using MongoDB.Driver;

namespace asktonidata {
    
    public sealed class DataClient : HttpClient {

        private static readonly DataClient _dc = new DataClient();
        private int pageOffset = 0;
        private int pageLimit = 50;
        private int numberResults = 15000;

        private bool headersAdded = false;

        DataClient() {
        }

        public static DataClient Dc {
            
            get 
            {
                return _dc;
            }
        }

        // Get list of restaurants and store in restaurants collection
        public void MineYelpData() {
            //TODO: Implement smarter way to get number of results instead of manual entry
            while (pageOffset < numberResults) {
                try {
                    GetPageOfResults(pageOffset).Wait();
                } catch (Exception ex) {
                    Console.WriteLine("Page request failed at offset: " + pageOffset);
                    Console.WriteLine(ex.Message);
                }
                pageOffset += pageLimit;
            }
        }

        // Get one page of restaurant results
        // Offset is incremented to iterate through pages of results
        public async Task GetPageOfResults(int offset)
        {
            // TODO: Initialize this with constructor instead of putting it here
            if (!headersAdded) {
                _dc.DefaultRequestHeaders.Accept.Clear();
                _dc.DefaultRequestHeaders.Add("Authorization", "Bearer " + ConfigurationManager.Config.GetSetting("Token"));
                headersAdded = true;
            }
            
            var httpRequest= _dc.GetStringAsync("https://api.yelp.com/v3/businesses/search?location=vancouver&limit=50&offset=" + offset);

            var response = await httpRequest;
            
            JObject json= JObject.Parse(response);

            JToken businesses = json.SelectToken("businesses");

            foreach (JToken b in businesses) {
                string[] categories = b["categories"].Children()["title"].Select(s => (string) s).ToArray();
                try {
                    MongoHelper.Client.AddToDatabase(new Restaurant() { Id = ObjectId.GenerateNewId(), RestaurantName = b["name"].ToString(),
                                                                    Categories = categories, ReviewCount = (int) b["review_count"],
                                                                    Rating = (double) b["rating"], Price = b["price"].ToString(), Address = b["location"]["address1"].ToString(), 
                                                                    City = b["location"]["city"].ToString(), RestaurantId = b["id"].ToString(), Phone = b["phone"].ToString()}).Wait();
                } catch (Exception ex) {
                    Console.WriteLine("Something went wrong with: " + b["name"].ToString());
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // Loop through all the businesses in the restaurants collection
        // Sends REST calls to Yelp Reviews endpoint for each business
        // Stores review documents in the reviews collection
        public async Task GetReviewsFromBusinessData() {
            
            var collection = MongoHelper.Client.GetRestaurants();
            using (IAsyncCursor<Restaurant> cursor = await collection.FindAsync(new BsonDocument()))
            {
                while (await cursor.MoveNextAsync())
                {
                    IEnumerable<Restaurant> batch = cursor.Current;
                    foreach (Restaurant r in batch)
                    {
                        await GetReviews(r.RestaurantId, r.RestaurantName);
                    }
                }
            }
        }
        
        // Get reviews for a restaurant given a restaurantId and restaurantName
        public async Task GetReviews(string restaurantId, string restaurantName) {
            // TODO: Initialize this with constructor instead of putting it here
            if (!headersAdded) {
                _dc.DefaultRequestHeaders.Accept.Clear();
                _dc.DefaultRequestHeaders.Add("Authorization", "Bearer " + ConfigurationManager.Config.GetSetting("Token"));
                headersAdded = true;
            }
            var httpRequest= _dc.GetStringAsync("https://api.yelp.com/v3/businesses/" + restaurantId + "/reviews");
            var response = await httpRequest;

            JObject json= JObject.Parse(response);

            JToken reviews = json.SelectToken("reviews");

            foreach (JToken r in reviews) {
                try {
                    MongoHelper.Client.AddToDatabase(new Review() { Id = ObjectId.GenerateNewId(), RestaurantId = restaurantId,
                                                                    Text = r["text"].ToString(), Rating = (double) r["rating"],
                                                                    TimeCreated = (DateTime) r["time_created"], RestaurantName = restaurantName
                                                                    }).Wait();
                } catch (Exception ex) {
                    Console.WriteLine("Something went wrong with: " + restaurantId);
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // Get Yelp API token by querying the token endpoint
        // Uses client ID and client secret to authenticate
        public async Task GetToken() {

            var param = new Dictionary<string, string> { { "client_id", ConfigurationManager.Config.GetSetting("Client_ID") },
                        { "client_secret", ConfigurationManager.Config.GetSetting("Client_Secret") }, {"grant_type", ConfigurationManager.Config.GetSetting("Grant_Type")} };
            var encodedContent = new FormUrlEncodedContent (param);

            _dc.DefaultRequestHeaders.Accept.Clear();
            var httpRequest = _dc.PostAsync(ConfigurationManager.Config.GetSetting("Token_Endpoint"),encodedContent);
            
            var response = await httpRequest;
            
            if (response.StatusCode == HttpStatusCode.OK) {
                var content = await response.Content.ReadAsStringAsync();
                
                JObject json = (JObject) JsonConvert.DeserializeObject(content);
                Console.WriteLine(json);
                //TODO: Store the token
                //ConfigurationManager.Config.UpdateSetting("Token",json["access_token"].ToString());
            }
        }

    }
    
}