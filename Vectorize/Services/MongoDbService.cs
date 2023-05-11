﻿using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Vectorize.Services
{
    public class MongoDbService
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;


        public MongoDbService(string connection, string databaseName, string collectionName, ILogger logger)
        {

            _client = new MongoClient(connection);
            _database = _client.GetDatabase(databaseName);
            _collection = _database.GetCollection<BsonDocument>(collectionName);
            
            //Find if vector index exists
            using (IAsyncCursor<BsonDocument> indexCursor = _collection.Indexes.List())
            {
                bool vectorIndexExists = indexCursor.ToList().Any(x => x["name"] == "vectorSearchIndex");
                if (!vectorIndexExists)                
                {
                    BsonDocumentCommand<BsonDocument> command = new BsonDocumentCommand<BsonDocument>(
                    BsonDocument.Parse(@"
                        { createIndexes: 'vectors', 
                          indexes: [{ 
                            name: 'vectorSearchIndex', 
                            key: { vector: 'cosmosSearch' }, 
                            cosmosSearchOptions: { kind: 'vector-ivf', numLists: 5, similarity: 'COS', dimensions: 3 } 
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

        
        public async Task UpsertVector(dynamic document, Microsoft.Extensions.Logging.ILogger logger)
        {

            try 
            { 
                BsonDocument doc = document.ToBsonDocument();

                await _collection.InsertOneAsync(doc);

            }
            catch (Exception ex) 
            {
                logger.LogError(ex.Message);
            }

        }


    }
}
