namespace PetAdoption.UserService.Infrastructure.Persistence.Serializers;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using PetAdoption.UserService.Domain.ValueObjects;

public class UserIdSerializer : SerializerBase<UserId>
{
    public override UserId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();
        switch (type)
        {
            case BsonType.String:
                return UserId.From(context.Reader.ReadString());
            default:
                throw new NotSupportedException($"Cannot deserialize UserId from BsonType {type}");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UserId value)
    {
        if (value == null)
        {
            context.Writer.WriteNull();
        }
        else
        {
            context.Writer.WriteString(value.Value);
        }
    }
}
