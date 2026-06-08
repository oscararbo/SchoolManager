using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;
using Back.Api.Persistence.Context;

using MongoDB.Bson;
using MongoDB.Driver;

namespace Back.Api.Persistence.Repositories;

public class AuditLogDomainRepository : IAuditLogDomainRepository
{
    private readonly MongoDbContext context;

    public AuditLogDomainRepository(MongoDbContext context)
    {
        this.context = context;
    }

    private static AdminLogDto ToDto(BsonDocument doc)
    {
        return new AdminLogDto
        {
            Id = doc.TryGetValue("_id", out var id) && id.IsObjectId ? id.AsObjectId.ToString() : id?.ToString() ?? "",
            
            Timestamp = GetDate(doc, "timestamp"),
            Level = GetString(doc, "level") ?? "",
            Message = GetString(doc, "message") ?? "",

            EventType = null,

            Entity = GetString(doc, "Entity"),
            UserEmail = GetString(doc, "UserEmail"),
            UserId = GetString(doc, "UserId"),
            UserRole = GetString(doc, "UserRole"),
            ClientIp = GetString(doc, "ClientIp"),

            Exception = GetString(doc, "Exception")
        };
    }

    private static string? GetString(BsonDocument doc, string key)
    {
        return doc.TryGetValue(key, out var value)
            ? value.AsString
            : null;
    }

    private static DateTime GetDate(BsonDocument doc, string key)
    {
        return doc.TryGetValue(key, out var value) && value.IsValidDateTime
            ? value.ToUniversalTime()
            : DateTime.MinValue;
    }

    public async Task<PaginatedLogsDto> SearchAsync(LogsQueryRequest request, CancellationToken ct = default)
    {
        var builder = Builders<BsonDocument>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrWhiteSpace(request.Level))
            filter &= builder.Eq("level", request.Level);

        if (!string.IsNullOrWhiteSpace(request.Entity))
            filter &= builder.Eq("Entity", request.Entity);

        if (!string.IsNullOrWhiteSpace(request.UserEmail))
            filter &= builder.Eq("UserEmail", request.UserEmail);

        if (request.From.HasValue)
            filter &= builder.Gte("timestamp", request.From.Value);

        if (request.To.HasValue)
            filter &= builder.Lte("timestamp", request.To.Value);

        if (!string.IsNullOrWhiteSpace(request.Query))
            filter &= BuildQueryFilter(request.Query);

        var total = await context.Logs.CountDocumentsAsync(filter, cancellationToken: ct);

        var docs = await context.Logs
            .Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Descending("timestamp"))
            .Skip(request.Page * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        return new PaginatedLogsDto
        {
            Total = total,
            Items = docs.Select(ToDto)
        };
    }

    private static FilterDefinition<BsonDocument> BuildQueryFilter(string query)
    {
        var builder = Builders<BsonDocument>.Filter;

        var parts = query.Split("AND", StringSplitOptions.RemoveEmptyEntries);
        var filters = new List<FilterDefinition<BsonDocument>>();

        foreach (var part in parts)
        {
            var kv = part.Split(':', 2);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim();
            var value = kv[1].Trim();

            filters.Add(builder.Regex(key, new BsonRegularExpression(value, "i")));
        }

        return filters.Count > 0
            ? builder.And(filters)
            : builder.Empty;
    }

    public async Task<IEnumerable<LogsTimelineDto>> GetTimelineAsync(LogsQueryRequest request, CancellationToken ct = default)
    {
        var builder = Builders<BsonDocument>.Filter;
        var match = builder.Empty;

        if (request.From.HasValue)
            match &= builder.Gte("timestamp", request.From.Value);

        if (request.To.HasValue)
            match &= builder.Lte("timestamp", request.To.Value);

        var pipeline = context.Logs.Aggregate()
        .Match(match)
        .AppendStage<BsonDocument>(new BsonDocument("$addFields",
            new BsonDocument("ts",
                new BsonDocument("$convert", new BsonDocument
                {
                    { "input", "$timestamp" },
                    { "to", "date" },
                    { "onError", BsonNull.Value },
                    { "onNull", BsonNull.Value }
                })
            )
        ))
        .AppendStage<BsonDocument>(new BsonDocument("$match",
            new BsonDocument
            {
                { "ts", new BsonDocument("$ne", BsonNull.Value) }
            }
        ))
        .Group(new BsonDocument
        {
            { "_id", new BsonDocument
                {
                    { "year", new BsonDocument("$year", "$ts") },
                    { "month", new BsonDocument("$month", "$ts") },
                    { "day", new BsonDocument("$dayOfMonth", "$ts") },
                    { "hour", new BsonDocument("$hour", "$ts") }
                }
            },

            { "info", new BsonDocument("$sum",
                new BsonDocument("$cond",
                    new BsonArray {
                        new BsonDocument("$eq", new BsonArray { "$level", "Information" }),
                        1, 0
                    })) },

            { "warn", new BsonDocument("$sum",
                new BsonDocument("$cond",
                    new BsonArray {
                        new BsonDocument("$eq", new BsonArray { "$level", "Warning" }),
                        1, 0
                    })) },

            { "error", new BsonDocument("$sum",
                new BsonDocument("$cond",
                    new BsonArray {
                        new BsonDocument("$eq", new BsonArray { "$level", "Error" }),
                        1, 0
                    })) }
        })
        .Sort("{ _id: 1 }");

        var result = await pipeline.ToListAsync(ct);

        return result.Select(x =>
        {
            var id = x["_id"].AsBsonDocument;

            return new LogsTimelineDto
            {
                Bucket = new DateTime(
                    id["year"].AsInt32,
                    id["month"].AsInt32,
                    id["day"].AsInt32,
                    id["hour"].AsInt32,
                    0, 0
                ),

                Info = x["info"].AsInt32,
                Warning = x["warn"].AsInt32,
                Error = x["error"].AsInt32
            };
        });
    }
}