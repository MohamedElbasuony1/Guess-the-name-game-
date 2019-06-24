using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Client
    {
        public NetworkStream Stream { get; set; } 
        public BinaryWriter Bw{get;set;}
        public BinaryReader Br { get; set; }
        public string Name { get; set; }
        public Thread ThreadClient{get;set;}
        public bool FlagClientConnected { set; get; }
        
        public Client(NetworkStream streamCons,string nameCons)
        {
            FlagClientConnected=true;
            Stream = streamCons;
            Br = new BinaryReader(streamCons);
            Bw = new BinaryWriter(streamCons);
            Name = nameCons;
            ThreadClient = new Thread(() => ClientListen(this));
            ThreadClient.Start();
        }

        void ClientListen(Client client)
        {
            while (FlagClientConnected)
            {
                if (client.Stream.DataAvailable)
                {
                    string[] ClientMsg = client.Br.ReadString().Split(',');
                    int RoomId;
                    switch (ClientMsg[0])
                    {
                        case "1":
                                string Rooms="";
                                foreach (Room item in Room.Rooms.Values)
                                {
                                    Rooms += item.ToString();
                                }
                                    //Console.WriteLine(Rooms.Substring(0,Rooms.Length-1));
                                    client.Bw.Write(Rooms);
                                break;                          
                        //"2,Movies,hard"
                        case "2":
                                //catch id of room
                                Room room =new Room(client, ClientMsg[1], ClientMsg[2]);
                                client.Bw.Write(room.Id.ToString()+','+room.Word);
                                break;
                            //"3,2"  Join Rquest
                        case "3":
                                RoomId = int.Parse(ClientMsg[1]);
                                if (Room.Rooms.ContainsKey(RoomId))
                                {
                                    if (Room.Rooms[RoomId].Opponent == null)
                                    {
                                        Room.Rooms[RoomId].Opponent = client;
                                        Room.Rooms[RoomId].Owner.Bw.Write(client.Name + ": wants to play with you");
                                    }
                                    else
                                    {
                                        client.Bw.Write("Refresh and Try again");
                                    }
                                }
                                else
                                {
                                    client.Bw.Write("Room does not exist anymore!! Please Refresh and try agian");
                                }
                                
                            break;
                            //confirm
                        case "4":
                            RoomId = int.Parse(ClientMsg[1]);
                            Room.Rooms[RoomId].CurrentWord = ClientMsg[2];
                            Room.Rooms[RoomId].Opponent.Bw.Write("1");
                            Room.Rooms[RoomId].ThreadOwner=new Thread(()=>Room.Rooms[RoomId].OwnerListen(client));
                            Room.Rooms[RoomId].ThreadOpponent = new Thread(() => Room.Rooms[RoomId].OpponentListen(Room.Rooms[RoomId].Opponent));
                            Room.Rooms[RoomId].OpponentThreadFlag = true;
                            Room.Rooms[RoomId].OwnerThreadFlag = true;
                            Room.Rooms[RoomId].ThreadOwner.Start();
                            Room.Rooms[RoomId].ThreadOpponent.Start();
                            Room.Rooms[RoomId].Opponent.ThreadClient.Suspend();
                            client.ThreadClient.Suspend();
                            break;
                            //refuse
                        case "5":
                            RoomId = int.Parse(ClientMsg[1]);
                            Room.Rooms[RoomId].Opponent.Bw.Write("2,request rejected");
                            Room.Rooms[RoomId].Opponent = null;
                            Room.Rooms[RoomId].NumOfPlayers=1;
                            Room.Rooms[RoomId].Join = true;  
                            break;
                        //watch
                        case "6":
                            RoomId = int.Parse(ClientMsg[1]);
                            if (Room.Rooms.ContainsKey(RoomId))
                            {
                                if (Room.Rooms[RoomId].Opponent != null)
                                {
                                    Room.Rooms[RoomId].WatcherId++;
                                    Room.Rooms[RoomId].Watchers.Add(Room.Rooms[RoomId].WatcherId, client);
                                    client.Bw.Write("1," + Room.Rooms[RoomId].WatcherId + "," + Room.Rooms[RoomId].PlayingNow + "," + Room.Rooms[RoomId].CurrentWord + ";" + Room.Rooms[RoomId].History);
                                }
                                else
                                {
                                    client.Bw.Write("2,Refresh and Try again");
                                }
                            }
                            else
                            {
                                client.Bw.Write("2,Room does not exist anymore, Please Refresh and try agian");
                            }
                            
                            break;
                        //watcher remove
                        case "8":
                            RoomId = int.Parse(ClientMsg[1]);
                            if (Room.Rooms.ContainsKey(RoomId))
                            { 
                               Room.Rooms[RoomId].Watchers.Remove(int.Parse(ClientMsg[2]));                                                                    
                            }
                            break;
                        //sudden close waiting for request
                        case "9":
                            Room.Rooms.Remove(int.Parse(ClientMsg[1]));
                            break;
                    }
                }
            }

        }
    }
}
