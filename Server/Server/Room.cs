using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Room
    {
        public Thread ThreadOwner { set; get; }
        public Thread ThreadOpponent { set; get; }
        public Client Owner { set; get; }
        Client opponent;
        int ownerCount;
        int OpponentCount;
        public Client Opponent
        {
            get
            {
                return opponent;
            }
            set
            {
                Join = false;
                NumOfPlayers = 2;
                opponent = value;
            }
        }

        public string Category { set; get; }
        public string Level { set; get; }
        public string Word { set; get; }
        public string PlayingNow;
        public string CurrentWord { set; get; }

        public int NumOfPlayers { set; get; }
        static int Count { set; get; }//identity
        public int Id { set; get; }//uniqueKey
        public int WatcherId { set; get; }
       
        public bool Join { set; get; }
        public bool OpponentThreadFlag;
        public bool OwnerThreadFlag;
        public bool? OpponentReplyFlag = null;        
 
        public static Dictionary<int, Room> Rooms { set; get; }
        public Dictionary<int, Client> Watchers { set; get; }
        
        public StringBuilder History{ set; get;}
        StreamWriter file;

        public Room(Client OwnerCons,string CategoryCons,string LevelCons)
        {
            History = new StringBuilder();
            Watchers = new Dictionary<int, Client>();
            file = File.AppendText(@".\score.txt");
            History.Append("");
            Owner = OwnerCons;
            Category = CategoryCons;
            Level = LevelCons;
            PlayingNow = "OwnerPlaying";
            WatcherId = 0;
            Id = ++Count;//room id
            ownerCount = 0;
            OpponentCount = 0;
            NumOfPlayers=1;
            Join = true;
            SelectedWord();
            Rooms.Add(Id, this);
        }

        void SelectedWord()
        {
            SqlConnection con = new SqlConnection("Data Source=.;Initial Catalog=GuessTheName;Integrated Security=True");
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "SelectWord";
            cmd.Parameters.Add(new SqlParameter("@Category", Category));
            cmd.Parameters.Add(new SqlParameter("@Level", Level));
            con.Open();
            Word=(string)cmd.ExecuteScalar();
            con.Close();
        }

       public void OwnerListen(Client OwnerFun)
        {
            while (OwnerThreadFlag)
            {
                if (OwnerFun.Stream.DataAvailable)
                {
                    //int RoomId;
                    string[] ClientMsg = OwnerFun.Br.ReadString().Split(',');
                    bool flag = true;
                    switch (ClientMsg[0])
                    {
                        case "1":
                            Opponent.Bw.Write("letter," + ClientMsg[1]);
                            PlayingNow = ClientMsg[2];
                            CurrentWord=ClientMsg[3];
                            History.Append(ClientMsg[1] + ",");
                            foreach (Client watcher in Watchers.Values)
                            {
                               // watcher.ThreadClient.Suspend();
                               watcher.Bw.Write("1,"+ClientMsg[1] + "," + PlayingNow + "," + CurrentWord);
                               //watcher.ThreadClient.Resume();
                            }

                            break;
                        case "2":
                            if (PlayingNow == "OwnerPlaying")
                            {
                                ownerCount++;
                            }
                            else
                            {
                                OpponentCount++;
                            }
                            while (flag)
                            {
                                if (OpponentReplyFlag!=null)
                                {
                                    SelectedWord();
                                    flag = false;
                                    History.Clear();
                                    if ((bool)OpponentReplyFlag)
                                    {
                                        Opponent.Bw.Write("2accepted," + Word);
                                        Owner.Bw.Write("2accepted," + Word);
                                    }
                                    else
                                    {
                                        Score();
                                        Owner.Bw.Write("1accepted,"+ Word);
                                        Opponent = null;
                                        NumOfPlayers = 1;
                                        Join = true;
                                        OwnerThreadFlag = false;
                                        
                                        Owner.ThreadClient.Resume();
                                    }
                                    OpponentReplyFlag = null;
                                }
                            }

                            break;
                        case "3":
                            if (PlayingNow == "OwnerPlaying")
                            {
                                ownerCount++;
                            }
                            else
                            {
                                OpponentCount++;
                            }
                            Score();
                            Owner.ThreadClient.Resume();
                            NumOfPlayers = 1;
                            Join = true;
                            History.Clear();
                            while (flag)
                            {
                                if (OpponentReplyFlag != null)
                                {
                                    flag = false;
                                    if ((bool)OpponentReplyFlag)
                                    {
                                        OpponentThreadFlag = false;
                                        OwnerThreadFlag = false;
                                        SelectedWord();
                                        Opponent.Bw.Write("Opponentaccepted," + Word);
                                        // Owner.ThreadClient.Resume();
                                        Opponent.ThreadClient.Resume();
                                        Owner = Opponent;
                                        Opponent = null;
                                        NumOfPlayers = 1;
                                        Join = true;
                                    }
                                    else
                                    {
                                        Rooms.Remove(Id);
                                        OwnerThreadFlag = false;
                                    }
                                }
                            }
                            OpponentReplyFlag = null;
                                break;
                        case "4":
                                OpponentCount++;
                                Score();
                                foreach (Client watcher in Watchers.Values)
                                {
                                    watcher.Bw.Write("2," + Owner.Name + " leave the room");
                                }
                                SelectedWord();
                                OpponentThreadFlag = false;
                                OwnerThreadFlag = false;
                                Opponent.Bw.Write("Ownerleave,"+Word);
                                Opponent.ThreadClient.Resume();
                                Owner.ThreadClient.Resume();
                                Owner = Opponent;
                                Opponent = null;
                                NumOfPlayers = 1;
                                Join = true;
                                History.Clear();
                                //Rooms.Remove(Id);
                                break;
                    }
                }
            }
        }

        public void OpponentListen(Client OpponentFun)
        {
            while (OpponentThreadFlag)
            {
                if (OpponentFun.Stream.DataAvailable)
                {
                    //int RoomId;
                    //'1,A'
                    string[] ClientMsg = OpponentFun.Br.ReadString().Split(',');
                    switch (ClientMsg[0])
                    {
                        //send Letter
                        case "1":
                            Owner.Bw.Write("letter,"+ClientMsg[1]);
                            PlayingNow = ClientMsg[2];
                            CurrentWord=ClientMsg[3];
                            History.Append(ClientMsg[1] + ",");
                            foreach (Client watcher in Watchers.Values)
                            {
                                watcher.Bw.Write("1," + ClientMsg[1] + "," + PlayingNow + "," + CurrentWord);
                            }
                            break;
                            //opponent play agian accept
                        case "2":
                            OpponentReplyFlag = true;
                            break;
                        //opponent play agian refuse
                        case "3":
                            OpponentReplyFlag = false;
                            OpponentThreadFlag = false;
                            Opponent.ThreadClient.Resume();
                            break;
                            //oppponent sudden close
                        case "4":
                            ownerCount++;
                            Score();
                            foreach (Client watcher in Watchers.Values)
                            {
                                watcher.Bw.Write("2,"+Opponent.Name+" leave the room");
                            }
                            OpponentThreadFlag = false;
                            OwnerThreadFlag = false;
                            SelectedWord();
                            Owner.Bw.Write("Opponentleave,"+Word);
                            Opponent.ThreadClient.Resume();
                            Owner.ThreadClient.Resume();
                            Opponent = null;
                            NumOfPlayers = 1;
                            Join = true;
                            History.Clear();
                            break;
                    }
                }
            }
        }

        static Room()
        {
            Count = 0;
            Rooms = new Dictionary<int, Room>();
        }

        public override string ToString()
        {
            string Msg;
            if(Opponent!=null)
            {
                Msg = Id.ToString() + ',' + Owner.Name + ',' + Opponent.Name + ',' + NumOfPlayers + ',' + Level + ',' + Category + ',' + Join.ToString() + ',' + Word + ';';
            }
            else
            {
                Msg = Id.ToString() + ',' + Owner.Name + ',' + "" + ',' + NumOfPlayers + ',' + Level + ',' + Category + ',' + Join.ToString() + ',' + Word + ';';
            }
            return Msg;
        }

       void Score()
        {
           file.WriteLine(Owner.Name + ": " + ownerCount + ", " + Opponent.Name + ": " + OpponentCount);
           file.Close();
           ownerCount = 0;
           OpponentCount = 0;
        }
    }
}
