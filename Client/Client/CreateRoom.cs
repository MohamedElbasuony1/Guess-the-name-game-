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
    public partial class CreateRoom : Form
    {
        Thread GoPlay;
        NetworkStream Stream;
        BinaryReader Br;
        BinaryWriter Bw;
        string[] IdWord;
        string name;
        bool flag;
        bool suddenClose;

        public CreateRoom(NetworkStream streamCons,string nameCons)
        {
            InitializeComponent();
            Stream = streamCons;
            Bw = new BinaryWriter(Stream);
            Br = new BinaryReader(Stream);
            name = nameCons;           
            suddenClose = true;
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                //request to create room
                Bw.Write("2," + cmbCategory.SelectedItem + "," + cmbLevel.SelectedItem);
                flag = true;
                suddenClose = false;
                while (flag)
                {
                    if (Stream.DataAvailable)
                    {
                        IdWord = Br.ReadString().Split(',');
                        flag = false;
                    }
                }
                GoPlay = new Thread(openPlay);
                GoPlay.SetApartmentState(ApartmentState.STA);
                Close();
                GoPlay.Start();
            }
            catch (IOException)
            {
                MessageBox.Show("server is disconnected,close and try again");
            }
            
        }

        void openPlay()
        {
            Application.Run(new Play(Stream, IdWord[0],IdWord[1], name, cmbCategory.SelectedItem.ToString(), cmbLevel.SelectedItem.ToString(),""));
        }

        private void CreateRoom_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (suddenClose)
            {
                Thread goWelcome = new Thread(() => Application.Run(new Welcome(Stream, name)));
                goWelcome.SetApartmentState(ApartmentState.STA);
                goWelcome.Start();   
            }
        }

        private void CreateRoom_Load(object sender, EventArgs e)
        {
            cmbCategory.SelectedIndex =cmbLevel.SelectedIndex =0;
            skinEngine1.SkinFile = "WaveColor2.ssk";
        }

    }
}
