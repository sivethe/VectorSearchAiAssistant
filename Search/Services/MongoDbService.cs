namespace Search.Services
{
    using MongoDB.Bson;
    using MongoDB.Driver;
    using Search.Models;
    using Search.Utilities;
    using StackExchange.Redis;

    /// <summary>
    /// Service to access Azure Cosmos DB for Mongo vCore.
    /// </summary>
    public class MongoDbService
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of the service.
        /// </summary>
        /// <param name="endpoint">Endpoint URI.</param>
        /// <param name="key">Account key.</param>
        /// <param name="databaseName">Name of the database to access.</param>
        /// <param name="containerNames">Names of the containers to access.</param>
        /// <exception cref="ArgumentNullException">Thrown when endpoint, key, databaseName, or containerNames is either null or empty.</exception>
        /// <remarks>
        /// This constructor will validate credentials and create a service client instance.
        /// </remarks>
        public MongoDbService(string connection, string databaseName, string collectionName, ILogger logger)
        {
            ArgumentException.ThrowIfNullOrEmpty(connection);
            ArgumentException.ThrowIfNullOrEmpty(databaseName);
            ArgumentException.ThrowIfNullOrEmpty(collectionName);

            _logger = logger;

            _client = new MongoClient(connection);
            _database = _client.GetDatabase(databaseName);
            _collection = _database.GetCollection<BsonDocument>(collectionName);
        }

        public async Task<string> VectorSearchAsync(float[] embeddings, int maxResults = 100)
        {
            List<string> retDocs = new List<string>();
            var memory = new ReadOnlyMemory<float>(embeddings);

            //Search Mongo vCore collection for similar embeddings
            BsonDocument[] pipeline = new BsonDocument[]
            {
                BsonDocument.Parse($"{{$search: {{cosmosSearch: {{ vector: [{string.Join(',', embeddings)}], path: 'vector', k: {maxResults}}}, returnStoredSource:true}}}}"),
            };

            List<BsonDocument> result = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            string resultDocuments = string.Join(Environment.NewLine + "-", result.Select(x => x.ToJson()));
            return resultDocuments;
        }
    }
}