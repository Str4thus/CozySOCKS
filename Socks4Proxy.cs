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
            NetworkStream stream = client.GetStream();
            if (!stream.CanRead)
            {
                Console.WriteLine("Could not read from client!");
            }
            else 
            {
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                Console.WriteLine("Read {0} bytes", readBytes);
                Console.WriteLine(buffer.ToHex(readBytes));

                byte version = buffer[0];
                byte cmdCode = buffer[1];
                byte destinationPort = buffer.SubArray(2, 3).ToValue();
                IPAddress destinationIp = new IPAddress(buffer.SubArray(4,7));

                Console.WriteLine("Version: {0}", version);
                Console.WriteLine("Command: {0}", cmdCode);
                Console.WriteLine("Destination Port: {0}", destinationPort);
                Console.WriteLine("Destination IP: {0}", destinationIp);
            }
        }

        private async Task SendConnectReply(NetworkStream stream, bool success)
        {
            var reply = new byte[]
            {
                0x00,
                success ? (byte)0x5a : (byte)0x5b,
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };

            await stream.WriteAsync(reply, 0, reply.Length);
        }


    }
}
