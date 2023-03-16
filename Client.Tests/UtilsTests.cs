using FluentAssertions;
using JsonParsing;

namespace Client.Tests;

public class UtilsTests
{
    [Fact]
    public void ConvertDashesToCamelCase_VerifyResult()
    {
        Utils.ConvertDashesToCamelCase("jeppe-Kristensen/roi").Should().Be("Jeppe_Kristensen_roi");
    }
}