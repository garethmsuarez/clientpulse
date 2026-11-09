using System.Globalization;
using ClientPulse.Api.Clients;
using ClientPulse.Api.Communications;

namespace ClientPulse.Api.Data;

// The synthetic demo dataset, loaded through EF Core HasData so it is part of
// the migrations. One connected scenario set: communications reference client
// ids, and FixtureScenarios match on communication content. Changing values
// here changes the model and requires a new migration.
public static class DemoSeedData
{
    // Seeded rows use explicit ids, which PostgreSQL identity columns do not
    // account for; new communications start above the seeded range.
    public const int FirstGeneratedCommunicationId = 1000;

    // Synthetic demo clients only; not real organisations or people.
    public static readonly IReadOnlyList<Client> Clients =
    [
        new() { Id = 1, Name = "Northwind Logistics", Industry = "Logistics", AccountOwner = "Priya Raman" },
        new() { Id = 2, Name = "Bluefin Health Partners", Industry = "Healthcare", AccountOwner = "Marcus Ellery" },
        new() { Id = 3, Name = "Cedar Ridge Manufacturing", Industry = "Manufacturing", AccountOwner = "Dana Okafor" },
        new() { Id = 4, Name = "Lumen Retail Group", Industry = "Retail", AccountOwner = "Priya Raman" },
        new() { Id = 5, Name = "Harbourview Financial", Industry = "Financial Services", AccountOwner = "Tomasz Weller" },
    ];

    // Synthetic demo communications only; invented content.
    public static readonly IReadOnlyList<Communication> Communications =
    [
        Seed(1, 1, CommunicationType.Email, "2026-10-06T14:20:00Z",
            "Q4 shipment volumes",
            "Thanks for the forecast. We expect volumes to rise about 15% next quarter and would like to confirm capacity on the northern routes."),
        Seed(2, 1, CommunicationType.Call, "2026-10-21T16:00:00Z",
            "Late deliveries follow-up",
            "Client raised three late deliveries in two weeks. They said their operations lead is frustrated and asked for a written remediation plan by Friday."),
        Seed(3, 1, CommunicationType.Note, "2026-10-28T09:30:00Z",
            "Remediation plan sent",
            "Sent the remediation plan. No reply yet; will check in early next week."),

        Seed(4, 2, CommunicationType.Meeting, "2026-09-30T15:00:00Z",
            "Quarterly business review",
            "Reviewed onboarding metrics. Client is happy with response times and wants to discuss expanding to two more clinics."),
        Seed(5, 2, CommunicationType.Email, "2026-10-14T11:45:00Z",
            "Expansion pricing",
            "Client requested revised pricing for the two additional clinics before their budget meeting at the end of the month."),

        Seed(6, 3, CommunicationType.Email, "2026-10-02T13:10:00Z",
            "Invoice discrepancy",
            "Client flagged that the September invoice does not match the agreed rate card. Finance is reviewing."),
        Seed(7, 3, CommunicationType.Call, "2026-10-19T17:30:00Z",
            "Contract renewal",
            "Procurement mentioned they are evaluating other vendors ahead of renewal and asked for a competitive proposal."),
        Seed(8, 3, CommunicationType.Email, "2026-11-03T10:05:00Z",
            "Request to pause project",
            "Client asked to pause the phase two rollout until the invoice issue is resolved and renewal terms are agreed."),

        Seed(9, 4, CommunicationType.Note, "2026-10-09T08:50:00Z",
            "Holiday readiness",
            "Client confirmed staffing plans for the holiday peak. No open issues."),
        Seed(10, 4, CommunicationType.Meeting, "2026-10-23T14:00:00Z",
            "Holiday campaign kickoff",
            "Kickoff went well. Client praised the dashboard improvements and asked for weekly summaries through January."),

        Seed(11, 5, CommunicationType.Email, "2026-10-07T12:00:00Z",
            "Compliance documentation",
            "Client requested updated compliance documentation for their annual audit."),
        Seed(12, 5, CommunicationType.Call, "2026-10-27T15:15:00Z",
            "Audit timeline concern",
            "Client said the delay on the compliance documents could put their audit at risk and asked for escalation to a senior contact."),
        Seed(13, 5, CommunicationType.Email, "2026-11-04T09:40:00Z",
            "Documents delivered",
            "Delivered the compliance package. Client acknowledged receipt and will confirm once their auditor has reviewed it."),
    ];

    private static Communication Seed(
        int id, int clientId, CommunicationType type, string occurredAtUtc, string subject, string body)
    {
        var occurredAt = DateTimeOffset.Parse(
            occurredAtUtc,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return new Communication
        {
            Id = id,
            ClientId = clientId,
            Type = type,
            OccurredAt = occurredAt,
            Subject = subject,
            Body = body,
            CreatedAt = occurredAt,
        };
    }
}
