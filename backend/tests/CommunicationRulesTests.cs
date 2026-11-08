using ClientPulse.Api.Communications;

namespace ClientPulse.Tests;

public class CommunicationRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private static CreateCommunicationRequest Valid() =>
        new("Email", "2026-11-07T15:30:00-05:00", "Synthetic subject", "Synthetic body.");

    private static Dictionary<string, string[]> ErrorsFor(CreateCommunicationRequest request)
    {
        Assert.False(CommunicationRules.TryValidate(request, Now, out var result, out var errors));
        Assert.Null(result);
        return errors;
    }

    [Fact]
    public void ValidRequest_ReturnsNormalizedCommunication()
    {
        var request = Valid() with { Type = " call ", Subject = "  Padded  ", Body = "\n Body \n" };

        Assert.True(CommunicationRules.TryValidate(request, Now, out var result, out var errors));
        Assert.Empty(errors);
        Assert.Equal(CommunicationType.Call, result.Type);
        Assert.Equal(new DateTimeOffset(2026, 11, 7, 20, 30, 0, TimeSpan.Zero), result.OccurredAt);
        Assert.Equal(TimeSpan.Zero, result.OccurredAt.Offset);
        Assert.Equal("Padded", result.Subject);
        Assert.Equal("Body", result.Body);
    }

    [Fact]
    public void MissingFields_AreAllReported()
    {
        var errors = ErrorsFor(new CreateCommunicationRequest(null, null, null, null));

        Assert.Equal(["body", "occurredAt", "subject", "type"], errors.Keys.Order());
    }

    [Theory]
    [InlineData("Fax")]
    [InlineData("1")]
    public void UnknownType_IsRejected(string type)
    {
        var errors = ErrorsFor(Valid() with { Type = type });

        Assert.Equal(["type"], errors.Keys);
    }

    [Fact]
    public void UnparseableOccurredAt_IsRejected()
    {
        var errors = ErrorsFor(Valid() with { OccurredAt = "yesterday" });

        Assert.Equal(["occurredAt"], errors.Keys);
    }

    [Fact]
    public void FutureOccurredAt_IsRejected()
    {
        var future = Now + CommunicationRules.FutureTolerance + TimeSpan.FromMinutes(1);

        var errors = ErrorsFor(Valid() with { OccurredAt = future.ToString("O") });

        Assert.Equal(["occurredAt"], errors.Keys);
    }

    [Fact]
    public void OccurredAtWithinClockSkewTolerance_IsAccepted()
    {
        var nearFuture = Now + TimeSpan.FromMinutes(2);

        Assert.True(CommunicationRules.TryValidate(
            Valid() with { OccurredAt = nearFuture.ToString("O") }, Now, out _, out _));
    }

    [Fact]
    public void WhitespaceSubjectAndBody_AreRejected()
    {
        var errors = ErrorsFor(Valid() with { Subject = "   ", Body = "\t\n" });

        Assert.Equal(["body", "subject"], errors.Keys.Order());
    }

    [Fact]
    public void OverlongSubjectAndBody_AreRejectedWithoutEchoingBody()
    {
        var body = "Synthetic-secret-body " + new string('x', CommunicationRules.BodyMaxLength);

        var errors = ErrorsFor(Valid() with
        {
            Subject = new string('s', CommunicationRules.SubjectMaxLength + 1),
            Body = body,
        });

        Assert.Equal(["body", "subject"], errors.Keys.Order());
        Assert.DoesNotContain(errors.Values.SelectMany(m => m), m => m.Contains("Synthetic-secret-body"));
    }

    [Fact]
    public void MaxLengthSubjectAndBody_AreAccepted()
    {
        var request = Valid() with
        {
            Subject = new string('s', CommunicationRules.SubjectMaxLength),
            Body = new string('b', CommunicationRules.BodyMaxLength),
        };

        Assert.True(CommunicationRules.TryValidate(request, Now, out _, out _));
    }
}
