using ApexBooking.Core.Persistence.Identity;
using ApexBooking.SharedKernel.Exceptions;
using Xunit;

namespace ApexBooking.Core.Domain.UnitTests.Identity;

public class ApplicationUserTests
{
    private static ApplicationUser CreateUser() =>
        ApplicationUser.Create("owner@example.com", "Ada", "Lovelace");

    [Fact]
    public void UpdateProfile_SetsNameAndPhoneNumber()
    {
        var user = CreateUser();
        var utcNow = DateTime.UtcNow;

        user.UpdateProfile("Grace", "Hopper", "+15551234567", utcNow);

        Assert.Equal("Grace", user.FirstName);
        Assert.Equal("Hopper", user.LastName);
        Assert.Equal("+15551234567", user.PhoneNumber);
        Assert.Equal(utcNow, user.UpdatedAt);
    }

    [Fact]
    public void UpdateProfile_WithBlankPhoneNumber_ClearsIt()
    {
        var user = CreateUser();
        user.UpdateProfile("Grace", "Hopper", "+15551234567", DateTime.UtcNow);

        user.UpdateProfile("Grace", "Hopper", "  ", DateTime.UtcNow);

        Assert.Null(user.PhoneNumber);
    }

    [Fact]
    public void UpdateProfile_WithEmptyLastName_Throws()
    {
        var user = CreateUser();

        Assert.Throws<BusinessRuleBrokenException>(() =>
            user.UpdateProfile("Grace", "  ", null, DateTime.UtcNow));
    }

    [Fact]
    public void UpdatePhoto_SetsPhotoUrl()
    {
        var user = CreateUser();
        var utcNow = DateTime.UtcNow;

        user.UpdatePhoto("https://cdn.example.com/photo.jpg", utcNow);

        Assert.Equal("https://cdn.example.com/photo.jpg", user.PhotoUrl);
        Assert.Equal(utcNow, user.UpdatedAt);
    }

    [Fact]
    public void UpdatePhoto_WithNull_ClearsPhotoUrl()
    {
        var user = CreateUser();
        user.UpdatePhoto("https://cdn.example.com/photo.jpg", DateTime.UtcNow);

        user.UpdatePhoto(null, DateTime.UtcNow);

        Assert.Null(user.PhotoUrl);
    }
}
