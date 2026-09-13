using System.Net;
using System.Net.Sockets;

namespace Lutra.Infrastructure.OpenRouter;

/// <summary>
/// Builds an <see cref="HttpClientHandler"/> that never connects to non-public addresses,
/// which protects against SSRF via user-supplied URLs and AI-suggested image URLs.
/// Redirects are disabled in favour of explicit, validated redirect following.
/// </summary>
public static class PublicNetworkHttpHandler
{
    public static SocketsHttpHandler Create()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var host = context.DnsEndPoint.Host;
                var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
                var publicAddresses = addresses.Where(IsPublicAddress).ToArray();

                if (publicAddresses.Length == 0)
                {
                    throw new HttpRequestException($"Host '{host}' does not resolve to a public address.");
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                try
                {
                    await socket.ConnectAsync(publicAddresses, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            return bytes[0] switch
            {
                0 or 10 or 127 => false,
                100 when bytes[1] is >= 64 and <= 127 => false,
                169 when bytes[1] == 254 => false,
                172 when bytes[1] is >= 16 and <= 31 => false,
                192 when bytes[1] == 168 => false,
                192 when bytes[1] == 0 && bytes[2] == 0 => false,
                198 when bytes[1] is 18 or 19 => false,
                >= 224 => false,
                _ => true
            };
        }

        var v6 = address.GetAddressBytes();

        if ((v6[0] & 0xFE) == 0xFC)
        {
            return false;
        }

        if (v6[0] == 0xFE && (v6[1] & 0xC0) == 0x80)
        {
            return false;
        }

        if (v6[0] == 0xFF)
        {
            return false;
        }

        return true;
    }
}
