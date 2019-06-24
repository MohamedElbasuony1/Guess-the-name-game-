using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Client
{
    public partial class Home : Form
    {
        Thread GoWelcome;
        NetworkStream Stream;
        string Name;
        public Home()
        {
            InitializeComponent();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (txtName.Text!=string.Empty)
            {
                try
                {
                    //TcpClient client = new TcpClient("172.16.8.101", 6867);
                    TcpClient client = new TcpClient("127.0.0.1", 6867);
                    Stream = client.GetStream();
                    Name = txtName.Text;
                    new BinaryWriter(Stream).Write(Name);
                    GoWelcome = new Thread(openWelcome);
                    GoWelcome.SetApartmentState(ApartmentState.STA);
                    Close();
                    GoWelcome.Start();
                    
                }
                catch (Exception Ex)
                {
                    MessageBox.Show(Ex.Message);
                }
            }
            else
                MessageBox.Show("Enter the name");
        }

        void openWelcome()
        {
            Application.Run(new Welcome(Stream,Name));
        }

        private void Home_Load(object sender, EventArgs e)
        {
            skinEngine1.SkinFile = "WaveColor2.ssk";
        }

    }
}
