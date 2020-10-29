using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace LiteNetLibManager
{
    public sealed class TcpTransport : ITransport
    {
        private TcpTransportClient client;
        private TcpTransportServer server;

        public TcpTransport() { }

        public void Destroy()
        {
            StopClient();
            StopServer();
        }

        public bool IsClientStarted()
        {
            return client != null && client.IsConnected;
        }

        public bool ClientReceive(out TransportEventData eventData)
        {
            eventData = default(TransportEventData);
            if (!IsClientStarted())
                return false;
            if (client.EventQueue.Count == 0)
                return false;
            return client.EventQueue.TryDequeue(out eventData);
        }

        public bool ClientSend(DeliveryMethod deliveryMethod, NetDataWriter writer)
        {
            if (IsClientStarted())
            {
                return client.SendAsync(writer.Data);
            }
            return false;
        }

        public bool StartClient(string address, int port)
        {
            IPAddress[] ipAddresses = Dns.GetHostAddresses(address);
            if (ipAddresses.Length == 0)
                return false;

            int indexOfAddress = -1;
            for (int i = 0; i < ipAddresses.Length; ++i)
            {
                if (ipAddresses[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    indexOfAddress = i;
                    break;
                }
            }

            if (indexOfAddress < 0)
                return false;

            client = new TcpTransportClient(ipAddresses[indexOfAddress], port);
            client.OptionDualMode = true;
            client.OptionNoDelay = true;
            return client.ConnectAsync();
        }

        public void StopClient()
        {
            if (client != null)
                client.Dispose();
            client = null;
        }

        public bool IsServerStarted()
        {
            return server != null && server.IsStarted;
        }

        public bool ServerDisconnect(long connectionId)
        {
            return server != null && server.Disconnect(connectionId);
        }

        public bool ServerReceive(out TransportEventData eventData)
        {
            eventData = default(TransportEventData);
            if (!IsServerStarted())
                return false;
            if (server.EventQueue.Count == 0)
                return false;
            return server.EventQueue.TryDequeue(out eventData);
        }

        public bool ServerSend(long connectionId, DeliveryMethod deliveryMethod, NetDataWriter writer)
        {
            return server != null && server.Send(connectionId, writer.Data);
        }

        public bool StartServer(int port, int maxConnections)
        {
            if (IsServerStarted())
                return false;
            server = new TcpTransportServer(IPAddress.Any, port);
            server.OptionDualMode = true;
            server.OptionNoDelay = true;
            return server.Start();
        }

        public void StopServer()
        {
            if (server != null)
                server.Dispose();
            server = null;
        }

        public int GetServerPeersCount()
        {
            if (server != null)
                return server.PeersCount;
            return 0;
        }
    }
}
