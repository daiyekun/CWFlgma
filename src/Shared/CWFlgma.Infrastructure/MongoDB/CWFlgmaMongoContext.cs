using MongoDB.Driver;
using CWFlgma.Infrastructure.MongoDB.Documents;

namespace CWFlgma.Infrastructure.MongoDB;

public class CWFlgmaMongoContext
{
    private readonly IMongoDatabase _database;

    public CWFlgmaMongoContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<DesignDocument> Designs => _database.GetCollection<DesignDocument>("designs");
    public IMongoCollection<OperationHistory> OperationHistory => _database.GetCollection<OperationHistory>("operation_history");
    public IMongoCollection<CollaborationSession> CollaborationSessions => _database.GetCollection<CollaborationSession>("collaboration_sessions");

    public async Task InitializeIndexesAsync()
    {
        // DesignDocument indexes
        var designIndexKeys = Builders<DesignDocument>.IndexKeys.Ascending(d => d.DocumentId);
        var designIndexOptions = new CreateIndexOptions { Unique = true };
        await Designs.Indexes.CreateOneAsync(new CreateIndexModel<DesignDocument>(designIndexKeys, designIndexOptions));

        // OperationHistory indexes
        var opHistoryIndexKeys = Builders<OperationHistory>.IndexKeys
            .Ascending(o => o.DocumentId)
            .Descending(o => o.Timestamp);
        await OperationHistory.Indexes.CreateOneAsync(new CreateIndexModel<OperationHistory>(opHistoryIndexKeys));

        var opSeqIndexKeys = Builders<OperationHistory>.IndexKeys
            .Ascending(o => o.DocumentId)
            .Ascending(o => o.SequenceNumber);
        await OperationHistory.Indexes.CreateOneAsync(new CreateIndexModel<OperationHistory>(opSeqIndexKeys));

        // CollaborationSession indexes
        var sessionDocIndexKeys = Builders<CollaborationSession>.IndexKeys.Ascending(s => s.DocumentId);
        await CollaborationSessions.Indexes.CreateOneAsync(new CreateIndexModel<CollaborationSession>(sessionDocIndexKeys));

        var sessionUserIndexKeys = Builders<CollaborationSession>.IndexKeys.Ascending(s => s.UserId);
        await CollaborationSessions.Indexes.CreateOneAsync(new CreateIndexModel<CollaborationSession>(sessionUserIndexKeys));

        var sessionActivityIndexKeys = Builders<CollaborationSession>.IndexKeys.Ascending(s => s.LastActivity);
        var sessionActivityOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromHours(1) };
        await CollaborationSessions.Indexes.CreateOneAsync(new CreateIndexModel<CollaborationSession>(sessionActivityIndexKeys, sessionActivityOptions));
    }
}
