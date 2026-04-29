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
        BsonSerializer.RegisterSerializer(new PetBreedSerializer());
        BsonSerializer.RegisterSerializer(new PetAgeSerializer());
        BsonSerializer.RegisterSerializer(new PetDescriptionSerializer());

        // Configure Pet entity mapping
        BsonClassMap.RegisterClassMap<Pet>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(p => p.Id)
                .SetIdGenerator(GuidGenerator.Instance)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapMember(p => p.PetTypeId)
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

        // Configure Favorite entity mapping
        BsonClassMap.RegisterClassMap<Favorite>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(c => c.Id)
                .SetIdGenerator(GuidGenerator.Instance)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapMember(c => c.UserId)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.MapMember(c => c.PetId)
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

    // Custom serializer for PetBreed value object (nullable)
    private class PetBreedSerializer : SerializerBase<PetBreed?>
    {
        public override PetBreed? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var type = context.Reader.GetCurrentBsonType();
            if (type == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }
            var value = context.Reader.ReadString();
            return new PetBreed(value);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PetBreed? value)
        {
            if (value is null)
                context.Writer.WriteNull();
            else
                context.Writer.WriteString(value.Value);
        }
    }

    // Custom serializer for PetAge value object (nullable)
    private class PetAgeSerializer : SerializerBase<PetAge?>
    {
        public override PetAge? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var type = context.Reader.GetCurrentBsonType();
            if (type == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }
            var value = context.Reader.ReadInt32();
            return new PetAge(value);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PetAge? value)
        {
            if (value is null)
                context.Writer.WriteNull();
            else
                context.Writer.WriteInt32(value.Months);
        }
    }

    // Custom serializer for PetDescription value object (nullable)
    private class PetDescriptionSerializer : SerializerBase<PetDescription?>
    {
        public override PetDescription? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var type = context.Reader.GetCurrentBsonType();
            if (type == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }
            var value = context.Reader.ReadString();
            return new PetDescription(value);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PetDescription? value)
        {
            if (value is null)
                context.Writer.WriteNull();
            else
                context.Writer.WriteString(value.Value);
        }
    }
}
