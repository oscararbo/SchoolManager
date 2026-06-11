using Back.Api.Application.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Back.Api.Persistence.Context;

public class MongoDbContext
{
    private readonly IMongoDatabase database;

    public MongoDbContext(IOptions<ConnectionStringsOptions> connectionOptions, IOptions<MongoOptions> mongoOptions)
    {
        var connections = connectionOptions.Value;
        var config = mongoOptions.Value;

        var client = new MongoClient(connections.MongoConnection);
        database = client.GetDatabase(config.DatabaseName);

        EnsureIndexes();
    }

    public IMongoCollection<BsonDocument> Logs =>
        database.GetCollection<BsonDocument>("serilog_logs");

    private void EnsureIndexes()
    {
        var timestampIndex = Builders<BsonDocument>
            .IndexKeys
            .Descending("timestamp");

        Logs.Indexes.CreateOne(
            new CreateIndexModel<BsonDocument>(timestampIndex)
        );
    }
}