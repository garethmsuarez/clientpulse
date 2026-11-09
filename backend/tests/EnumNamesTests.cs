using ClientPulse.Api;
using ClientPulse.Api.Communications;

namespace ClientPulse.Tests;

public class EnumNamesTests
{
    [Theory]
    [InlineData("Email", CommunicationType.Email)]
    [InlineData("email", CommunicationType.Email)]
    [InlineData("  CALL ", CommunicationType.Call)]
    public void AcceptsNames_CaseInsensitiveAndTrimmed(string value, CommunicationType expected)
    {
        Assert.Equal(expected, EnumNames.Parse<CommunicationType>(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("Fax")]
    public void RejectsNumbersBlanksAndUnknownNames(string? value)
    {
        Assert.Null(EnumNames.Parse<CommunicationType>(value));
    }
}
