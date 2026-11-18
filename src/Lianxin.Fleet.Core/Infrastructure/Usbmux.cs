using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Lianxin.Fleet.Core.Infrastructure;

public sealed record UsbmuxDevice(int DeviceId, string Udid, string ConnectionType);

// usbmuxd plist 协议，与 pymobiledevice3/libusbmuxd 相同：u32 LE 总长度（含自身）+ version=1 + message=8 + tag + XML plist。
public static class Usbmux
{
    private const int MaxPacket = 1024 * 1024;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public static async Task<IReadOnlyList<UsbmuxDevice>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var socket = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var stream = new NetworkStream(socket, ownsSocket: false);
        var reply = await ExchangeAsync(stream, "ListDevices", [], cancellationToken).ConfigureAwait(false);
        if (!reply.TryGetValue("DeviceList", out var list) || list.Name.LocalName != "array")
            throw new InvalidDataException("usbmuxd 未返回设备列表。");
        return list.Elements("dict").Select(Dict).Where(item => item.ContainsKey("Properties")).Select(item =>
        {
            var properties = Dict(item["Properties"]);
            return new UsbmuxDevice(int.Parse(item["DeviceID"].Value, CultureInfo.InvariantCulture),
                properties.TryGetValue("SerialNumber", out var serial) ? serial.Value : "",
                properties.TryGetValue("ConnectionType", out var kind) ? kind.Value : "");
        }).ToArray();
    }

    // 返回直通手机端口的原始数据流；调用方负责释放。
    public static async Task<Stream> ConnectAsync(string udid, int devicePort, CancellationToken cancellationToken = default)
    {
        if (devicePort is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(devicePort));
        var target = Normalize(udid);
        var device = (await ListAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => Normalize(item.Udid) == target).OrderBy(item => item.ConnectionType == "USB" ? 0 : 1).FirstOrDefault()
            ?? throw new IOException("USB 未发现此设备，请检查连接、解锁及信任状态。");
        var socket = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var stream = new NetworkStream(socket, ownsSocket: true);
        try
        {
            var port = (ushort)IPAddress.HostToNetworkOrder((short)devicePort);
            var reply = await ExchangeAsync(stream, "Connect", [("DeviceID", device.DeviceId), ("PortNumber", (int)port)],
                cancellationToken).ConfigureAwait(false);
            var result = reply.TryGetValue("Number", out var number) ? number.Value : "";
            if (result != "0")
                throw new IOException($"usbmuxd 无法连接手机端口 {devicePort}（{result}），请确认控制通道已启动。");
            return stream;
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal static string Normalize(string udid) => udid.Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();

    private static async Task<Socket> OpenAsync(CancellationToken cancellationToken)
    {
        EndPoint endpoint = OperatingSystem.IsWindows() ? new IPEndPoint(IPAddress.Loopback, 27015)
            : new UnixDomainSocketEndPoint("/var/run/usbmuxd");
        var socket = new Socket(endpoint.AddressFamily, SocketType.Stream,
            endpoint is IPEndPoint ? ProtocolType.Tcp : ProtocolType.Unspecified);
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(Timeout);
            await socket.ConnectAsync(endpoint, deadline.Token).ConfigureAwait(false);
            return socket;
        }
        catch (Exception error) when (error is SocketException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            socket.Dispose();
            throw new IOException("无法连接 Apple USB 设备服务，请确认已安装 Apple 设备驱动。", error);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task<Dictionary<string, XElement>> ExchangeAsync(Stream stream, string messageType,
        (string Key, object Value)[] fields, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Timeout);
        await stream.WriteAsync(Packet(messageType, fields), deadline.Token).ConfigureAwait(false);
        var header = new byte[16];
        await stream.ReadExactlyAsync(header, deadline.Token).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (length is < 16 or > MaxPacket || BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4)) != 1
            || BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8)) != 8)
            throw new InvalidDataException("usbmuxd 响应格式无效。");
        var payload = new byte[length - 16];
        await stream.ReadExactlyAsync(payload, deadline.Token).ConfigureAwait(false);
        return Parse(payload);
    }

    internal static byte[] Packet(string messageType, (string Key, object Value)[] fields)
    {
        var dict = new XElement("dict");
        foreach (var (key, value) in fields.Prepend(("MessageType", messageType)).Concat([("ClientVersionString", "lianxin-fleet"),
            ("ProgName", "Lianxin.Fleet"), ("kLibUSBMuxVersion", 3)]))
            dict.Add(new XElement("key", key), value is int number
                ? new XElement("integer", number.ToString(CultureInfo.InvariantCulture)) : new XElement("string", (string)value));
        var xml = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" "
            + "\"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" + new XElement("plist", new XAttribute("version", "1.0"), dict)
            .ToString(SaveOptions.DisableFormatting));
        var packet = new byte[16 + xml.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(packet, (uint)packet.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8), 8);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(12), 1);
        xml.CopyTo(packet, 16);
        return packet;
    }

    internal static Dictionary<string, XElement> Parse(byte[] payload)
    {
        using var reader = XmlReader.Create(new MemoryStream(payload), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null
        });
        return Dict(XDocument.Load(reader).Root?.Element("dict") ?? throw new InvalidDataException("usbmuxd 响应不是字典。"));
    }

    private static Dictionary<string, XElement> Dict(XElement dictionary)
    {
        var values = dictionary.Elements().ToArray();
        if (dictionary.Name.LocalName != "dict" || values.Length % 2 != 0)
            throw new InvalidDataException("usbmuxd 字典格式无效。");
        var result = new Dictionary<string, XElement>(StringComparer.Ordinal);
        for (var i = 0; i < values.Length; i += 2)
            result[values[i].Value] = values[i + 1];
        return result;
    }
}
