using System.ComponentModel.DataAnnotations;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Tests.Unit;

/// <summary>Unit tests for the validation rules on request DTOs.</summary>
public class RequestValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static RegisterRequest ValidParticipant() => new()
    {
        Email = "nomsa@test.co.za",
        UserName = "NomsaWalks",
        Password = "Str0ngPass!",
        Role = UserRole.Participant,
        Name = "Nomsa Zulu"
    };

    [Fact]
    public void Register_ValidParticipant_HasNoErrors()
    {
        Assert.Empty(Validate(ValidParticipant()));
    }

    [Fact]
    public void Register_ParticipantCannotBeAdmin()
    {
        var request = ValidParticipant();
        request.AccountType = AccountType.Admin;

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(RegisterRequest.AccountType)));
    }

    [Fact]
    public void Register_OrganiserCannotHaveDateOfBirth()
    {
        var request = ValidParticipant();
        request.Role = UserRole.Organiser;
        request.DateOfBirth = new DateOnly(1990, 1, 1);

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(RegisterRequest.DateOfBirth)));
    }

    [Fact]
    public void Register_FutureDateOfBirth_IsRejected()
    {
        var request = ValidParticipant();
        request.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void Register_MissingRole_IsRejected()
    {
        var request = ValidParticipant();
        request.Role = null;

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(RegisterRequest.Role)));
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("1.00:00:00")]
    public void Result_FinishingTimeOutsideOneDay_IsRejected(string time)
    {
        var request = new ResultValues { FinishingTime = TimeSpan.Parse(time), Position = 1 };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(ResultValues.FinishingTime)));
    }

    [Fact]
    public void Event_ZeroDistance_IsRejected()
    {
        var request = new EventRequest
        {
            EventName = "Comrades Training Walk",
            EventDate = new DateOnly(2027, 6, 1),
            Location = "Pietermaritzburg",
            DistanceKm = 0,
            EventType = EventType.Walk
        };

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(EventRequest.DistanceKm)));
    }
}
