using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace VocalUtau.Pipe.Demo.Host
{
    public partial class MainForm : Form
    {
        NamedPipe_Server WavToolPipe = new NamedPipe_Server("WavTool.test");
        NamedPipe_Server ResamplerPipe = new NamedPipe_Server("Resampler.test");
        public MainForm(string[] args)
        {
            InitializeComponent();
            WavToolPipe.StartServer();
            ResamplerPipe.StartServer();
            WavToolPipe.RecieveArgs += WavToolPipe_RecieveArgs;
            ResamplerPipe.RecieveArgs += ResamplerPipe_RecieveArgs;
        }

        void ResamplerPipe_RecieveArgs(string[] Args)
        {
            this.Invoke(new Action(() =>
            {
                ListViewItem lvi = new ListViewItem("{resampler} "+String.Join(" ", Args));
                lvi.Tag = Args;
                listView2.Items.Add(lvi);
            }));
        }

        void WavToolPipe_RecieveArgs(string[] Args)
        {
            this.Invoke(new Action(()=>{
                ListViewItem lvi = new ListViewItem("{wavtool} " + String.Join(" ", Args));
                lvi.Tag = Args;
                listView1.Items.Add(lvi);
            }));
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        public string GetMD5(string myString)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] fromData = System.Text.Encoding.Unicode.GetBytes(myString);
            byte[] targetData = md5.ComputeHash(fromData);
            string byte2String = null;
            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String += targetData[i].ToString("x");
            }
            return byte2String;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count == listView2.Items.Count)
            {
                //resampler<==>wavtool
                Dictionary<ListViewItem, ListViewItem> lvid = new Dictionary<ListViewItem, ListViewItem>();
                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    ListViewItem resamp = listView2.Items[i];
                    string[] resamparg = (string[])resamp.Tag;
                    ListViewItem wavtoo = listView1.Items[i];
                    string[] wavtooarg = (string[])wavtoo.Tag;
                    if (resamparg[1] == wavtooarg[1])
                    {
                        string rhash = String.Join("|", resamparg);
                        resamparg[1]= GetMD5(rhash) + ".wav";
                        wavtooarg[1] = resamparg[1];
                        resamp.Tag = resamparg;
                        wavtoo.Tag = wavtooarg;
                        listView2.Items[i].Text = "{resampler} " + String.Join(" ", resamparg);
                        listView1.Items[i].Text = "{wavtool} " + String.Join(" ", wavtooarg);
                    }
                }

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            listView2.Items.Clear();
        }

        private void RunAndWait(string FileName, string[] Args)
        {
            string cmdarg = "";
            foreach (string s in Args)
            {
                cmdarg = cmdarg + " " + (s.IndexOf(" ") == -1 ? s : "\"" + s + "\"");
            }
            cmdarg = cmdarg.Trim();
            Process proc = new Process();
            proc.StartInfo = new ProcessStartInfo();
            proc.StartInfo.Arguments = cmdarg;
            proc.StartInfo.FileName = FileName;
            proc.Start();
            proc.WaitForExit();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string res = @"D:\Console-DebugWavToo\UAtau\engines\model4.exe";
            RunAndWait(res, ((string[])listView2.Items[0].Tag));
        }
    }
}
