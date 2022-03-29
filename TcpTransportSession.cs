using LiteNetLib.Utils;
using NetCoreServer;

namespace LiteNetLibManager
{
    public class TcpTransportSession : TcpSession
    {
        public long ConnectionId { get; private set; }

        private readonly TcpTransportServer _server;
        private readonly Buffer _readBuffer;
        private int _packetReadingSize;

        public TcpTransportSession(long connectionId, TcpTransportServer server) : base(server)
        {
            ConnectionId = connectionId;
            _server = server;
            _readBuffer = new Buffer(0);
            _packetReadingSize = 0;
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            _readBuffer.Resize(OptionReceiveBufferSize);
            _readBuffer.Clear();
            _packetReadingSize = 0;
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
                disconnectInfo = new LiteNetLib.DisconnectInfo()
                {
                    Reason = LiteNetLib.DisconnectReason.RemoteConnectionClose,
                },
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
            _readBuffer.Append(buffer, (int)offset, (int)size);
            if (_packetReadingSize == 0)
            {
                if (!ReadPacketSize())
                    return;
            }

            while (_packetReadingSize > 0 && _packetReadingSize <= _readBuffer.Size)
            {
                byte[] coppiedBuffer = new byte[_packetReadingSize];
                System.Buffer.BlockCopy(_readBuffer.Data, 0, coppiedBuffer, 0, _packetReadingSize);
                _server.EventQueue.Enqueue(new TransportEventData()
                {
                    connectionId = ConnectionId,
                    type = ENetworkEvent.DataEvent,
                    reader = new NetDataReader(coppiedBuffer),
                });
                // Remove readed data from buffer
                _readBuffer.Remove(0, _packetReadingSize);
                // Check next packet size
                if (!ReadPacketSize())
                    return;
            }
        }

        private bool ReadPacketSize()
        {
            if (_readBuffer.Size < 4)
            {
                _packetReadingSize = 0;
                return false;
            }

            _packetReadingSize =
                (_readBuffer.Data[0] << 24) |
                (_readBuffer.Data[1] << 16) |
                (_readBuffer.Data[2] << 8) |
                _readBuffer.Data[3];
            _readBuffer.Remove(0, 4);
            return true;
        }

        public bool SendPacket(int length, byte[] buffer)
        {
            return SendAsync(new byte[] {
                (byte)(length >> 24),
                (byte)(length >> 16),
                (byte)(length >> 8),
                (byte)length
            }) && SendAsync(buffer, 0, length);
        }
    }
}
