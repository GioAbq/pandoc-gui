#nullable enable
using System;
using PandocGui.CliWrapper;
using Xunit;

namespace PandocGui.Tests;

public class PandocEnvironmentTest
{
    [Theory]
    [InlineData("pandoc 3.9.0.2", "3.9.0.2")]
    [InlineData("pandoc.exe 3.1.11\nFeatures: +server +lua", "3.1.11")]
    [InlineData("pandoc 2.19\nCompiled with pandoc-types", "2.19")]
    [InlineData("pandoc 3.9.0.2\nFeatures: +server +lua\nScripting engine: Lua 5.4", "3.9.0.2")]
    public void ParseVersion_ParsesPandocVersionOutput(string output, string expected)
    {
        Assert.Equal(Version.Parse(expected), PandocEnvironment.ParseVersion(output));
    }

    [Theory]
    [InlineData("garbage output with no version")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseVersion_ReturnsNull_WhenNoMatch(string? output)
    {
        Assert.Null(PandocEnvironment.ParseVersion(output));
    }

    [Theory]
    [InlineData("Found Pandoc [JohnMacFarlane.Pandoc]\nVersion: 3.9.0.2\nPublisher: John MacFarlane", "3.9.0.2")]
    [InlineData("Znaleziono Pandoc\nWersja: 3.9.0.2\nWydawca: John MacFarlane", "3.9.0.2")]
    public void ParseWingetShowVersion_ParsesLocalizedVersionLine(string output, string expected)
    {
        Assert.Equal(Version.Parse(expected), PandocEnvironment.ParseWingetShowVersion(output));
    }

    [Theory]
    [InlineData("no version line here")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseWingetShowVersion_ReturnsNull_WhenNoMatch(string? output)
    {
        Assert.Null(PandocEnvironment.ParseWingetShowVersion(output));
    }

    [Theory]
    [InlineData("3.1.0", "3.9.0", true)]
    [InlineData("3.9.0", "3.9.0", false)]
    [InlineData("3.9.0", "3.1.0", false)]
    public void IsUpdateAvailable_ComparesVersions(string current, string latest, bool expected)
    {
        Assert.Equal(expected, PandocEnvironment.IsUpdateAvailable(Version.Parse(current), Version.Parse(latest)));
    }

    [Fact]
    public void IsUpdateAvailable_ReturnsFalse_WhenEitherVersionIsNull()
    {
        Assert.False(PandocEnvironment.IsUpdateAvailable(Version.Parse("3.9.0"), null));
        Assert.False(PandocEnvironment.IsUpdateAvailable(null, Version.Parse("3.9.0")));
        Assert.False(PandocEnvironment.IsUpdateAvailable(null, null));
    }

    [Theory]
    [InlineData(OsKind.MacOs, "brew")]
    [InlineData(OsKind.Linux, "apt")]
    [InlineData(OsKind.Windows, "pandoc.org")]
    [InlineData(OsKind.Other, "pandoc.org")]
    public void GetManualInstallInstructions_MentionsExpectedHint(OsKind os, string expectedHint)
    {
        Assert.Contains(expectedHint, PandocEnvironment.GetManualInstallInstructions(os));
    }
}
