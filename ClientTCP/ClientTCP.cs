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
    /// An Event for when the server sends a message to the client.
    /// Message is in string format, and can be access through the
    /// Message property.
    /// </summary>
    public class ServerToApplicationEvent : EventArgs
    {
        private string p;
        /// <summary>
        /// The message that was sent from the server to this client.
        /// </summary>
        public string Message { get { return p; } set { p = value; } }

        public ServerToApplicationEvent(string p)
        {
            this.p = p;
        }

    }

    /// <summary>
    /// General-use TCP client to send String messages to a specified server.
    /// Procedure is to call ConnectToServer(...) on a new ClientTCP object,
    /// send messages by calling SendMessage(), and receiving messages from the
    /// server through the RaiseReceivedEvent event handler.
    /// Once application is finished with client, procedure is to call Shutdown()
    /// to shutdown connection socket to server.
    /// </summary>
    public class ClientTCP
    {
        private IPAddress serverIP;
        private int serverPort;
        private Socket clientSocket;
        /// <summary>
        /// Event handler for a raised ServerToApplicationEvent.  
        /// This is raised when a server sends a message to the client.
        /// </summary>
        public event EventHandler<ServerToApplicationEvent> RaiseReceivedEvent;
        private string data;
        private byte[] buffer = new byte[1024];

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ClientTCP()
        {     
        }

        /// <summary>
        /// Checks whether user provided port is a valid port.
        /// Currently, it only checks if port is not a restricted port number.
        /// </summary>
        private bool ValidPort(int port, out int portObject)
        {
            if (port < 1023 || port > 49150)
            {
                portObject = -1;
                return false;
            }
            portObject = port;
            return true;
        }

        /// <summary>
        /// Attempts to connect to a specified server at a given IP Address and Port Number.
        /// If the port number of IP Address is invalid, an error is thrown.
        /// If unable to connect to given server, an exception will be thrown.
        /// </summary>
        public string ConnectToServer(string ipAddress, int port)
        {
            // determine if given IP address and port will work
            if (!(IPAddress.TryParse(ipAddress, out serverIP)))
                throw new System.ArgumentException("Invalid IP address.");

            if (!(ValidPort(port, out serverPort)))
                throw new System.ArgumentException("Invalid port number.  Try a port number between 1025 and 49150.");

            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // connect socket to server
                clientSocket.Connect(new IPEndPoint(serverIP, serverPort));

                // start receiving from server as needed
                ReceiveMessage(clientSocket);
                return clientSocket.RemoteEndPoint.ToString();
            }
            catch (Exception e)
            {
                throw new Exception("Unable to connect to server.\n\n" + e.ToString());
            }
        }

        private void ReceiveMessage(Socket clientSocket)
        {
            try
            {
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to receive message from server: " + e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            clientSocket = (Socket)ar.AsyncState;

            // read data from client as much as specified ReceivedBufferObject allowed
            int readPacketData = clientSocket.EndReceive(ar);

            // if 0 bytes were read, client shutdown socket, and all available data has been received.
            if (readPacketData > 0)
            {
                // convert data to string and store in the state object
                string message = Encoding.ASCII.GetString(buffer, 0, readPacketData);
                data += message;

                try
                {
                    // if the end of the data was found, we're done
                    if (message.IndexOf('\n') > -1)
                    {
                        // tell listening application that server is communicating with it
                        ReceiveToApplication(new ServerToApplicationEvent(data));

                        // ask for more messages
                        data = "";
                        clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
                    }
                    // otherwise, retrieve more data
                    else
                    {
                        Console.WriteLine("Server sent partial message.");
                        clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
                    }
                }
                catch (Exception e )
                {
                    Console.WriteLine("Server closed socket:" + e.ToString());
                    Shutdown();
                }
            }
            else
            {
                Console.WriteLine("Warning: 0 packet sent from server.");
            }
        }

        private void ReceiveToApplication(ServerToApplicationEvent e)
        {
            EventHandler<ServerToApplicationEvent> handler = RaiseReceivedEvent;

            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Send message to server.  Need to successfully call ConnectToServer(...)
        /// before messages can be passed.  If the server has closed the socket,
        /// the method will indicate on the console.
        /// </summary>
        public void SendMessage(string message)
        {
            // encode message to bytes for sending
            // probably want to put this and the sending inside a different method
            byte[] packet = Encoding.ASCII.GetBytes(message);

            try
            {
                // send message
                clientSocket.BeginSend(packet, 0, packet.Length, SocketFlags.None, new AsyncCallback(SendCallback), packet.Length); // normally, last parameter is socket, but I'm just concerned with sending the full packet
            }
            catch (Exception e )
            {
                Console.WriteLine("Server closed socket:" + e.ToString());
                Shutdown();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            int totalBytes = (int)ar.AsyncState;
            try {
                int sentBytes = clientSocket.EndSend(ar);
                if (totalBytes != sentBytes)
                    throw new Exception("Only sent " + sentBytes + " bytes of " + totalBytes + " bytes.");
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }

        /// <summary>
        /// Will release the socket connecting the server to the client.
        /// Should be called when the application is finished communicating
        /// with the server.
        /// </summary>
        public void Shutdown()
        {
            // release socket
            try
            {
                clientSocket.Shutdown(SocketShutdown.Both);
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                clientSocket.Close();
            }
        }
    }
}
