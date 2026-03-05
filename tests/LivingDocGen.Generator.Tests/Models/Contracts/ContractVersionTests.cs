using System;
using System.Collections.Generic;
using LivingDocGen.Generator.Models.Contracts;
using Xunit;

namespace LivingDocGen.Generator.Tests.Models.Contracts;

public class ContractVersionTests
{
    [Fact]
    public void SchemaVersion_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(ContractVersion.SchemaVersion));
    }

    [Fact]
    public void CompatibilityMinVersion_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(ContractVersion.CompatibilityMinVersion));
    }

    [Fact]
    public void GeneratorVersion_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(ContractVersion.GeneratorVersion));
    }

    [Fact]
    public void IsCompatible_CurrentVersion_ReturnsTrue()
    {
        Assert.True(ContractVersion.IsCompatible(ContractVersion.SchemaVersion));
    }

    [Fact]
    public void IsCompatible_NullVersion_ReturnsFalse()
    {
        Assert.False(ContractVersion.IsCompatible(null));
    }

    [Fact]
    public void IsCompatible_EmptyVersion_ReturnsFalse()
    {
        Assert.False(ContractVersion.IsCompatible(""));
    }

    [Fact]
    public void IsCompatible_WhitespaceVersion_ReturnsFalse()
    {
        Assert.False(ContractVersion.IsCompatible("   "));
    }

    [Fact]
    public void IsCompatible_SameMajorHigherMinor_ReturnsTrue()
    {
        // If min is 1.0, then 1.5 should be compatible
        Assert.True(ContractVersion.IsCompatible("1.5"));
    }

    [Fact]
    public void IsCompatible_DifferentMajor_ReturnsFalse()
    {
        Assert.False(ContractVersion.IsCompatible("2.0"));
    }

    [Fact]
    public void IsCompatible_MajorZero_ReturnsFalse()
    {
        Assert.False(ContractVersion.IsCompatible("0.1"));
    }

    [Fact]
    public void IsCompatible_InvalidFormat_ReturnsFalse()
    {
        Assert.False(ContractVersion.IsCompatible("abc"));
    }

    [Fact]
    public void IsCompatible_ThreePartVersion_ReturnsFalse()
    {
        Assert.False(ContractVersion.IsCompatible("1.0.0"));
    }

    [Fact]
    public void IsCompatible_SingleNumber_ReturnsFalse()
    {
        Assert.False(ContractVersion.IsCompatible("1"));
    }
}
