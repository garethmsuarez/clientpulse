using ClientPulse.Api.Analysis;
using ClientPulse.Api.Communications;

namespace ClientPulse.Tests;

public class InputFingerprintTests
{
    private static readonly AnalysisCommunication First =
        new(1, CommunicationType.Email, new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero), "Subject A", "Body A");

    private static readonly AnalysisCommunication Second =
        new(2, CommunicationType.Call, new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero), "Subject B", "Body B");

    [Fact]
    public void IsStableAndOrderInsensitive()
    {
        Assert.Equal(InputFingerprint.Compute([First, Second]), InputFingerprint.Compute([Second, First]));
        Assert.Matches("^[0-9a-f]{64}$", InputFingerprint.Compute([First]));
    }

    [Fact]
    public void IgnoresIdsAndOffsetRepresentation()
    {
        var renumbered = First with { Id = 5000, OccurredAt = First.OccurredAt.ToOffset(TimeSpan.FromHours(-6)) };

        Assert.Equal(InputFingerprint.Compute([First]), InputFingerprint.Compute([renumbered]));
    }

    [Fact]
    public void ChangesWhenAnyContentChanges()
    {
        var baseline = InputFingerprint.Compute([First, Second]);

        Assert.NotEqual(baseline, InputFingerprint.Compute([First]));
        Assert.NotEqual(baseline, InputFingerprint.Compute([First, Second, Second with { Id = 3, Subject = "New" }]));
        Assert.NotEqual(baseline, InputFingerprint.Compute([First with { Type = CommunicationType.Note }, Second]));
        Assert.NotEqual(baseline, InputFingerprint.Compute([First with { OccurredAt = First.OccurredAt.AddSeconds(1) }, Second]));
        Assert.NotEqual(baseline, InputFingerprint.Compute([First with { Subject = "Subject A." }, Second]));
        Assert.NotEqual(baseline, InputFingerprint.Compute([First with { Body = "Body A." }, Second]));
    }
}
