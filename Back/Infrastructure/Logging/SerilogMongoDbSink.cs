using Back.Api.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Back.Api.Infrastructure.Logging;

public class SerilogMongoDbSink : IBatchedLogEventSink
{
    private static readonly HashSet<string> ExcludedProperties = new()
    {
        "SourceContext",
        "ActionId",
        "ActionName",
        "RequestId",
        "ConnectionId",
        "RequestPath",
        "SpanId",
        "TraceId",
        "ParentId",
        "RequestMethod",
        "RequestScheme",
        "RequestHost",
        "RequestProtocol"
    };

    private readonly IMongoCollection<BsonDocument> _collection;

    public SerilogMongoDbSink(MongoOptions options)
    {
        var client = new MongoClient(options.ConnectionString);
        var db = client.GetDatabase(options.DatabaseName);
        _collection = db.GetCollection<BsonDocument>("serilog_logs");
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var docs = batch.Select(ToDocument).ToList();
        await _collection.InsertManyAsync(docs);
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    private static BsonDocument ToDocument(LogEvent e)
    {
        var doc = new BsonDocument
        {
            ["timestamp"] = e.Timestamp.UtcDateTime,
            ["level"] = e.Level.ToString(),
            ["message"] = e.RenderMessage()
        };

        foreach (var (key, value) in e.Properties)
        {
            if (!ExcludedProperties.Contains(key))
            {
                if (value is ScalarValue scalar)
                    doc[key] = scalar.Value != null
                        ? BsonValue.Create(scalar.Value)
                        : BsonNull.Value;
                else
                    doc[key] = new BsonString(value.ToString());
            }
        }
        return doc;
    }
}
