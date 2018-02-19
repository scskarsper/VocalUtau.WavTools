﻿using System;
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

namespace VocalUtau.Wavtools.BPlayer
{
    public partial class Form1 : Form
    {
        FileStream Fs;
        FileStream Bs;
        Pipe_Server pserver;
        BufferedPlayer bplayer;
        double prebufftime = 3000;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void InitFile()
        {
            Fs = new FileStream("D:\\Tmp1.wav", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            byte[] EBF = WavFile_Heads.Init_EmptyFile(uint.MaxValue);
            Fs.Write(EBF, 0, EBF.Length);
            Fs.Seek(44, SeekOrigin.Begin);
        }
        private void InitPlayer()
        {
            bplayer = new BufferedPlayer(Fs, true);
            bplayer.InitPlayer();
            bplayer.Buffer_Play();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InitFile();
            InitPlayer();
            timer1.Enabled = true;
            pserver = new Pipe_Server("VocalUtau.WavTool.PPC",Fs,44);
            pserver.StartServer(); 
            prebufftime = 1000;
            pserver.RecieveEndSignal += pserver_RecieveEndSignal;
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
            WavFile_Heads.wfh_putlength(Fs, (int)total);
            Fs.Close();
            pserver.ExitServer();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int bfb = (int)((bplayer.BufferPercent / 0.8) * 100);
            progressBar1.Value = bfb < 100 ? bfb : 100;
            int bf2 = (int)((bplayer.UntallPercent / 0.9) * 100);
            progressBar2.Value = bf2 < 100 ? bf2 : 100;
            int bf3 = (int)((bplayer.StreamPercent / 1) * 100);
            progressBar3.Value = bf3 < 100 ? bf3 : 100;
            bplayer.FillBuffer(WavFile_Datas.MsTime2BytesCount(prebufftime));
        }
    }
}