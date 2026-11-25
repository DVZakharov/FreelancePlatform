using FreelancePlatform.Application.Features.Auth.Register;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Validators;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    private static RegisterRequest Valid() => new("user@test.local", "Passw0rd!", "Иван Иванов");

    [Fact]
    public void Valid_Passes() => _validator.Passes(Valid());

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign")]
    [InlineData("a@")]
    [InlineData("@b.com")]
    public void BadEmail_Fails(string email) =>
        _validator.FailsOn(Valid() with { Email = email }, nameof(RegisterRequest.Email));

    [Theory]
    [InlineData(3, false)]
    [InlineData(6, true)]
    public void PasswordLength_Boundary(int length, bool valid)
    {
        var request = Valid() with { Password = new string('a', length) + "A!1" };

        if (valid) _validator.Passes(request);
        else _validator.FailsOn(request, nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyFullName_Fails(string name) =>
        _validator.FailsOn(Valid() with { FullName = name }, nameof(RegisterRequest.FullName));

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void FullNameLength_Boundary(int length, bool valid)
    {
        var request = Valid() with { FullName = new string('a', length) };

        if (valid) _validator.Passes(request);
        else _validator.FailsOn(request, nameof(RegisterRequest.FullName));
    }
}