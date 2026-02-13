using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public static class MongoDbConfiguration
{
    private static bool _isConfigured;

    public static void Configure()
    {
        if (_isConfigured)
            return;

        // Register value object serializers
        BsonSerializer.RegisterSerializer(new PetNameSerializer());

        // Configure Pet entity mapping
        BsonClassMap.RegisterClassMap<Pet>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(p => p.Id)
                .SetIdGenerator(GuidGenerator.Instance)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.SetIgnoreExtraElements(true);
        });

        // Configure PetType entity mapping
        BsonClassMap.RegisterClassMap<PetType>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(pt => pt.Id)
                .SetIdGenerator(GuidGenerator.Instance)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.SetIgnoreExtraElements(true);
        });

        // Configure OutboxEvent entity mapping
        BsonClassMap.RegisterClassMap<OutboxEvent>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(e => e.Id)
                .SetIdGenerator(GuidGenerator.Instance)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.SetIgnoreExtraElements(true);
        });

        _isConfigured = true;
    }

    // Custom serializer for PetName value object
    private class PetNameSerializer : SerializerBase<PetName>
    {
        public override PetName Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var value = context.Reader.ReadString();
            return new PetName(value);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PetName value)
        {
            context.Writer.WriteString(value?.Value ?? string.Empty);
        }
    }
}
