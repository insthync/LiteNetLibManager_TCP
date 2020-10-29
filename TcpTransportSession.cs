using LiteNetLib.Utils;
using NetCoreServer;

namespace LiteNetLibManager
{
    public class TcpTransportSession : TcpSession
    {
        public long ConnectionId { get; private set; }

        private TcpTransportServer _server;

        public TcpTransportSession(long connectionId, TcpTransportServer server) : base(server)
        {
            ConnectionId = connectionId;
            _server = server;
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            _server.EventQueue.Enqueue(new TransportEventData()
            {
                connectionId = ConnectionId,
                type = ENetworkEvent.ConnectEvent,
            });
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();
            _server.EventQueue.Enqueue(new TransportEventData()
            {
                connectionId = ConnectionId,
                type = ENetworkEvent.DisconnectEvent,
            });
        }

        protected override void OnError(System.Net.Sockets.SocketError error)
        {
            base.OnError(error);
            _server.EventQueue.Enqueue(new TransportEventData()
            {
                connectionId = ConnectionId,
                type = ENetworkEvent.ErrorEvent,
                socketError = error,
            });
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            base.OnReceived(buffer, offset, size);
            _server.EventQueue.Enqueue(new TransportEventData()
            {
                connectionId = ConnectionId,
                type = ENetworkEvent.DataEvent,
                reader = new NetDataReader(buffer, (int)offset, (int)size),
            });
        }
    }
}
