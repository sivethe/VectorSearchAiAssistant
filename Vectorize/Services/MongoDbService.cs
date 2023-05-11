using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Vectorize.Models;

namespace Vectorize.Services
{
    public class MongoDbService
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ILogger _logger;

        public MongoDbService(string connection, string databaseName, string collectionName, ILogger logger)
        {

            _client = new MongoClient(connection);
            _database = _client.GetDatabase(databaseName);
            _collection = _database.GetCollection<BsonDocument>(collectionName);
            _logger = logger;
            
            //Find if vector index exists
            using (IAsyncCursor<BsonDocument> indexCursor = _collection.Indexes.List())
            {
                bool vectorIndexExists = indexCursor.ToList().Any(x => x["name"] == "vectorSearchIndex");
                if (!vectorIndexExists)                
                {
                    logger.LogInformation("Creating vector index");
                    BsonDocumentCommand<BsonDocument> command = new BsonDocumentCommand<BsonDocument>(
                    BsonDocument.Parse(@"
                        { createIndexes: 'vectors', 
                          indexes: [{ 
                            name: 'vectorSearchIndex', 
                            key: { vector: 'cosmosSearch' }, 
                            cosmosSearchOptions: { kind: 'vector-ivf', numLists: 5, similarity: 'COS', dimensions: 1536 } 
                          }] 
                        }"));

                    BsonDocument result = _database.RunCommand(command);
                    if (result["ok"] != 1)
                    {
                        logger.LogError("CreateIndex failed with response: " + result.ToJson());
                    }
                }
            }
        }

        
        public async Task InsertVector(BsonDocument document)
        {
            if (!document.Contains("_id"))
            {
                _logger.LogError("Document does not contain _id.");
                throw new ArgumentException("Document does not contain _id.");
            }

            string? _idValue =  document.GetValue("_id").ToString();

            try 
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", _idValue);
                var options = new ReplaceOptions { IsUpsert = true };
                await _collection.ReplaceOneAsync(filter, document, options);
            }
            catch (Exception ex) 
            {
                //TODO: fix the logger. Output does not show up anywhere
                _logger.LogError(ex.Message);
            }
        }
    }

    public class JsonToBsonSerializer : SerializerBase<dynamic>
    {
        public override dynamic Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return BsonSerializer.Deserialize<dynamic>(context.Reader);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, dynamic value)
        {
            var bsonDocument = new BsonDocument();
            BsonSerializer.Serialize(context.Writer, value.GetType(), value);

            bsonDocument.AddRange(value.ToBsonDocument());

            if (value.Id != ObjectId.Empty)
            {
                bsonDocument["_id"] = value.id;
            }

            context.Writer.WriteEndDocument();
        }
    }
}
