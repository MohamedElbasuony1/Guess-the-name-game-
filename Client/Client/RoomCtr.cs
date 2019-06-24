using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    public partial class RoomCtr : UserControl
    {
        public string Word { set; get; }
        public string LoginName { set; get; }
        public string IdRoom { get; set; }
        public string SetLevel { set { lblLevel.Text = value; } }
        public string SetCatogry { set { lblCategory.Text = value; } }
        public string SetOwner { set { lblPlayer1.Text = value; } }
        public string SetPlayer { set { lblPlayer2.Text = value; } }
        public string SetNumOfPlayer { set { lblNoOfPlayers.Text = value; } }
        public bool btnJoinEnable { set { btnJoin.Enabled = value; } }
        public bool btnWatchEnable { set { btnWatch.Enabled = value; } }
        NetworkStream Stream;
        BinaryWriter Bw;
        BinaryReader Br;
        string OwnerName;
        string Category;
        string Level;
        string getPlayer; 
        Thread GoPlay;
        TableLayoutPanel table;
        string[] rooms;

        public RoomCtr(NetworkStream streamCons,string IdCons,string OwnerCons, string PlayerCons, string NumOfPlayerCons, string LevelCons, string CatogroyCons,bool JoinCons,string WordCons,string LoginNameCons,TableLayoutPanel tableCons)
        {
            InitializeComponent();
            table = tableCons;
            getPlayer = PlayerCons;
            LoginName = LoginNameCons;
            Word = WordCons;
            Stream = streamCons;
            Bw = new BinaryWriter(streamCons);
            Br = new BinaryReader(streamCons);
            IdRoom=IdCons;
            SetLevel = Level = LevelCons;
            SetCatogry = Category = CatogroyCons;
            SetOwner = OwnerName =OwnerCons;
            SetPlayer = PlayerCons;
            SetNumOfPlayer = NumOfPlayerCons;
            btnJoinEnable = JoinCons;
            btnWatchEnable = !JoinCons;
        }

        private void btnJoin_Click(object sender, EventArgs e)
        {
            try
            {
                Bw.Write("3," + IdRoom);
                //wait for the request
                bool flag = true;
                string[] OwnerMsg;

                while (flag)
                {
                    if (Stream.DataAvailable)
                    {
                        OwnerMsg = Br.ReadString().Split(',');
                        switch (OwnerMsg[0])
                        {
                            case "1":
                                GoPlay = new Thread(delegate() { Application.Run(new Play(Stream, IdRoom, Word, OwnerName, Category, Level, LoginName)); });
                                GoPlay.SetApartmentState(ApartmentState.STA);
                                this.ParentForm.Close();
                                GoPlay.Start();
                                break;
                            case "2":
                                MessageBox.Show(OwnerMsg[1]);
                                break;
                            default:
                                MessageBox.Show(OwnerMsg[0]);
                                RefreshPage();
                                break;
                        }
                        flag = false;
                    }
                }
            }
            catch (IOException)
            {
                MessageBox.Show("Server is disconnected,close and try again");
            }
            
        }

        private void btnWatch_Click(object sender, EventArgs e)
        {
            try
            {
                Bw.Write("6," + IdRoom);
                bool flag = true;
                while (flag)
                {
                    if (Stream.DataAvailable)
                    {
                        string[] WatcherMsg = Br.ReadString().Split(';');//case,watcherid,Playingnow,currentword;history
                        string[] Watcherinfo = WatcherMsg[0].Split(',');
                        flag = false;
                        switch (Watcherinfo[0])
                        {
                            case "1":
                                Thread goWatch = new Thread(() => Application.Run(new Watch(Stream, Level, Category, OwnerName, getPlayer, IdRoom, LoginName, Watcherinfo[1], Watcherinfo[2], Watcherinfo[3], WatcherMsg[1], Word)));
                                goWatch.SetApartmentState(ApartmentState.STA);
                                this.ParentForm.Close();
                                goWatch.Start();
                                break;
                            case "2":
                                MessageBox.Show(Watcherinfo[1]);
                                RefreshPage();
                                break;
                        }
                    }
                }
            }
            catch (IOException)
            {

                MessageBox.Show("Server is disconnected,close and try again");
            }
        }

        void RefreshPage()
        {
            try
            {
                bool flag = true;
                Bw.Write("1");//request to get rooms from server
                table.Controls.Clear();
                table.RowCount = 1;
                while (flag)
                {
                    if (Stream.DataAvailable)
                    {
                        rooms = Br.ReadString().Split(';');
                        flag = false;
                    }
                }
                if (rooms[0].Contains(","))
                {
                    for (int i = 0; i < rooms.Length - 1; i++)
                    {
                        string[] RoomInfo = rooms[i].Split(',');
                        RoomCtr Room = new RoomCtr(Stream, RoomInfo[0], RoomInfo[1], RoomInfo[2], RoomInfo[3], RoomInfo[4], RoomInfo[5], Convert.ToBoolean(RoomInfo[6]), RoomInfo[7], LoginName, table);
                        table.Controls.Add(Room, 1, table.RowCount);
                        table.RowCount++;
                    }
                }
            }

            catch (IOException)
            {

                MessageBox.Show("Server is disconnected,close and try again");
            }
        }

    }
}
