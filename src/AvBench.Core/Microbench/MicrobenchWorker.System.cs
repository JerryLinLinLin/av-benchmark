using System.Diagnostics;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32;
using AvBench.Core.Internal;
using AvBench.Core.Models;

namespace AvBench.Core.Microbench;

public static partial class MicrobenchWorker
{
    private const uint TokenQueryAccess = 0x0008;
    private const int TokenQueryBufferSize = 1024;
    private const int CryptoPayloadSize = 64 * 1024;

    private static MicrobenchMetrics ExecuteNetConnectLoopback(int totalOperations)
    {
        var payload = new byte[1024];
        var response = new byte[payload.Length];
        Random.Shared.NextBytes(payload);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start(16);
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        var serverTask = Task.Run(() => RunLoopbackEchoServer(listener, totalOperations, payload.Length));

        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            using (var client = new TcpClient(AddressFamily.InterNetwork))
            {
                client.NoDelay = true;
                client.Connect(endpoint.Address, endpoint.Port);
                using var stream = client.GetStream();
                stream.Write(payload, 0, payload.Length);
                ReadExact(stream, response, response.Length);
            }

            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        serverTask.GetAwaiter().GetResult();
        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteDnsResolve(int totalOperations)
    {
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            var addresses = Dns.GetHostAddresses("localhost");
            if (addresses.Length == 0)
            {
                throw new InvalidOperationException("DNS resolution returned no addresses for localhost.");
            }

            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteRegistryCrud(int totalOperations)
    {
        const string BasePath = @"Software\AvBench\Temp";
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var subKeyPath = $@"{BasePath}\bench_{index:D5}";
            var start = Stopwatch.GetTimestamp();
            using (var key = Registry.CurrentUser.CreateSubKey(subKeyPath, writable: true)
                   ?? throw new InvalidOperationException($"Failed to create registry key HKCU\\{subKeyPath}."))
            {
                key.SetValue("Name", $"bench-{index}", RegistryValueKind.String);
                key.SetValue("Number", index, RegistryValueKind.DWord);
                key.SetValue("Blob", new byte[] { 1, 2, 3, 4 }, RegistryValueKind.Binary);
                key.SetValue("Paths", new[] { "a", "b", "c" }, RegistryValueKind.MultiString);
                key.SetValue("Expand", @"%TEMP%\avbench", RegistryValueKind.ExpandString);

                _ = key.GetValue("Name");
                _ = key.GetValue("Number");
                _ = key.GetValue("Blob");
                _ = key.GetValue("Paths");
                _ = key.GetValue("Expand");
                _ = key.GetValueNames();
            }

            Registry.CurrentUser.DeleteSubKeyTree(subKeyPath, throwOnMissingSubKey: false);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        Registry.CurrentUser.DeleteSubKeyTree(BasePath, throwOnMissingSubKey: false);
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecutePipeRoundtrip(int totalOperations)
    {
        var payload = new byte[4096];
        var response = new byte[payload.Length];
        Random.Shared.NextBytes(payload);

        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var pipeName = $"avbench-pipe-{System.Environment.ProcessId}-{index:D5}";
            var serverTask = Task.Run(() =>
            {
                using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None);
                server.WaitForConnection();
                var buffer = new byte[payload.Length];
                ReadExact(server, buffer, buffer.Length);
                server.Write(buffer, 0, buffer.Length);
                server.Flush();
            });

            var start = Stopwatch.GetTimestamp();
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None);
            client.Connect(5_000);
            client.Write(payload, 0, payload.Length);
            client.Flush();
            ReadExact(client, response, response.Length);
            serverTask.GetAwaiter().GetResult();
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteTokenQuery(int totalOperations)
    {
        var processHandle = GetCurrentProcess();
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            if (!OpenProcessToken(processHandle, TokenQueryAccess, out var tokenHandle))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken failed.");
            }

            try
            {
                var buffer = new byte[TokenQueryBufferSize];
                if (!GetTokenInformation(tokenHandle, TokenInformationClass.TokenPrivileges, buffer, buffer.Length, out _))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "GetTokenInformation failed.");
                }
            }
            finally
            {
                CloseHandle(tokenHandle);
            }

            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteCryptoHashVerify(int totalOperations)
    {
        var data = new byte[CryptoPayloadSize];
        Random.Shared.NextBytes(data);
        using var rsa = RSA.Create(2048);
        var signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            var hash = SHA256.HashData(data);
            if (!rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                throw new InvalidOperationException("RSA verification failed.");
            }

            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static void RunLoopbackEchoServer(TcpListener listener, int expectedConnections, int payloadLength)
    {
        for (var index = 0; index < expectedConnections; index++)
        {
            using var socket = listener.AcceptTcpClient();
            socket.NoDelay = true;
            using var stream = socket.GetStream();
            var buffer = new byte[payloadLength];
            ReadExact(stream, buffer, buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }
    }

    private static void ReadExact(Stream stream, byte[] buffer, int length)
    {
        var offset = 0;
        while (offset < length)
        {
            var read = stream.Read(buffer, offset, length - offset);
            if (read <= 0)
            {
                throw new EndOfStreamException($"Expected {length} bytes but reached end of stream after {offset} bytes.");
            }

            offset += read;
        }
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle,
        TokenInformationClass tokenInformationClass,
        byte[] tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    private enum TokenInformationClass
    {
        TokenPrivileges = 3
    }
}
