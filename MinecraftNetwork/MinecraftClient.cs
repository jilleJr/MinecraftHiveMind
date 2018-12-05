using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
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

            Header1("#>> Handshake packet <<#");
            PrintPacket(pHandshake);

            var pLoginStart = new PLoginStart
            {
                PlayerUsername = username
            };
            _notchian.FlushPacket(pLoginStart);

            Header1("#>> Login packet <<#");
            PrintPacket(pLoginStart);

            var pEncryptionRequest = _notchian.ReadPacket<PEncryptionRequest>();

            Header1("#>> Encryption response packet <<#");
            PrintPacket(pEncryptionRequest);
        }

        private static void PrintPacket(IPacket packet)
        {
            using (var memory = new MemoryStream())
            using (var notchian = new NotchianStream(memory))
            {
                packet.Write(in notchian);

                Header2(">>Packet (hex):");

                byte[] array = memory.ToArray();
                int arrayCeil = 8* (int)Math.Ceiling(array.Length/8d);
                for (var row = 0; row < arrayCeil; row+=8)
                {
                    for (int i = row; i < row+8; i++)
                    {
                        if (i < array.Length)
                        {
                            Console.ForegroundColor = IsASCIIChar((char) array[i])
                                ? ConsoleColor.White : ConsoleColor.DarkGray;

                            Console.Write("{0:x2} ", array[i]);
                        }
                        else
                        {
                            Console.Write("   ");
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(" |  ");

                    for (int i = row; i < row + 8; i++)
                    {
                        if (i < array.Length)
                        {
                            Console.ForegroundColor = IsASCIIChar((char) array[i]) 
                                ? ConsoleColor.White : ConsoleColor.DarkGray;

                            Console.Write((char)array[i]);
                        }
                        else
                        {
                            Console.Write("   ");
                        }
                    }

                    Console.WriteLine();
                }
                Console.ResetColor();
            }
        }

        private static bool IsASCIIChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }

        public static void Header1(string header)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine(header);
            Console.ResetColor();
        }

        public static void Header2(string header)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine();
            Console.WriteLine(header);
            Console.ResetColor();
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
