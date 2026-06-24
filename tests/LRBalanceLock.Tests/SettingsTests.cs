using LRBalanceLock.App.Models;
using Xunit;

namespace LRBalanceLock.Tests;

public sealed class SettingsTests
{
    [Fact]
    public void DefaultsAreSafe()
    {
        var settings = new AppSettings();
        Assert.False(settings.BalanceLockEnabled);
        Assert.Equal("default", settings.SelectedDeviceMode);
        Assert.True(settings.MinimizeToTrayOnClose);
        Assert.Equal(1000, settings.PollingIntervalMs);
    }

    [Theory]
    [InlineData(100, 250)]
    [InlineData(1000, 1000)]
    [InlineData(8000, 5000)]
    public void PollingIntervalIsClamped(int input, int expected)
    {
        var settings = new AppSettings { PollingIntervalMs = input };
        settings.Normalize();
        Assert.Equal(expected, settings.PollingIntervalMs);
    }
}
