﻿using LiteNetLib.Utils;
using NetCoreServer;
using System;
using System.Net.Sockets;

namespace LiteNetLibManager
{
    public class TcpTransportSession : TcpSession
    {
        public long ConnectionId { get; private set; }

        private readonly TcpTransportServer _server;

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

        protected override void OnError(SocketError error)
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
            byte[] coppiedBuffer = new byte[size];
            Array.Copy(buffer, offset, coppiedBuffer, 0, size);
            _server.EventQueue.Enqueue(new TransportEventData()
            {
                connectionId = ConnectionId,
                type = ENetworkEvent.DataEvent,
                reader = new NetDataReader(coppiedBuffer),
            });
        }
    }
}
