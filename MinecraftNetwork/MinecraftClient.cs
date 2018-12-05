using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using MinecraftHiveMind;
using MinecraftNetwork.Packets;
using MinecraftNetwork.Protocol;

namespace MinecraftNetwork
{
    public class MinecraftClient : IDisposable
    {
        public string Hostname { get; }
        public ushort Port { get; }

        public bool Connected => _client.Connected;

        private readonly TcpClient _client;
        private readonly NetworkStream _network;
        private readonly NotchianStream _notchian;

        public MinecraftClient(string hostname, ushort port)
        {
            Hostname = hostname;
            Port = port;

            Console.WriteLine("Connecting to client on {0}:{1}...", hostname, port);

            _client = new TcpClient(hostname, port);
            _network = _client.GetStream();
            _notchian = new NotchianStream(_network);

            Console.WriteLine("Connected!");
        }

        public void Login(string username)
        {
            Console.WriteLine("Logging in...");

            var pHandshake = new PHandshake
            {
                ProtocolVersion = 404,
                ServerAddress = Hostname,
                ServerPort = Port,
                NextState = 2
            };
            _notchian.FlushPacket(pHandshake);

            Debug.Header1("#>> Handshake packet <<#");
            Debug.PrintPacket(pHandshake);

            var pLoginStart = new PLoginStart
            {
                PlayerUsername = username
            };
            _notchian.FlushPacket(pLoginStart);

            Debug.Header1("#>> Login packet <<#");
            Debug.PrintPacket(pLoginStart);

            var pEncryptionRequest = _notchian.ReadPacket<PEncryptionRequest>();

            Debug.Header1("#>> Encryption response packet <<#");
            Debug.PrintPacket(pEncryptionRequest);
        }

        public void Dispose()
        {
            using (_client)
            using (_network)
            using (_notchian)
            { }
        }
    }
}
