using System;
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
    public partial class Watch : Form
    {
        Thread WatcherListen;
        NetworkStream Stream;
        BinaryWriter Bw;
        BinaryReader Br;
        string[] watcherMsg;
        string RoomId;
        string Word;
        string name;
        string Id;
        bool flag;

        public Watch(NetworkStream StreamCons,string Level,string Category,string OwnerName,string OpponentName,string RoomIdCons,string nameCons,string IdCons,string playingNow,string Wordlbl,string history,string WordCons)
        {
            InitializeComponent();
            Stream = StreamCons;
            WatcherListen = new Thread(Listen);
            Br = new BinaryReader(Stream);
            Bw = new BinaryWriter(Stream);

            lblCat.Text = Category;
            lblLevel.Text = Level;
            lblOwner.Text = OwnerName;
            lblOpponent.Text = OpponentName;
            lbl_word.Text = Wordlbl;
            Word = WordCons.ToLower();
            name = nameCons;
            Id = IdCons;
            RoomId = RoomIdCons;
            flag = true;

            if (history.Contains(","))
            {
                string[] chars=history.Split(',');
                for (int i = 0; i <chars.Length-1; i++)
                {
                    groupBox_key.Controls["btn" + chars[i].ToUpper()].Enabled = false;
                }
            }

            if (playingNow=="OwnerPlaying")
            {
                label_playnow1.Text = "Playing";
            }
            else
            {
                label_playnow2.Text = "Playing";
            }

            if (lbl_word.Text == Word && label_playnow1.Text == "Playing")
            {
                MessageBox.Show(lblOwner.Text + " won the Game");
            }
            else if (lbl_word.Text == Word && label_playnow2.Text == "Playing")
            {
                MessageBox.Show(lblOpponent.Text + " won the Game");
            }
            WatcherListen.Start();
            //skinEngine1.SkinFile = "WaveColor2.ssk";
        }

        void Listen()
        {
            try
            {
                while (flag)
                {
                    if (Stream.DataAvailable)
                    {
                        watcherMsg = Br.ReadString().Split(',');
                        switch (watcherMsg[0])
                        {
                            case "1":
                                this.Invoke(new Action(() =>
                                {
                                    groupBox_key.Controls["btn" + watcherMsg[1].ToUpper()].Enabled = false;
                                    lbl_word.Text = watcherMsg[3];
                                    if (watcherMsg[2] == "OwnerPlaying")
                                    {
                                        label_playnow1.Text = "Playing";
                                        label_playnow2.Text = "";
                                    }
                                    else
                                    {
                                        label_playnow2.Text = "Playing";
                                        label_playnow1.Text = "";
                                    }

                                    if (lbl_word.Text == Word && label_playnow1.Text == "Playing")
                                    {
                                        MessageBox.Show(lblOwner.Text + " won the Game");
                                        Close();
                                    }
                                    else if (lbl_word.Text == Word && label_playnow2.Text == "Playing")
                                    {
                                        MessageBox.Show(lblOpponent.Text + " won the Game");
                                        Close();
                                    }
                                }));
                                break;
                            case "2":
                                MessageBox.Show(watcherMsg[1]);
                                this.Invoke(new Action(() => Close()));
                                break;
                        }

                    }
                }
            }
            catch (IOException)
            {
                MessageBox.Show("server is disconnected,close and try again");
            }
           
        }

        private void Watch_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                flag = false;
                Bw.Write("8," + RoomId + "," + Id);
                Thread goWelcome = new Thread(() => Application.Run(new Welcome(Stream, name)));
                goWelcome.SetApartmentState(ApartmentState.STA);
                goWelcome.Start();
            }
            catch (Exception)
            {}
            
        }

        private void Watch_Load(object sender, EventArgs e)
        {
            skinEngine1.SkinFile = "WaveColor2.ssk";
        }

    }
}
