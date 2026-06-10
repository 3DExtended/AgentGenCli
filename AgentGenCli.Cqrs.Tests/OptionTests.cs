using AgentGenCli.Cqrs;

namespace AgentGenCli.Cqrs.Tests;

public class OptionTests
{
    [Fact]
    public void From_null_returns_None()
    {
        var option = Option.From<string>(null);
        Assert.True(option.IsNone);
    }

    [Fact]
    public void From_value_returns_Some()
    {
        var option = Option.From("hello");
        Assert.True(option.IsSome);
        Assert.Equal("hello", option.Get());
    }

    [Fact]
    public void Get_on_None_throws()
    {
        var option = Option<string>.None;
        Assert.Throws<InvalidOperationException>(() => option.Get());
    }

    [Fact]
    public void Implicit_conversion_from_value()
    {
        Option<string> option = "test";
        Assert.True(option.IsSome);
        Assert.Equal("test", option.Get());
    }
}
