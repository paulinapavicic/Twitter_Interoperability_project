using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new XmlRpcListenerServer(8080); // Listen on port 8080
            server.AddService(typeof(WeatherService), "/");
            Console.WriteLine("XML-RPC server running on http://localhost:8080/");
            Console.WriteLine("Press Enter to exit.");
            server.Start();
            Console.ReadLine();
            server.Stop();
        }
    }
}
