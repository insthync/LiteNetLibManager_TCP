using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LiteNetLibManager
{
    public sealed class TcpTransport : ITransport, ITransportConnectionGenerator
    {
        private long nextConnectionId = 1;
        private TcpTransportClient client;
        private TcpTransportServer server;
        public bool IsClientStarted
        {
            get { return client != null && client.IsConnected; }
        }
        public bool IsServerStarted
        {
            get { return server != null && server.IsStarted; }
        }
        public int ServerPeersCount
        {
            get
            {
                if (server != null)
                    return server.PeersCount;
                return 0;
            }
        }
        public int ServerMaxConnections
        {
            get
            {
                if (server != null)
                    return server.MaxConnections;
                return 0;
            }
        }

        public TcpTransport() { }

        public void Destroy()
        {
            StopClient();
            StopServer();
        }

        public bool ClientReceive(out TransportEventData eventData)
        {
            eventData = default(TransportEventData);
            if (client == null)
                return false;
            if (client.EventQueue.Count == 0)
                return false;
            return client.EventQueue.TryDequeue(out eventData);
        }

        public bool ClientSend(byte dataChannel, DeliveryMethod deliveryMethod, NetDataWriter writer)
        {
            if (IsClientStarted)
            {
                return client.SendPacket(writer.Length, writer.Data);
            }
            return false;
        }

        public bool StartClient(string address, int port)
        {
            if (IsClientStarted)
            {
                Logging.Log(nameof(TcpTransport), "Client started, so it can't be started again");
                return false;
            }

            IPAddress[] ipAddresses = Dns.GetHostAddresses(address);
            if (ipAddresses.Length == 0)
            {
                Logging.Log(nameof(TcpTransport), "Cannot find IP addresses from " + address);
                return false;
            }

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
            {
                Logging.Log(nameof(TcpTransport), "Cannot find index of address from " + address);
                return false;
            }

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

        public bool ServerDisconnect(long connectionId)
        {
            return server != null && server.Disconnect(connectionId);
        }

        public bool ServerReceive(out TransportEventData eventData)
        {
            eventData = default(TransportEventData);
            if (!IsServerStarted)
                return false;
            if (server.EventQueue.Count == 0)
                return false;
            return server.EventQueue.TryDequeue(out eventData);
        }

        public bool ServerSend(long connectionId, byte dataChannel, DeliveryMethod deliveryMethod, NetDataWriter writer)
        {
            return server != null && server.SendPacket(connectionId, writer.Length, writer.Data);
        }

        public bool StartServer(int port, int maxConnections)
        {
            if (IsServerStarted)
                return false;
            server = new TcpTransportServer(this, IPAddress.Any, port, maxConnections);
            server.OptionDualMode = true;
            server.OptionNoDelay = true;
            return server.Start();
        }

        public void StopServer()
        {
            if (server != null)
                server.Dispose();
            server = null;
            nextConnectionId = 1;
        }

        public long GetNewConnectionID()
        {
            return Interlocked.Increment(ref nextConnectionId);
        }
    }
}
