using System.Net;
using System.Net.Sockets;

namespace Lianxin.Fleet.Core.OpenApi;

// 连接与正文的资源上限：每个远程地址的连接数、同时接收的正文总量。
public sealed partial class OpenApiServer
{
    private static IPAddress? RemoteAddress(TcpClient client)
    {
        try
        {
            return client.Client.RemoteEndPoint is IPEndPoint { Address: var address }
                ? address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address : null;
        }
        catch (Exception error) when (error is SocketException or ObjectDisposedException)
        {
            return null;
        }
    }

    // 每个远程地址的连接计数。回环地址只受总连接数限制（本机程序可以同时控制很多台手机）。
    private bool Unlimited(IPAddress? address) =>
        address is null || (IPAddress.IsLoopback(address) && !options.LimitLoopbackPerAddress);

    private bool TryEnter(IPAddress? address)
    {
        if (Unlimited(address))
            return true;
        var key = address!;
        lock (perAddress)
        {
            var count = perAddress.GetValueOrDefault(key);
            if (count >= options.MaxConnectionsPerAddress)
                return false;
            perAddress[key] = count + 1;
            return true;
        }
    }

    private void Leave(IPAddress? address)
    {
        if (Unlimited(address))
            return;
        var key = address!;
        lock (perAddress)
            if (perAddress.TryGetValue(key, out var count))
            {
                if (count <= 1)
                    perAddress.Remove(key);
                else
                    perAddress[key] = count - 1;
            }
    }

    // 读取正文前预留它的长度；所有连接正在接收的正文总量超过 MaxInFlightBodyBytes 时拒绝。
    private bool TryReserveBody(long length)
    {
        if (length <= 0)
            return true;
        if (Interlocked.Add(ref bodyBytes, length) <= options.MaxInFlightBodyBytes)
            return true;
        Interlocked.Add(ref bodyBytes, -length);
        return false;
    }

    private void ReleaseBody(long length)
    {
        if (length > 0)
            Interlocked.Add(ref bodyBytes, -length);
    }
}
