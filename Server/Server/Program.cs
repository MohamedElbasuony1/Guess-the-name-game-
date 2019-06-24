using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Server
{
    class Program
    {
        static Thread ThreadServer;
        //static
        static TcpListener server;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to our server");
            //server = new TcpListener(new IPAddress(new byte[] { 172,16,8,101}), 6867);
            server = new TcpListener(new IPAddress(new byte[] { 127,0,0,1 }), 6867);
            server.Start();
            ThreadServer = new Thread(ServerListen);
            ThreadServer.Start();
        }
        static void ServerListen()
        {
            while(true)
            {
                Socket ClientSocket = server.AcceptSocket();
                bool flag = true;
                NetworkStream Stream= new NetworkStream(ClientSocket);
                while (flag)
                {
                    if (Stream.DataAvailable)
                    {
                        //store New Client info
                        string ClientName = new BinaryReader(Stream).ReadString();
                        Console.WriteLine(ClientName);
                        Client client =new Client(Stream, ClientName);
                        flag = false;
                    }
               }
            }
        }
        
    }
}
