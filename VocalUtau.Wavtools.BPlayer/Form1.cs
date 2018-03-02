using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VocalUtau.WavTools.Model.Pipe;
using VocalUtau.WavTools.Model.Wave;
using VocalUtau.WavTools.Model.Player;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.Wavtools.BPlayer
{
    public partial class Form1 : Form
    {
        FileStream Fs;
        FileStream Bs;
        Pipe_Server pserver;
        BufferedPlayer bplayer;
        long headSize = 0;
        double prebufftime = 1000;
        double delaybufftime = 3000;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private long InitFile()
        {
            Fs = new FileStream("D:\\Tmp1.wav", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            byte[] head = IOHelper.GenerateHead();
            Fs.Write(head, 0, head.Length);
            Fs.Seek(head.Length, SeekOrigin.Begin);
            return head.Length;
        }
        private void InitPlayer(long headSize)
        {
            FormatHelper fh = new FormatHelper(IOHelper.NormalPcmMono16_Format);
            bplayer = new BufferedPlayer(Fs, headSize+(long)fh.Ms2Bytes(delaybufftime));
            bplayer.InitPlayer();
            bplayer.Buffer_Play();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            headSize=InitFile();
            InitPlayer(headSize);
            timer1.Enabled = true;
            pserver = new Pipe_Server("VocalUtau.WavTool.PPC", Fs, (int)headSize);
            pserver.StartServer(); 
            prebufftime = 1000;
            pserver.RecieveEndSignal += pserver_RecieveEndSignal;
            button1.Enabled = false;
            button2.Enabled = true;
        }

        void pserver_RecieveEndSignal(long SignalData)
        {
            prebufftime = 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            bplayer.DisposePlayer();
            long total = Fs.Length;
            byte[] head = IOHelper.GenerateHead((int)(total - headSize));
            Fs.Seek(0, SeekOrigin.Begin);
            Fs.Write(head, 0, head.Length);
            Fs.Close();
            pserver.ExitServer();
            button2.Enabled = false;
            button1.Enabled = true;
            progressBar1.Value = 0;
            progressBar2.Value = 0;
            progressBar3.Value = 0;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int bfb = (int)((bplayer.BufferPercent / 0.8) * 100);
            progressBar1.Value = bfb < 100 ? bfb : 100;
            int bf2 = (int)((bplayer.UntallPercent / 0.9) * 100);
            progressBar2.Value = bf2 < 100 ? bf2 : 100;
            int bf3 = (int)((bplayer.StreamPercent / 1) * 100);
            progressBar3.Value = bf3 < 100 ? bf3 : 100;
            FormatHelper fh = new FormatHelper(IOHelper.NormalPcmMono16_Format);
            bplayer.FillBuffer(fh.Ms2Bytes(prebufftime));
        }
    }
}
