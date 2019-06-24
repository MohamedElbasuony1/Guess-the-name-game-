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
    public partial class Play : Form
    {
        NetworkStream Stream;
        BinaryWriter Bw;
        BinaryReader Br;
        Thread PlayerListener;
        Thread GetMsgRequest;
        string trueName;
        string OwnerName;
        string OpponentName;
        string Category;
        string Level;
        string RoomId;
        string Word;
        bool flag;
        bool flagListen;
        bool suddenclose;

        StringBuilder HiddenWordBuilder = new StringBuilder();

        public Play(NetworkStream StreamCons, string RoomIdCons, string WordCons, string nameCons, string categoryCons, string levelCons, string OpponentNameCons)
        {
            InitializeComponent();
            suddenclose = true;
            Stream = StreamCons;
            Bw = new BinaryWriter(Stream);
            Br = new BinaryReader(Stream);
            GetMsgRequest = new Thread(Request);
            PlayerListener = new Thread(Listen);

            RoomId = RoomIdCons;
            OwnerName = nameCons;
            lblOwner.Text = nameCons;
            OpponentName = OpponentNameCons;
            lblOpponent.Text = OpponentNameCons;
            Category = categoryCons;
            lblCat.Text = categoryCons;
            Level = levelCons;
            lblLevel.Text = levelCons;

            Word = WordCons.ToLower();
            lbl_word.Hide();
            HiddenWordBuilder.Append(string.Empty.PadLeft(Word.Length, '-'));

            flag = true;
            flagListen = true;
            groupBox_key.Enabled = false;

            if (OpponentName == "")
            {
                GetMsgRequest.Start();
                trueName = nameCons;
            }
            else
            {
                label_playnow1.Text = "Playing";
                label_playnow2.Text = "";
                trueName = OpponentName;
                lbl_word.Text = Word;
                CreateSpace();
                lbl_word.Show();
                PlayerListener.Start();
            }
            //skinEngine1.SkinFile = "WaveColor2.ssk";
        }

        void Request()
        {
            try
            {
                string Msg;
                while (flag)
                {
                    if (Stream.DataAvailable)
                    {
                        Msg = Br.ReadString();
                        DialogResult Result = MessageBox.Show(Msg, "Request", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                        switch (Result)
                        {
                            case DialogResult.Yes:
                                //play
                                flag = false;
                                lblOpponent.Invoke(new Action(() =>
                                {
                                    lblOpponent.Text = Msg.Split(':')[0];
                                    OpponentName = Msg.Split(':')[0];
                                }));

                                lbl_word.Invoke(new Action(() =>
                                {
                                    groupBox_key.Enabled = true;
                                    label_playnow1.Text = "Playing";
                                    label_playnow2.Text = "";
                                    CreateSpace();
                                    lbl_word.Show();

                                }));

                                Bw.Write("4," + RoomId + "," + lbl_word.Text);
                                PlayerListener.Start();
                                break;
                            case DialogResult.No:
                                Bw.Write("5," + RoomId);
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

        void Listen()
        {
            try
            {
                while (flagListen)
                {
                    if (Stream.DataAvailable)
                    {
                        string[] s = Br.ReadString().Split(',');
                        switch (s[0])
                        {
                            case "letter":
                                //Require Invoke
                                groupBox_key.Invoke(new Action(() =>
                                {
                                    btn_replacemt(s[1]);
                                    groupBox_key.Controls["btn" + s[1]].Enabled = false;
                                    if (!Word.Contains(s[1]))
                                    {
                                        if (trueName == OpponentName)
                                        {
                                            label_playnow2.Text = "Playing";
                                            label_playnow1.Text = "";
                                        }
                                        else
                                        {
                                            label_playnow2.Text = "";
                                            label_playnow1.Text = "Playing";
                                        }
                                        groupBox_key.Enabled = true;
                                    }
                                }));
                                btn_replacementCheck();
                                break;
                            case "2accepted":
                                this.Invoke(new Action(() =>
                                {
                                    for (int i = 0; i < groupBox_key.Controls.Count; i++)
                                    {
                                        groupBox_key.Controls[i].Enabled = true;
                                    }
                                    Word = s[1].ToLower();
                                    lbl_word.Hide();
                                    HiddenWordBuilder.Clear();
                                    HiddenWordBuilder.Append(string.Empty.PadLeft(Word.Length, '-'));
                                    CreateSpace();
                                    lbl_word.Show();
                                }));
                                break;
                            case "1accepted":
                                this.Invoke(new Action(() =>
                                {
                                    flagListen = false;
                                    suddenclose = false;
                                    Thread gotoPlay = new Thread(() => Application.Run(new Play(Stream, RoomId, s[1], OwnerName, Category, Level, "")));
                                    gotoPlay.SetApartmentState(ApartmentState.STA);
                                    Close();
                                    gotoPlay.Start();
                                }));
                                break;
                            case "Opponentaccepted":
                                this.Invoke(new Action(() =>
                                {
                                    flagListen = false;
                                    suddenclose = false;
                                    Thread gotoPlay = new Thread(() => Application.Run(new Play(Stream, RoomId, s[1], OpponentName, Category, Level, "")));
                                    gotoPlay.SetApartmentState(ApartmentState.STA);
                                    Close();
                                    gotoPlay.Start();
                                }));
                                break;
                            case "Ownerleave":
                                MessageBox.Show(OwnerName + " has withdrawn", "Congratulations");
                                this.Invoke(new Action(() =>
                                {
                                    flagListen = false;
                                    suddenclose = false;
                                    Thread gotoPlay = new Thread(() => Application.Run(new Play(Stream, RoomId, s[1], OpponentName, Category, Level, "")));
                                    gotoPlay.SetApartmentState(ApartmentState.STA);
                                    Close();
                                    gotoPlay.Start();
                                }));
                                break;
                            case "Opponentleave":
                                suddenclose = false;
                                MessageBox.Show(OpponentName + " has withdrawn", "Congratulations");
                                this.Invoke(new Action(() =>
                                {
                                    flagListen = false;
                                    Thread gotoPlay = new Thread(() => Application.Run(new Play(Stream, RoomId, s[1], OwnerName, Category, Level, "")));
                                    gotoPlay.SetApartmentState(ApartmentState.STA);
                                    Close();
                                    gotoPlay.Start();
                                }));
                                break;
                            default:
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

        void CreateSpace()
        {
            string s = " ";
            if (Word.Contains(s))
            {
                List<int> Indexes = new List<int>();
                for (int i = Word.IndexOf(s); i > -1; i = Word.IndexOf(s, i + 1))
                {
                    Indexes.Add(i);
                }
                foreach (int i in Indexes)
                {
                    HiddenWordBuilder.Remove(i, s.Length);
                    HiddenWordBuilder.Insert(i, s);
                }
            }
            lbl_word.Text = HiddenWordBuilder.ToString();
        }


        private void btn_Click(object sender, EventArgs e)
        {
            string s;
            string playing;
            s = ((Button)sender).Text.ToLower();
            ((Button)sender).Enabled = false;
            if (Word.Contains(s))
            {
                if (label_playnow1.Text == "Playing")
                {
                    playing = "OwnerPlaying";
                }
                else
                {
                    playing = "OpponentPlaying";
                }
            }
            else
            {
                if (label_playnow1.Text == "Playing")
                {
                    playing = "OpponentPlaying";
                }
                else
                {
                    playing = "OwnerPlaying";
                }
            }
            btn_replacemt(s);
            try
            {
                Bw.Write("1," + s + "," + playing + "," + lbl_word.Text);
                btn_replacementCheck();
            }
            catch (IOException)
            {

                MessageBox.Show("server is disconnected,close and try again");
            }

        }


        private void btn_replacemt(string s)
        {
            if (Word.Contains(s))
            {
                // MessageBox.Show(s);
                List<int> Indexes = new List<int>();
                for (int i = Word.IndexOf(s); i > -1; i = Word.IndexOf(s, i + 1))
                {
                    Indexes.Add(i);
                }
                foreach (int i in Indexes)
                {
                    HiddenWordBuilder.Remove(i, s.Length);
                    HiddenWordBuilder.Insert(i, s);
                }
            }
            else
            {
                if (trueName == OpponentName)
                {
                    label_playnow2.Text = "";
                    label_playnow1.Text = "Playing";
                }
                else
                {
                    label_playnow2.Text = "Playing";
                    label_playnow1.Text = "";
                }
                groupBox_key.Enabled = false;
            }
            if (lbl_word.InvokeRequired)
            {
                lbl_word.Invoke(new Action(() => { lbl_word.Text = HiddenWordBuilder.ToString().ToLower(); }));
            }
            else
            {
                lbl_word.Text = HiddenWordBuilder.ToString().ToLower();
            }
        }

        void btn_replacementCheck()
        {
            DialogResult status = DialogResult.None;
            if (lbl_word.Text == Word && groupBox_key.Enabled)
                status = MessageBox.Show("Winner Winner chicken dinner !!\n Do you want to play again ?!", "Congratulation", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            else if (lbl_word.Text == Word)
                status = MessageBox.Show("Good luck next time !!\n Do you want to play again ?!", "Try again", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            switch (status)
            {
                case DialogResult.Yes:
                    Bw.Write("2");
                    break;
                case DialogResult.No:
                    Bw.Write("3");
                    flagListen = false;
                    suddenclose = false;
                    Thread goWelcome = new Thread(() => Application.Run(new Welcome(Stream, trueName)));
                    goWelcome.SetApartmentState(ApartmentState.STA);
                    this.Invoke(new Action(() => Close()));
                    goWelcome.Start();
                    break;
            }
        }

        private void Play_FormClosed(object sender, FormClosedEventArgs e)
        {
            //flagListen = false;
            try
            {
                if (suddenclose)
                {
                    if (OpponentName == "")
                    {
                        flag = false;
                        Bw.Write("9," + RoomId);
                        Thread goWelcome = new Thread(() => Application.Run(new Welcome(Stream, trueName)));
                        goWelcome.SetApartmentState(ApartmentState.STA);
                        goWelcome.Start();
                    }
                    else
                    {
                        flag = false;
                        flagListen = false;
                        Bw.Write("4");
                        Thread goWelcome = new Thread(() => Application.Run(new Welcome(Stream, trueName)));
                        goWelcome.SetApartmentState(ApartmentState.STA);
                        goWelcome.Start();
                    }
                }
            }
            catch (IOException)
            {
            }
        }

        private void Play_Load(object sender, EventArgs e)
        {
            skinEngine1.SkinFile = "WaveColor2.ssk";
        }
    }
}