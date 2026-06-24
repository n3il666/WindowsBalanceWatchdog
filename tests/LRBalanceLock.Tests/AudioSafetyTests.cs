using System.Text.RegularExpressions;
using Xunit;

namespace LRBalanceLock.Tests;

public sealed class AudioSafetyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AudioCodeOnlyWritesFirstTwoEndpointVolumeChannels()
    {
        var audioSource = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "LRBalanceLock.App", "AudioDeviceService.cs"));
        var channelSetters = Regex.Matches(audioSource, @"endpoint\.Channels\[(\d+)\]\.VolumeLevelScalar\s*=");

        Assert.Equal(new[] { "0", "1" }, channelSetters.Select(match => match.Groups[1].Value).ToArray());
        Assert.DoesNotContain("MasterVolumeLevelScalar =", audioSource, StringComparison.Ordinal);
        Assert.DoesNotContain("VolumeLevel =", audioSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationDoesNotReferenceForbiddenAudioOrSystemMutationApis()
    {
        var forbiddenTerms = new[]
        {
            "DataFlow.Capture",
            "Role.Communications",
            "SetDefaultEndpoint",
            "PolicyConfig",
            "AudioSessionManager",
            "SimpleAudioVolume",
            "WaveIn",
            "WasapiCapture",
            "BufferedWaveProvider",
            "SignalGenerator",
            "PKEY_AudioEngine_DeviceFormat",
            "SetValue(\"PKEY_",
            "HKEY_LOCAL_MACHINE",
        };

        var sourceText = string.Join('\n', Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src", "LRBalanceLock.App"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        foreach (var forbiddenTerm in forbiddenTerms)
        {
            Assert.DoesNotContain(forbiddenTerm, sourceText, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void StartupRegistryMutationIsLimitedToCurrentUserRunEntry()
    {
        var startupSource = File.ReadAllText(Path.Combine(RepositoryRoot, "src", "LRBalanceLock.App", "StartupService.cs"));

        Assert.Contains("Registry.CurrentUser", startupSource, StringComparison.Ordinal);
        Assert.Contains(@"Software\Microsoft\Windows\CurrentVersion\Run", startupSource, StringComparison.Ordinal);
        Assert.Contains("LRBalanceLock", startupSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Registry.LocalMachine", startupSource, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LRBalanceLock.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
