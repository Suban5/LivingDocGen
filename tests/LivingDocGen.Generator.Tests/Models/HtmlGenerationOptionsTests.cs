using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Services;
using Xunit;

namespace LivingDocGen.Generator.Tests.Models;

/// <summary>
/// Tests for HtmlGenerationOptions defaults after PR-6 default switch.
/// </summary>
public class HtmlGenerationOptionsTests
{
    [Fact]
    public void DefaultOutputMode_IsChunked()
    {
        var options = new HtmlGenerationOptions();
        Assert.Equal(OutputMode.Chunked, options.OutputMode);
    }

    [Fact]
    public void CanSetOutputMode_ToLegacy()
    {
#pragma warning disable CS0618 // Suppress obsolete warning for test
        var options = new HtmlGenerationOptions
        {
            OutputMode = OutputMode.Legacy
        };
        Assert.Equal(OutputMode.Legacy, options.OutputMode);
#pragma warning restore CS0618
    }

    [Fact]
    public void DefaultTheme_IsPurple()
    {
        var options = new HtmlGenerationOptions();
        Assert.Equal("purple", options.Theme);
    }

    [Fact]
    public void DefaultIncludeComments_IsTrue()
    {
        var options = new HtmlGenerationOptions();
        Assert.True(options.IncludeComments);
    }
}
