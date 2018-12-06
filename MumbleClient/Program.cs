using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MumbleSharp;
using MumbleSharp.Model;

namespace MumbleClient
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string addr, name, pass;
            int port;
            FileInfo serverConfigFile = new FileInfo(Environment.CurrentDirectory + "\\server.txt");
                using (StreamReader reader = new StreamReader(serverConfigFile.OpenRead()))
                {
                    addr = reader.ReadLine();
                    port = int.Parse(reader.ReadLine());
                    name = reader.ReadLine();
                    pass = reader.ReadLine();
                }

            ConsoleMumbleProtocol protocol = new ConsoleMumbleProtocol();
            MumbleConnection connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses(addr).First(a => a.AddressFamily == AddressFamily.InterNetwork), port), protocol);
            connection.Connect(name, pass, new string[0], addr);

            Thread t = new Thread(a => UpdateLoop(connection)) {IsBackground = true};
            t.Start();
            
            var r = new MicrophoneRecorder(protocol);
            r.Record();

            //When localuser is set it means we're really connected
            while (!protocol.ReceivedServerSync)
            {
            }

            Console.WriteLine("Connected as " + protocol.LocalUser.Id);

            DrawChannel("", protocol.Channels.ToArray(), protocol.Users.ToArray(), protocol.RootChannel);

            Console.ReadLine();
        }

        private static void DrawChannel(string indent, IEnumerable<Channel> channels, IEnumerable<User> users, Channel c)
        {
            Console.WriteLine(indent + c.Name + (c.Temporary ? "(temp)" : ""));

            foreach (var user in users.Where(u => u.Channel.Equals(c)))
            {
                if (string.IsNullOrWhiteSpace(user.Comment))
                    Console.WriteLine(indent + "-> " + user.Name);
                else
                    Console.WriteLine(indent + "-> " + user.Name + " (" + user.Comment + ")");
            }

            foreach (var channel in channels.Where(ch => ch.Parent == c.Id && ch.Parent != ch.Id))
                DrawChannel(indent + "\t", channels, users, channel);
        }

        private static void UpdateLoop(MumbleConnection connection)
        {
            while (connection.State != ConnectionStates.Disconnected)
            {
                connection.Process();
            }
        }
    }
}
