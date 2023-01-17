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
            Socks4Proxy proxy = new Socks4Proxy(System.Net.IPAddress.Loopback);
            await proxy.Start();
        }
    }
}
