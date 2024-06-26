﻿namespace Aoxe.Mongo;

public class AoxeMongoClient : IAoxeMongoClient
{
    private readonly MongoCollectionSettings _collectionSettings;
    private readonly GuidSerializer _guidSerializer;
    private readonly ConcurrentDictionary<Type, string> _tableNames = new();
    private readonly ConcurrentDictionary<Type, PropertyInfo> _idProperties = new();

    public IMongoDatabase MongoDatabase { get; }

    public AoxeMongoClient(AoxeMongoOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        if (options.MongoClientSettings is null)
            throw new ArgumentNullException(nameof(options.MongoClientSettings));
        var guidSerializer = new GuidSerializer(options.GuidRepresentation);
        BsonSerializer.RegisterSerializer(guidSerializer);
        _guidSerializer = guidSerializer;
        ConventionRegistry.Register(
            "IgnoreExtraElements",
            new ConventionPack { new IgnoreExtraElementsConvention(true) },
            _ => true
        );
        var serializer = new DateTimeSerializer(options.DateTimeKind, BsonType.DateTime);
        BsonSerializer.RegisterSerializer(typeof(DateTime), serializer);
        _collectionSettings = new MongoCollectionSettings { AssignIdOnInsert = true };
        MongoDatabase = new MongoClient(options.MongoClientSettings).GetDatabase(options.Database);
    }

    public IQueryable<T> GetQueryable<T>()
        where T : class
    {
        var tableName = GetTableName(typeof(T));
        return MongoDatabase.GetCollection<T>(tableName, _collectionSettings).AsQueryable();
    }

    public void Add<T>(T entity)
        where T : class
    {
        var tableName = GetTableName(typeof(T));
        MongoDatabase.GetCollection<T>(tableName, _collectionSettings).InsertOne(entity);
    }

    public async ValueTask AddAsync<T>(T entity)
        where T : class
    {
        var tableName = GetTableName(typeof(T));
        await MongoDatabase.GetCollection<T>(tableName, _collectionSettings).InsertOneAsync(entity);
    }

    public void AddRange<T>(IEnumerable<T> entities)
        where T : class
    {
        var tableName = GetTableName(typeof(T));
        MongoDatabase.GetCollection<T>(tableName, _collectionSettings).InsertMany(entities);
    }

    public async ValueTask AddRangeAsync<T>(IEnumerable<T> entities)
        where T : class
    {
        var tableName = GetTableName(typeof(T));
        await MongoDatabase
            .GetCollection<T>(tableName, _collectionSettings)
            .InsertManyAsync(entities);
    }

    public long Delete<T>(T entity)
        where T : class
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var tableName = GetTableName(typeof(T));
        var collection = MongoDatabase.GetCollection<T>(tableName, _collectionSettings);

        var filter = GetJsonFilterDefinition(entity, _guidSerializer);

        var result = collection.DeleteOne(filter);

        return result.DeletedCount;
    }

    public async ValueTask<long> DeleteAsync<T>(T entity)
        where T : class
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var tableName = GetTableName(typeof(T));
        var collection = MongoDatabase.GetCollection<T>(tableName, _collectionSettings);

        var filter = GetJsonFilterDefinition(entity, _guidSerializer);

        var result = await collection.DeleteOneAsync(filter);

        return result.DeletedCount;
    }

    public long Delete<T>(Expression<Func<T, bool>> where)
        where T : class
    {
        if (where is null)
            throw new ArgumentNullException(nameof(where));
        var tableName = GetTableName(typeof(T));
        var collection = MongoDatabase.GetCollection<T>(tableName, _collectionSettings);
        return collection.DeleteMany(where).DeletedCount;
    }

    public async ValueTask<long> DeleteAsync<T>(Expression<Func<T, bool>> where)
        where T : class
    {
        if (where is null)
            throw new ArgumentNullException(nameof(where));
        var tableName = GetTableName(typeof(T));
        var collection = MongoDatabase.GetCollection<T>(tableName, _collectionSettings);
        var result = await collection.DeleteManyAsync(where);
        return result.DeletedCount;
    }

    public long Update<T>(T entity)
        where T : class
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var tableName = GetTableName(typeof(T));
        var collection = MongoDatabase.GetCollection<T>(tableName, _collectionSettings);

        var filter = GetJsonFilterDefinition(entity, _guidSerializer);

        var result = collection.UpdateOne(
            filter,
            new BsonDocumentUpdateDefinition<T>(
                new BsonDocument { { "$set", entity.ToBsonDocument() } }
            )
        );

        return result.ModifiedCount;
    }

    public async ValueTask<long> UpdateAsync<T>(T entity)
        where T : class
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var tableName = GetTableName(typeof(T));
        var collection = MongoDatabase.GetCollection<T>(tableName, _collectionSettings);

        var filter = GetJsonFilterDefinition(entity, _guidSerializer);

        var result = await collection.UpdateOneAsync(
            filter,
            new BsonDocumentUpdateDefinition<T>(
                new BsonDocument { { "$set", entity.ToBsonDocument() } }
            )
        );

        return result.ModifiedCount;
    }

    public long Update<T>(Expression<Func<T>> update, Expression<Func<T, bool>> where)
        where T : class
    {
        if (update is null)
            throw new ArgumentNullException(nameof(update));
        if (where is null)
            throw new ArgumentNullException(nameof(where));

        var tableName = GetTableName(typeof(T));
        var collection = MongoDatabase.GetCollection<T>(tableName, _collectionSettings);

        var result = collection.UpdateMany(where, update);
        return result.ModifiedCount;
    }

    public async ValueTask<long> UpdateAsync<T>(
        Expression<Func<T>> update,
        Expression<Func<T, bool>> where
    )
        where T : class
    {
        if (update is null)
            throw new ArgumentNullException(nameof(update));
        if (where is null)
            throw new ArgumentNullException(nameof(where));

        var tableName = GetTableName(typeof(T));
        var collection = MongoDatabase.GetCollection<T>(tableName, _collectionSettings);

        var result = await collection.UpdateManyAsync(where, update);
        return result.ModifiedCount;
    }

    private JsonFilterDefinition<T> GetJsonFilterDefinition<T>(
        T entity,
        GuidSerializer guidSerializer
    )
    {
        var idPropertyInfo = GetIdProperty(typeof(T));

        var value = idPropertyInfo.GetValue(entity, null);

        string json;
        if (IsNumericType(idPropertyInfo.PropertyType))
            json = $"{{\"_id\":{value}}}";
        else if (idPropertyInfo.PropertyType == typeof(Guid))
        {
            json = guidSerializer.GuidRepresentation switch
            {
                GuidRepresentation.Unspecified => "{\"_id\":" + $"UUID(\"{value}\")" + "}",
                GuidRepresentation.Standard => "{\"_id\":" + $"UUID(\"{value}\")" + "}",
                GuidRepresentation.CSharpLegacy => "{\"_id\":" + $"CSUUID(\"{value}\")" + "}",
                GuidRepresentation.JavaLegacy => "{\"_id\":" + $"UUID(\"{value}\")" + "}",
                GuidRepresentation.PythonLegacy => "{\"_id\":" + $"UUID(\"{value}\")" + "}",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        else
            json = $"{{\"_id\":\"{value}\"}}";

        return new JsonFilterDefinition<T>(json);
    }

    private static bool IsNumericType(Type type) =>
        Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => true,
            TypeCode.SByte => true,
            TypeCode.UInt16 => true,
            TypeCode.UInt32 => true,
            TypeCode.UInt64 => true,
            TypeCode.Int16 => true,
            TypeCode.Int32 => true,
            TypeCode.Int64 => true,
            TypeCode.Decimal => true,
            TypeCode.Double => true,
            TypeCode.Single => true,
            _ => false
        };

    private PropertyInfo GetIdProperty(Type type) =>
        _idProperties.GetOrAdd(
            type,
            _ =>
            {
                var propertyInfo =
                    type.GetProperties()
                        .FirstOrDefault(
                            property =>
                                Attribute
                                    .GetCustomAttributes(property)
                                    .OfType<BsonIdAttribute>()
                                    .Any()
                        )
                    ?? type.GetProperty("Id")
                    ?? type.GetProperty("id")
                    ?? type.GetProperty("_id");
                return propertyInfo
                    ?? throw new NullReferenceException("The primary key can not be found.");
            }
        );

    private string GetTableName(Type type) =>
        _tableNames.GetOrAdd(
            type,
            _ =>
                Attribute.GetCustomAttributes(type).OfType<TableAttribute>().FirstOrDefault()?.Name
                ?? type.Name
        );
}
