using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ExtensionMethods;

namespace CozySOCKS
{
    public class Socks4Proxy
    {
        private readonly int _bindPort;
        private readonly IPAddress _bindAddress;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public Socks4Proxy(IPAddress bindAddress = null, int bindPort = 1080) {
            _bindPort = bindPort;
            _bindAddress = bindAddress;
        }

        public async Task Start()
        {
            TcpListener listener = new TcpListener(_bindAddress, _bindPort);
            listener.Start(100);
            Console.WriteLine("Listening on {0}:{1}", _bindAddress.ToString(), _bindPort);
            while (!_tokenSource.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                Thread thread = new Thread(async () => await HandleClient(client));
                thread.Start();
            }

            listener.Stop();
        }

        public void Stop()
        {
            _tokenSource.Cancel();
        }

        private async Task HandleClient(TcpClient client)
        {
            // Setup Connection
            Socks4Request request = await ReadConnectRequest(client);
            Console.WriteLine(request);
            bool isVersionFour = request != null;
            await SendConnectReply(client, isVersionFour);

            if (!isVersionFour)
            {
                Console.WriteLine("Received invalid connection!");
                client.Close();
                return;
            }

            if (request.DestinationAddress.ToString().StartsWith("0.0.0."))
            {
                Console.WriteLine("Failed to resolve {0}!", request.Domain);
                client.Close();
                return;
            }

            // Relaying Traffic
            TcpClient destination = new TcpClient();
            try
            {
                await destination.ConnectAsync(request.DestinationAddress, request.DestinationPort);
            } catch (SocketException ex)
            {
                Console.WriteLine("Cannot connect to destination {0}:{1}!", request.DestinationAddress, request.DestinationPort);
                client.Close();
                return;
            }

            while (!_tokenSource.IsCancellationRequested)
            {
                if (client.DataAvailable())
                {
                    var req = await client.ReadClient();
                    await destination.WriteClient(req);
                }

                if (destination.DataAvailable())
                {
                    var resp = await destination.ReadClient();
                    await client.WriteClient(resp);
                }

                await Task.Delay(100);
            }

            client.Close();
        }

        private async Task<Socks4Request> ReadConnectRequest(TcpClient client)
        {
            byte[] raw = await client.ReadClient();
            Console.WriteLine(raw.ToHex());
            return raw.Length == 0 ? null : await Socks4Request.FromBytes(raw);
        }

        private async Task SendConnectReply(TcpClient client, bool success)
        {
            var reply = new byte[]
            {
                0x00,
                success ? (byte)0x5a : (byte)0x5b,
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };

            await client.WriteClient(reply);
        }
    }

    internal class Socks4Request
    {
        public CommandCode Command { get; private set; }
        public int DestinationPort { get; private set; }
        public IPAddress DestinationAddress { get; private set; }
        public int UserID { get; private set; }
        public string Domain { get; private set; }

        public static async Task<Socks4Request> FromBytes(byte[] raw)
        {
            if (raw[0] != 4) 
            {
                return null; // packet is not SOCKS4(a)
            }

            var request = new Socks4Request()
            {
                Command = (CommandCode)raw[1],
                DestinationPort = raw.ReadBytes(2, 3).GetInt(),
                DestinationAddress = new IPAddress(raw.ReadBytes(4, 7)),
            };

            int userIdSize = raw.NumberOfBytesUntilNull(8);
            request.UserID = raw.ReadBytes(8, 8 + userIdSize).GetInt();

            // if SOCKS4a
            if (request.DestinationAddress.ToString().StartsWith("0.0.0."))
            {
                request.Domain = raw.ReadBytesToEnd(8 + userIdSize + 1).GetString();

                try
                {
                    var lookup = await Dns.GetHostAddressesAsync(request.Domain);

                    // Find first IPv4 address that the domain resolves to
                    request.DestinationAddress = lookup.First(i => i.AddressFamily == AddressFamily.InterNetwork);
                } catch(SocketException e)
                {
                }
            }

            return request;
        }

        public override string ToString()
        {
            return string.Format("Command: {0}\nDestinationPort: {1}\nDestinationAddress: {2}\nUserID: {3}\nDomain: {4}\n", Command, DestinationPort, DestinationAddress, UserID, Domain); 
        }

        public enum CommandCode : byte
        {
            CONNECT = 0x01,
            BIND = 0x02
        }
    }
}
