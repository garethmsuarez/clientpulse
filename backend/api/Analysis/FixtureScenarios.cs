namespace ClientPulse.Api.Analysis;

public record FixtureScenario(
    string Name,
    IReadOnlyList<string> CommunicationSubjects,
    string Summary,
    AttentionLevel AttentionLevel,
    IReadOnlyList<FixtureSignal> Signals);

public record FixtureSignal(SignalKind Kind, string Description, IReadOnlyList<string> EvidenceSubjects);

// Predefined, hand-written results for the synthetic seed scenarios. These are
// canned demo content, not inference. Each scenario names the seed
// communications it was written for by subject; it applies only when the
// communications submitted match that set exactly (see FixtureCommunicationAnalyzer).
public static class FixtureScenarios
{
    public static readonly IReadOnlyList<FixtureScenario> All =
    [
        new(
            "northwind-delivery-issues",
            ["Q4 shipment volumes", "Late deliveries follow-up", "Remediation plan sent"],
            "Growth plans are on record, but recent late deliveries have caused frustration and the remediation plan has not yet been acknowledged.",
            AttentionLevel.Medium,
            [
                new(SignalKind.Concern,
                    "Repeated late deliveries were raised, with a request for a written remediation plan.",
                    ["Late deliveries follow-up"]),
                new(SignalKind.Concern,
                    "The remediation plan was sent but no reply has been recorded.",
                    ["Remediation plan sent"]),
                new(SignalKind.Positive,
                    "The client expects higher volumes next quarter and wants to confirm capacity.",
                    ["Q4 shipment volumes"]),
            ]),
        new(
            "bluefin-expansion-interest",
            ["Quarterly business review", "Expansion pricing"],
            "The client reports satisfaction with service and is exploring an expansion, pending revised pricing.",
            AttentionLevel.Low,
            [
                new(SignalKind.Positive,
                    "The client is happy with response times and wants to discuss expanding to more clinics.",
                    ["Quarterly business review"]),
                new(SignalKind.Concern,
                    "Revised expansion pricing is needed before the client's budget meeting.",
                    ["Expansion pricing"]),
            ]),
        new(
            "cedar-ridge-renewal-risk",
            ["Invoice discrepancy", "Contract renewal", "Request to pause project"],
            "An unresolved invoice dispute, a competitive review ahead of renewal, and a request to pause work together call for prompt attention.",
            AttentionLevel.High,
            [
                new(SignalKind.Concern,
                    "The client flagged an invoice that does not match the agreed rate card.",
                    ["Invoice discrepancy"]),
                new(SignalKind.Concern,
                    "Procurement is evaluating other vendors ahead of renewal.",
                    ["Contract renewal"]),
                new(SignalKind.Concern,
                    "The client asked to pause the next rollout phase until billing and renewal are resolved.",
                    ["Request to pause project", "Invoice discrepancy"]),
            ]),
        new(
            "lumen-steady-engagement",
            ["Holiday readiness", "Holiday campaign kickoff"],
            "Engagement is positive with no open issues recorded.",
            AttentionLevel.Low,
            [
                new(SignalKind.Positive,
                    "The client confirmed holiday staffing with no open issues.",
                    ["Holiday readiness"]),
                new(SignalKind.Positive,
                    "The client praised the dashboard improvements and asked for ongoing weekly summaries.",
                    ["Holiday campaign kickoff"]),
            ]),
        new(
            "harbourview-audit-escalation",
            ["Compliance documentation", "Audit timeline concern", "Documents delivered"],
            "A documentation delay led to an escalation over audit risk; the documents have since been delivered and await auditor review.",
            AttentionLevel.Medium,
            [
                new(SignalKind.Concern,
                    "The client said the documentation delay could put their audit at risk and asked for escalation.",
                    ["Audit timeline concern"]),
                new(SignalKind.Positive,
                    "The compliance package was delivered and receipt was acknowledged.",
                    ["Documents delivered", "Compliance documentation"]),
            ]),
    ];
}
