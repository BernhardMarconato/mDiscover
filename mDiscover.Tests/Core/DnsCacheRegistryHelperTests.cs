using Microsoft.Win32;
using mDiscover.Core.Helpers;
using mDiscover.Core.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace mDiscover.Tests.Core;

public class DnsCacheRegistryHelperTests
{
    private readonly IRegistryService _registryService = Substitute.For<IRegistryService>();
    private const string SubKey = DnsCacheRegistryHelper.DnscacheParametersSubKey;
    private const string ValueName = DnsCacheRegistryHelper.EnableMdnsValueName;

    [Fact]
    public void IsMdnsDisabledInRegistry_ReturnsFalse_WhenValueIsNull()
    {
        _registryService.GetInt32(RegistryHive.LocalMachine, SubKey, ValueName).Returns((int?)null);

        var result = DnsCacheRegistryHelper.IsMdnsDisabledInRegistry(_registryService);

        Assert.False(result);
    }

    [Fact]
    public void IsMdnsDisabledInRegistry_ReturnsFalse_WhenEnableMdnsIsOne()
    {
        _registryService.GetInt32(RegistryHive.LocalMachine, SubKey, ValueName).Returns(1);

        var result = DnsCacheRegistryHelper.IsMdnsDisabledInRegistry(_registryService);

        Assert.False(result);
    }

    [Fact]
    public void IsMdnsDisabledInRegistry_ReturnsTrue_WhenEnableMdnsIsZero()
    {
        _registryService.GetInt32(RegistryHive.LocalMachine, SubKey, ValueName).Returns(0);

        var result = DnsCacheRegistryHelper.IsMdnsDisabledInRegistry(_registryService);

        Assert.True(result);
    }

    [Fact]
    public void IsMdnsDisabledInRegistry_ReturnsFalse_WhenRegistryThrowsException()
    {
        _registryService.GetInt32(RegistryHive.LocalMachine, SubKey, ValueName)
            .Throws(new InvalidOperationException("Registry error"));

        var result = DnsCacheRegistryHelper.IsMdnsDisabledInRegistry(_registryService);

        Assert.False(result);
    }
}

