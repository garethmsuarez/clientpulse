using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ClientPulse.Api.Communications;

namespace ClientPulse.Api.Analysis;

// Identifies a set of communications by content, independent of order and of
// database-generated ids. Any added, removed, or changed communication
// produces a different fingerprint.
public static class InputFingerprint
{
    public static string Compute(IEnumerable<AnalysisCommunication> communications) =>
        Compute(communications.Select(c => (c.Type, c.OccurredAt, c.Subject, c.Body)));

    public static string Compute(IEnumerable<Communication> communications) =>
        Compute(communications.Select(c => (c.Type, c.OccurredAt, c.Subject, c.Body)));

    private static string Compute(
        IEnumerable<(CommunicationType Type, DateTimeOffset OccurredAt, string Subject, string Body)> communications)
    {
        // Unit/record separators keep field and item boundaries unambiguous.
        var canonical = communications
            .Select(c => string.Join('\u001f',
                c.Type.ToString(),
                c.OccurredAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture),
                c.Subject,
                c.Body))
            .Order(StringComparer.Ordinal);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\u001e', canonical)));
        return Convert.ToHexStringLower(bytes);
    }
}
