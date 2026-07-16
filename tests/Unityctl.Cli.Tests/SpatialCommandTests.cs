using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class SpatialCommandTests
{
    [Fact]
    public void DescribeRequest_HasCorrectCommand()
    {
        var request = SpatialCommand.CreateDescribeRequest("Ceiling");
        Assert.Equal(WellKnownCommands.SpatialDescribe, request.Command);
    }

    [Fact]
    public void DescribeRequest_SetsTarget()
    {
        var request = SpatialCommand.CreateDescribeRequest("Ceiling");
        Assert.Equal("Ceiling", request.Parameters!["target"]!.ToString());
    }

    [Fact]
    public void DescribeRequest_Default_NoFull()
    {
        var request = SpatialCommand.CreateDescribeRequest("Ceiling");
        Assert.Null(request.Parameters!["full"]);
    }

    [Fact]
    public void DescribeRequest_Full_SetsParameter()
    {
        var request = SpatialCommand.CreateDescribeRequest("Ceiling", full: true);
        Assert.True(request.Parameters!["full"]!.GetValue<bool>());
    }

    [Fact]
    public void DescribeRequest_EmptyTarget_Throws()
    {
        Assert.Throws<ArgumentException>(() => SpatialCommand.CreateDescribeRequest(""));
    }

    [Fact]
    public void DescribeRequest_WhitespaceTarget_Throws()
    {
        Assert.Throws<ArgumentException>(() => SpatialCommand.CreateDescribeRequest("  "));
    }

    [Fact]
    public void CheckRequest_HasCorrectCommand()
    {
        var request = SpatialCommand.CreateCheckRequest("Cover", "covers", "Ceiling");
        Assert.Equal(WellKnownCommands.SpatialCheck, request.Command);
    }

    [Fact]
    public void CheckRequest_SetsSubjectPredicateTarget()
    {
        var request = SpatialCommand.CreateCheckRequest("Cover", "covers", "Ceiling");
        Assert.Equal("Cover", request.Parameters!["subject"]!.ToString());
        Assert.Equal("covers", request.Parameters!["predicate"]!.ToString());
        Assert.Equal("Ceiling", request.Parameters!["target"]!.ToString());
    }

    [Fact]
    public void CheckRequest_NormalizesPredicateCase()
    {
        var request = SpatialCommand.CreateCheckRequest("A", "COVERS", "B");
        Assert.Equal("covers", request.Parameters!["predicate"]!.ToString());
    }

    [Theory]
    [InlineData("covers")]
    [InlineData("inside")]
    [InlineData("on-top-of")]
    [InlineData("overlaps")]
    [InlineData("aligned")]
    public void CheckRequest_AcceptsAllValidPredicates(string predicate)
    {
        var request = SpatialCommand.CreateCheckRequest("A", predicate, "B");
        Assert.Equal(predicate, request.Parameters!["predicate"]!.ToString());
    }

    [Fact]
    public void CheckRequest_InvalidPredicate_Throws()
    {
        Assert.Throws<ArgumentException>(() => SpatialCommand.CreateCheckRequest("A", "under", "B"));
    }

    [Fact]
    public void CheckRequest_EmptySubject_Throws()
    {
        Assert.Throws<ArgumentException>(() => SpatialCommand.CreateCheckRequest("", "covers", "B"));
    }

    [Fact]
    public void CheckRequest_EmptyTarget_Throws()
    {
        Assert.Throws<ArgumentException>(() => SpatialCommand.CreateCheckRequest("A", "covers", ""));
    }

    [Fact]
    public void CheckRequest_EmptyPredicate_Throws()
    {
        Assert.Throws<ArgumentException>(() => SpatialCommand.CreateCheckRequest("A", "", "B"));
    }
}
