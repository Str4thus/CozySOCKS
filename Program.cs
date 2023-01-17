using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CozySOCKS
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // based on https://rastamouse.me/socks4a-proxy-in-csharp/
            Socks4Proxy proxy = new Socks4Proxy(System.Net.IPAddress.Loopback);
            _ = proxy.Start();
            await Task.Delay(1000);
            proxy.Stop();
        }
    }
}
