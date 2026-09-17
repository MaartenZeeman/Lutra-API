using System.Net;
using FluentAssertions;
using Lutra.Infrastructure.OpenRouter;

namespace Lutra.Infrastructure.OpenRouter.UnitTests;

public class PublicNetworkHttpHandlerTests
{
    [Theory]
    [InlineData("0.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("100.64.0.1")]
    [InlineData("100.127.255.254")]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.1.1")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.254")]
    [InlineData("192.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("198.18.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    public void IsPublicAddress_PrivateOrReservedIpv4_ReturnsFalse(string address)
    {
        PublicNetworkHttpHandler.IsPublicAddress(IPAddress.Parse(address)).Should().BeFalse();
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("172.32.0.1")]
    [InlineData("100.128.0.1")]
    public void IsPublicAddress_PublicIpv4_ReturnsTrue(string address)
    {
        PublicNetworkHttpHandler.IsPublicAddress(IPAddress.Parse(address)).Should().BeTrue();
    }

    [Theory]
    [InlineData("::1")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    [InlineData("ff02::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:10.0.0.1")]
    public void IsPublicAddress_PrivateOrReservedIpv6_ReturnsFalse(string address)
    {
        PublicNetworkHttpHandler.IsPublicAddress(IPAddress.Parse(address)).Should().BeFalse();
    }

    [Fact]
    public void IsPublicAddress_PublicIpv6_ReturnsTrue()
    {
        PublicNetworkHttpHandler.IsPublicAddress(IPAddress.Parse("2001:4860:4860::8888")).Should().BeTrue();
    }
}