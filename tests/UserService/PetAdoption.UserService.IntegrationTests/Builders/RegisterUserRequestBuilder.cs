namespace PetAdoption.UserService.IntegrationTests.Builders;

public class RegisterUserRequestBuilder
{
    private string _email = "user@example.com";
    private string _fullName = "John Doe";
    private string _password = "SecurePass123!";
    private string? _phoneNumber;

    public RegisterUserRequestBuilder WithEmail(string email) { _email = email; return this; }
    public RegisterUserRequestBuilder WithFullName(string name) { _fullName = name; return this; }
    public RegisterUserRequestBuilder WithPassword(string password) { _password = password; return this; }
    public RegisterUserRequestBuilder WithPhoneNumber(string? phone) { _phoneNumber = phone; return this; }

    public object Build() => new
    {
        Email = _email,
        FullName = _fullName,
        Password = _password,
        PhoneNumber = _phoneNumber
    };

    public static RegisterUserRequestBuilder Default() => new();
}
