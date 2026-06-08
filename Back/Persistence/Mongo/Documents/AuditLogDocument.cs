using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Back.Api.Persistence.Mongo.Documents;

public class AuditLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("level")]
    public string Level { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("Entity")]
    public string? Entity { get; set; }

    [BsonElement("UserEmail")]
    public string? UserEmail { get; set; }

    [BsonElement("Exception")]
    public string? Exception { get; set; }
}