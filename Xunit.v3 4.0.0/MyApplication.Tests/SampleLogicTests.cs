using Xunit;
using Shouldly;

namespace MyApplication.Tests;

public class SampleLogicTests
{
    [Fact]
    public void DotNet10_With_Shouldly_Works()
    {
        // Arrange
        string currentFramework = ".NET 10";
        var features = new List<string> { "xUnit v3", "Native Coverage", "Shouldly" };

        // Act & Assert
        currentFramework.ShouldBe(".NET 10");
        currentFramework.ShouldStartWith(".NET");
        features.ShouldContain("Shouldly");
    }
}
