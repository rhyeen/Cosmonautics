using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    /// <summary>
    /// Test client for Cosmo server.  Sends a simple "Testing\n" message
    /// to a IP address and port # of a server, then shuts down.
    /// </summary>
    class ClientC
    {
        private static ClientTCP client;

        /// <summary>
        /// Argument [0] = server IP address
        /// Argument [1] = server port
        /// </summary>
        static void Main(string[] args)
        {
            int serverPort;
            // remove later
            args = new string[] { "127.0.0.1", "44444" };

            // check if argument was a correct IP address
            if (args.Length != 2)
            {
                Console.WriteLine("Need two arguments. Received " + args.Length);
                Console.WriteLine("Expected arguments: <string> server IP address <port> server port");
                return;
            }

            if (!(Int32.TryParse(args[1], out serverPort)))
            {
                Console.WriteLine("First argument should be an integer.");
                Console.WriteLine("Expected arguments: <string> server IP address <port> server port");
                return;
            }

            try
            {
                client = new ClientTCP();
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("Expected arguments: <string> server IP address <port> server port");
                return;
            }

            try
            {
                string serverInfo = client.ConnectToServer(args[0], serverPort);
                Console.WriteLine("Successfully connected to server " + serverInfo + ".");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "\n");
                Console.WriteLine("Exiting program.");
                return;
            }

            client.SendMessage("Testing\n");

            client.Shutdown();

        }
    }
}
