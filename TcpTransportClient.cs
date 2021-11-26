using LiteNetLib.Utils;
using NetCoreServer;
using System;
using System.Collections.Concurrent;
using System.Net;

namespace LiteNetLibManager
{
    public class TcpTransportClient : TcpClient
    {
        public ConcurrentQueue<TransportEventData> EventQueue { get; private set; }

        public TcpTransportClient(IPAddress address, int port) : base(address, port)
        {
            EventQueue = new ConcurrentQueue<TransportEventData>();
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            EventQueue.Enqueue(new TransportEventData()
            {
                type = ENetworkEvent.ConnectEvent,
            });
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();
            EventQueue.Enqueue(new TransportEventData()
            {
                type = ENetworkEvent.DisconnectEvent,
            });
        }

        protected override void OnError(System.Net.Sockets.SocketError error)
        {
            base.OnError(error);
            EventQueue.Enqueue(new TransportEventData()
            {
                type = ENetworkEvent.ErrorEvent,
                socketError = error,
            });
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            byte[] coppiedBuffer = new byte[size];
            Array.Copy(buffer, offset, coppiedBuffer, 0, size);
            EventQueue.Enqueue(new TransportEventData()
            {
                type = ENetworkEvent.DataEvent,
                reader = new NetDataReader(coppiedBuffer),
            });
        }
    }
}
