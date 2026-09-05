using TerminalDotnet;
using Xunit;

namespace TerminalDotnet.Tests;

public sealed class WhenReportingTheVersion
{
    [Fact]
    public void It_reads_the_three_parts_of_the_built_version()
    {
        // Act
        var version = VersionNumber.From("0.1.42");

        // Assert
        Assert.Equal(new VersionNumber(0, 1, 42), version);
    }

    [Fact]
    public void It_ignores_the_source_revision_recorded_by_the_build()
    {
        // Act
        var version = VersionNumber.From("0.1.42+9f3c1ab7d0e4c5b6a7980f1e2d3c4b5a69780abc");

        // Assert
        Assert.Equal(new VersionNumber(0, 1, 42), version);
    }

    [Fact]
    public void It_assumes_the_first_patch_when_the_build_omits_one()
    {
        // Act
        var version = VersionNumber.From("0.2");

        // Assert
        Assert.Equal(new VersionNumber(0, 2, 0), version);
    }

    [Fact]
    public void It_falls_back_to_zero_when_no_version_was_built_in()
    {
        // Act
        var version = VersionNumber.From(null);

        // Assert
        Assert.Equal(new VersionNumber(0, 0, 0), version);
    }

    [Fact]
    public void It_prints_the_parts_separated_by_dots()
    {
        // Act
        var printed = new VersionNumber(1, 2, 3).ToString();

        // Assert
        Assert.Equal("1.2.3", printed);
    }

    [Fact]
    public void It_reports_the_version_of_the_running_build()
    {
        // Act
        var version = VersionNumber.Current;

        // Assert
        Assert.StartsWith("0.1.", version.ToString());
    }
}
