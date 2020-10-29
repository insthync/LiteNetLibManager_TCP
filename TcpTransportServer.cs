using NetCoreServer;
using System.Collections.Concurrent;
using System.Net;

namespace LiteNetLibManager
{
    public class TcpTransportServer : TcpServer
    {
        public ConcurrentDictionary<long, TcpTransportSession> AcceptedClients { get; private set; }
        public ConcurrentQueue<TransportEventData> EventQueue { get; private set; }
        public int PeersCount { get { return AcceptedClients.Count; } }

        private long _nextConnectionId;

        public TcpTransportServer(IPAddress address, int port) : base(address, port)
        {
            AcceptedClients = new ConcurrentDictionary<long, TcpTransportSession>();
            EventQueue = new ConcurrentQueue<TransportEventData>();
            _nextConnectionId = 1;
        }

        protected override TcpSession CreateSession()
        {
            TcpTransportSession newSession = new TcpTransportSession(_nextConnectionId++, this);
            AcceptedClients.TryAdd(newSession.ConnectionId, newSession);
            return newSession;
        }

        internal bool SendPacket(long connectionId, int length, byte[] buffer)
        {
            return AcceptedClients.ContainsKey(connectionId) && AcceptedClients[connectionId].SendPacket(length, buffer);
        }

        internal bool Disconnect(long connectionId)
        {
            TcpTransportSession session;
            if (AcceptedClients.TryRemove(connectionId, out session))
            {
                session.Dispose();
                return true;
            }
            return false;
        }
    }
}