using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ClientPulse.Api.Communications;

public record CreateCommunicationRequest(string? Type, string? OccurredAt, string? Subject, string? Body);

public record ValidCommunication(CommunicationType Type, DateTimeOffset OccurredAt, string Subject, string Body);

public static class CommunicationRules
{
    public const int SubjectMaxLength = 200;
    public const int BodyMaxLength = 10_000;
    public static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    // Error messages must never include the submitted body text.
    public static bool TryValidate(
        CreateCommunicationRequest request,
        DateTimeOffset now,
        [NotNullWhen(true)] out ValidCommunication? result,
        out Dictionary<string, string[]> errors)
    {
        errors = [];

        var type = EnumNames.Parse<CommunicationType>(request.Type);
        if (string.IsNullOrWhiteSpace(request.Type))
        {
            errors["type"] = ["Type is required."];
        }
        else if (type is null)
        {
            errors["type"] = [$"Type must be one of: {string.Join(", ", Enum.GetNames<CommunicationType>())}."];
        }

        DateTimeOffset occurredAt = default;
        if (string.IsNullOrWhiteSpace(request.OccurredAt))
        {
            errors["occurredAt"] = ["Occurred at is required."];
        }
        else if (!DateTimeOffset.TryParse(
                     request.OccurredAt,
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                     out occurredAt))
        {
            errors["occurredAt"] = ["Occurred at must be a valid date and time."];
        }
        else if (occurredAt > now + FutureTolerance)
        {
            errors["occurredAt"] = ["Occurred at cannot be in the future."];
        }

        var subject = request.Subject?.Trim() ?? "";
        if (subject.Length == 0)
        {
            errors["subject"] = ["Subject is required."];
        }
        else if (subject.Length > SubjectMaxLength)
        {
            errors["subject"] = [$"Subject must be {SubjectMaxLength} characters or fewer."];
        }

        var body = request.Body?.Trim() ?? "";
        if (body.Length == 0)
        {
            errors["body"] = ["Body is required."];
        }
        else if (body.Length > BodyMaxLength)
        {
            errors["body"] = [$"Body must be {BodyMaxLength:N0} characters or fewer."];
        }

        if (errors.Count > 0 || type is null)
        {
            result = null;
            return false;
        }

        result = new ValidCommunication(type.Value, occurredAt.ToUniversalTime(), subject, body);
        return true;
    }
}
