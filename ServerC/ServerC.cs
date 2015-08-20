using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    /// <summary>
    /// Since we need separate buffers for each thread, we can store the buffered data inside separate objects.
    /// </summary>
    class ReceivedBufferObject
    {
        public int ClientNumber { get { return number; } }
        public string Data { get { return message; } set { message = value; } }
        private string message;
        private int number;
        public Socket socket = null;
        public byte[] buffer = new byte[1024]; // 1024 is pretty standard buffer size for sockets, can change later if needed.

        public ReceivedBufferObject(Socket s, int n, int bufferSize)
        {
            socket = s;
            number = n;
            buffer = new byte[bufferSize];
        }

        public ReceivedBufferObject(Socket s, int n)
        {
            socket = s;
            number = n;
        }
    }

    /// <summary>
    /// A console-based server for Cosmonautics
    /// </summary>
    class ServerC
    {
        private static int serverPort = 0; // 0 is an invalid port number.

        // Count of clients, not too useful, but good for debugging purposes
        private static int clientCount = 0;
        private static List<ReceivedBufferObject> playerList;

        // lock for clientCount, since we can't use ints as lock
        private static Object lockCount = new Object();

        /// <summary>
        ///  MRE used for infinite loop in listener.  Loop will wait for connection to be made before continuing.
        /// </summary>
        private static ManualResetEvent mre = new ManualResetEvent(false);

        /// <summary>
        /// 
        /// args[0] = port number, which for now is automatically set to 44444
        /// </summary>
        public static void Main(string[] args)
        {
            playerList = new List<ReceivedBufferObject>();

            // remove later
            args = new string[] { "44444" };

            // check if argument was a correct port
            if (args.Length > 1)
            {
                Console.WriteLine("Too many arguments.");
                Console.WriteLine("Expected arguments: <int> port number");
                return;
            }

            if (!(Int32.TryParse(args[0], out serverPort)))
            {
                Console.WriteLine("First argument should be an integer.");
                Console.WriteLine("Expected arguments: <int> port number");
                return;
            }

            if (!(ValidPort()))
            {
                Console.WriteLine("Invalid port number.  Try a port number between 1025 and 49150.");
                Console.WriteLine("Expected arguments: <int> port number");
                return;
            }

            // attempt to connect at that port, listen for incoming requests
            Listen();
        }

        /// <summary>
        /// Checks whether user provided port is a valid port.
        /// Currently, it only checks if port is not a restricted port number.
        /// </summary>
        private static bool ValidPort()
        {
            if (serverPort < 1023 || serverPort > 49150)
                return false;
            /*
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
                Console.WriteLine(port);
            */
            return true;
        }

        /// <summary>
        /// Start listening on portServer for incoming TCP connection requests.
        /// Start handshake, then pass to connection socket.
        /// </summary>
        private static void Listen()
        {
            try
            {
                // create TCP IPv4 socket for listening.
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // bind listener socket to the given port
                listener.Bind(new IPEndPoint(IPAddress.Any, serverPort));

                // allow for 50 clients to connect to this server
                listener.Listen(50); // modify number later as needed

                Console.WriteLine("Server started on port " + serverPort + ".");

                while (true)
                {
                    // block current thread
                    mre.Reset();

                    Console.WriteLine("Waiting for a client...");
                    listener.BeginAccept(new AsyncCallback(ConnectionCallback), listener);

                    // continue to block thread until mre.Set() is read
                    mre.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Listener socket failed: " + e.ToString());
            }
        }

        private static void SendMessage(String message, ReceivedBufferObject toClient)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(message);

            toClient.socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), toClient.socket);
            Console.WriteLine("Sending: " + message + "to Client " + toClient.ClientNumber + ".");
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket toClient = (Socket)ar.AsyncState;
                toClient.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine("Send was unsuccessful: " + e.ToString());
            }
        }

        /// <summary>
        /// Callback is made once connection is successfully made with a client.
        /// Handle client request with a new socket.
        /// </summary>
        private static void ConnectionCallback(IAsyncResult ar)
        {
            // allow the listener to continue with new threads
            mre.Set();
            Console.WriteLine("Client connected.");

            // create socket that handles clients
            Socket connection = ((Socket)ar.AsyncState).EndAccept(ar);

            // create an object that will store the received data from the client
            ReceivedBufferObject storageContainer;
            lock (lockCount)
            {
                clientCount++;
                storageContainer = new ReceivedBufferObject(connection, clientCount);
                playerList.Add(storageContainer);
            }
            Console.WriteLine("Waiting for message from client " + storageContainer.ClientNumber + "...");

            // wait for message from client
            connection.BeginReceive(storageContainer.buffer, 0, storageContainer.buffer.Length, SocketFlags.None, new AsyncCallback(JoinCallback), storageContainer);
        }

        /// <summary>
        /// When a client connects to the server successfully, the callback to join will be made to receive the client's messages.
        /// </summary>
        private static void JoinCallback(IAsyncResult ar)
        {
            ReceivedBufferObject storageContainer = (ReceivedBufferObject)ar.AsyncState;
            Socket clientSocket = storageContainer.socket;

            // read data from client as much as specified ReceivedBufferObject allowed
            int readPacketData = clientSocket.EndReceive(ar);

            // if 0 bytes were read, client shutdown socket, and all available data has been received.
            if (readPacketData > 0)
            {
                // convert data to string and store in the state object
                string message = Encoding.ASCII.GetString(storageContainer.buffer, 0, readPacketData);
                storageContainer.Data += message;

                // if the end of the data was found, we're done
                if (message.IndexOf('\n') > -1)
                {
                    Console.WriteLine("Client " + storageContainer.ClientNumber + " sent message: " + storageContainer.Data);

                    // echo changes to other clients
                    lock (lockCount)
                    {
                        foreach (ReceivedBufferObject player in playerList)
                        {
                            if (player.ClientNumber != storageContainer.ClientNumber)
                            {
                                SendMessage(storageContainer.ClientNumber + " " + storageContainer.Data, player);
                            }
                        }
                    }
                    storageContainer.Data = "";
                    // ask for more messages
                    clientSocket.BeginReceive(storageContainer.buffer, 0, storageContainer.buffer.Length, SocketFlags.None, new AsyncCallback(JoinCallback), storageContainer);
                }
                // otherwise, retrieve more data
                else
                {
                    Console.WriteLine("Client " + storageContainer.ClientNumber + " sent partial message.");
                    clientSocket.BeginReceive(storageContainer.buffer, 0, storageContainer.buffer.Length, SocketFlags.None, new AsyncCallback(JoinCallback), storageContainer);
                }
            }
            else
            {
                Console.WriteLine("Warning: 0 packet sent from client " + storageContainer.ClientNumber + ".");
            }

        }
    }
}
