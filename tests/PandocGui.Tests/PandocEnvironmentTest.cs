#nullable enable
using System;
using PandocGui.CliWrapper;
using Shouldly;
using Xunit;

namespace PandocGui.Tests;

public sealed class PandocEnvironmentTest
{
    [Theory]
    [InlineData("pandoc 3.9.0.2", "3.9.0.2")]
    [InlineData("pandoc.exe 3.1.11\nFeatures: +server +lua", "3.1.11")]
    [InlineData("pandoc 2.19\nCompiled with pandoc-types", "2.19")]
    [InlineData("pandoc 3.9.0.2\nFeatures: +server +lua\nScripting engine: Lua 5.4", "3.9.0.2")]
    public void ParseVersion_ParsesPandocVersionOutput(string output, string expected)
    {
        PandocEnvironment.ParseVersion(output).ShouldBe(Version.Parse(expected));
    }

    [Theory]
    [InlineData("garbage output with no version")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseVersion_ReturnsNull_WhenNoMatch(string? output)
    {
        PandocEnvironment.ParseVersion(output).ShouldBeNull();
    }

    [Theory]
    [InlineData("Found Pandoc [JohnMacFarlane.Pandoc]\nVersion: 3.9.0.2\nPublisher: John MacFarlane", "3.9.0.2")]
    [InlineData("Znaleziono Pandoc\nWersja: 3.9.0.2\nWydawca: John MacFarlane", "3.9.0.2")]
    public void ParseWingetShowVersion_ParsesLocalizedVersionLine(string output, string expected)
    {
        PandocEnvironment.ParseWingetShowVersion(output).ShouldBe(Version.Parse(expected));
    }

    [Theory]
    [InlineData("no version line here")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseWingetShowVersion_ReturnsNull_WhenNoMatch(string? output)
    {
        PandocEnvironment.ParseWingetShowVersion(output).ShouldBeNull();
    }

    [Theory]
    [InlineData("3.1.0", "3.9.0", true)]
    [InlineData("3.9.0", "3.9.0", false)]
    [InlineData("3.9.0", "3.1.0", false)]
    public void IsUpdateAvailable_ComparesVersions(string current, string latest, bool expected)
    {
        PandocEnvironment.IsUpdateAvailable(Version.Parse(current), Version.Parse(latest)).ShouldBe(expected);
    }

    [Fact]
    public void IsUpdateAvailable_ReturnsFalse_WhenEitherVersionIsNull()
    {
        PandocEnvironment.IsUpdateAvailable(Version.Parse("3.9.0"), null).ShouldBeFalse();
        PandocEnvironment.IsUpdateAvailable(null, Version.Parse("3.9.0")).ShouldBeFalse();
        PandocEnvironment.IsUpdateAvailable(null, null).ShouldBeFalse();
    }

    [Theory]
    [InlineData(OsKind.MacOs, "brew")]
    [InlineData(OsKind.Linux, "apt")]
    [InlineData(OsKind.Windows, "pandoc.org")]
    [InlineData(OsKind.Other, "pandoc.org")]
    public void GetManualInstallInstructions_MentionsExpectedHint(OsKind os, string expectedHint)
    {
        PandocEnvironment.GetManualInstallInstructions(os).ShouldContain(expectedHint);
    }
}
