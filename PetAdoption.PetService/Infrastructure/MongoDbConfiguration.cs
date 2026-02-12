using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure;

public static class MongoDbConfiguration
{
    private static bool _isConfigured;

    public static void Configure()
    {
        if (_isConfigured)
            return;

        // Configure Pet entity mapping
        BsonClassMap.RegisterClassMap<Pet>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(p => p.Id)
                .SetIdGenerator(GuidGenerator.Instance)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            cm.SetIgnoreExtraElements(true);
        });

        _isConfigured = true;
    }
}
