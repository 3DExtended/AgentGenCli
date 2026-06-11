using AgentGenCli.Cli.Scaffolding;
using Xunit;

namespace AgentGenCli.Cli.Tests;

public class FeatureNameNormalizerTests
{
    [Theory]
    [InlineData("PublicGitSshKey", "PublicGitSshKey", "public-git-ssh-key", "PublicGitSshKeyEntity")]
    [InlineData("public-git-ssh-key", "PublicGitSshKey", "public-git-ssh-key", "PublicGitSshKeyEntity")]
    [InlineData("public_git_ssh_key", "PublicGitSshKey", "public-git-ssh-key", "PublicGitSshKeyEntity")]
    [InlineData("orders", "Orders", "orders", "OrdersEntity")]
    [InlineData(
        "public-git-ssh-key-entity",
        "PublicGitSshKeyEntity",
        "public-git-ssh-key-entity",
        "PublicGitSshKeyEntity"
    )]
    public void Normalize_ProducesExpectedNames(
        string input,
        string expectedPascal,
        string expectedFolder,
        string expectedEntityPascal
    )
    {
        var feature = FeatureNameNormalizer.Normalize(input);

        Assert.Equal(expectedPascal, feature.PascalName);
        Assert.Equal(expectedFolder, feature.FolderName);
        Assert.Equal(expectedEntityPascal, feature.EntityPascalName);
    }

    [Fact]
    public void SplitIntoSegments_SplitsPascalCaseWithoutDelimiters()
    {
        var segments = FeatureNameNormalizer.SplitIntoSegments("PublicGitSshKey");

        Assert.Equal(["Public", "Git", "Ssh", "Key"], segments);
    }

    [Fact]
    public void SplitIntoSegments_SplitsOnDelimiters()
    {
        var segments = FeatureNameNormalizer.SplitIntoSegments("public-git-ssh-key");

        Assert.Equal(["public", "git", "ssh", "key"], segments);
    }
}
