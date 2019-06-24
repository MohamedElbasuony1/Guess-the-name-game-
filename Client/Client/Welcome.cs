using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Welcome : Form
    {
        Thread GoCreateRoom;
        NetworkStream Stream;
        BinaryReader Br;
        BinaryWriter Bw;
        string name;
        string[] rooms;
        

        public Welcome(NetworkStream streamCons,string nameCons)
        {
            InitializeComponent();
            Stream = streamCons;
            Bw = new BinaryWriter(streamCons);
            Br = new BinaryReader(streamCons);
            name = nameCons;
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            GoCreateRoom = new Thread(openCreateRoom);
            GoCreateRoom.SetApartmentState(ApartmentState.STA);
            Close();
            GoCreateRoom.Start();
        }

        private void Welcome_Load(object sender, EventArgs e)
        {
            RefreshPage();
            skinEngine1.SkinFile = "WaveColor2.ssk";
        }

        void openCreateRoom()
        {
            Application.Run(new CreateRoom(Stream, name));
        }
        void RefreshPage()
        {
            try
            {
                bool flag = true;
                Bw.Write("1");//request to get rooms from server
                tableLayoutPanel1.Controls.Clear();
                tableLayoutPanel1.RowCount = 1;
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
                        RoomCtr Room = new RoomCtr(Stream, RoomInfo[0], RoomInfo[1], RoomInfo[2], RoomInfo[3], RoomInfo[4], RoomInfo[5], Convert.ToBoolean(RoomInfo[6]), RoomInfo[7], name, tableLayoutPanel1);
                        tableLayoutPanel1.Controls.Add(Room, 1, tableLayoutPanel1.RowCount);
                        tableLayoutPanel1.RowCount++;
                    }
                }
            }
            catch (IOException)
            {
                MessageBox.Show("server is not connected,close and try again");
                //Close();
            }
            
        }
        private void btnrefresh_Click(object sender, EventArgs e)
        {
            RefreshPage();
        }
    }
}
