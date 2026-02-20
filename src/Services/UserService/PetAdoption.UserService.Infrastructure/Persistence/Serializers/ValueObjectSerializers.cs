namespace PetAdoption.UserService.Infrastructure.Persistence.Serializers;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using PetAdoption.UserService.Domain.ValueObjects;

public class EmailSerializer : SerializerBase<Email>
{
    public override Email Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Email.From(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Email value)
    {
        context.Writer.WriteString(value?.Value);
    }
}

public class FullNameSerializer : SerializerBase<FullName>
{
    public override FullName Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return FullName.From(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, FullName value)
    {
        context.Writer.WriteString(value?.Value);
    }
}

public class PasswordSerializer : SerializerBase<Password>
{
    public override Password Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Password.FromHash(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Password value)
    {
        context.Writer.WriteString(value?.HashedValue);
    }
}

public class PhoneNumberSerializer : SerializerBase<PhoneNumber?>
{
    public override PhoneNumber? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();
        if (type == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        return PhoneNumber.FromOptional(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PhoneNumber? value)
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
