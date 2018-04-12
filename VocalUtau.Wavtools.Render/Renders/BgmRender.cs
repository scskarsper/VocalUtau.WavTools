using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.Wavtools.Render
{
    public class BgmRender : IRender
    {
        string _RendingFile = "";

        public string RendingFile
        {
            get { return _RendingFile; }
        }
        public string getRendingFile()
        {
            return _RendingFile;
        }

        bool _IsRending = false;

        public bool IsRending
        {
            get { return _IsRending; }
            set { _IsRending = value; }
        }
        public bool getIsRending()
        {
            return _IsRending;
        }

        public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler RendingStateChange;

        string CacheSignal = "";
        public BgmRender(string CacheSignal = "")
        {
            if (CacheSignal == "")
            {
                this.CacheSignal = CacheSignal = Guid.NewGuid().ToString();
            }
            else
            {
                this.CacheSignal = CacheSignal;
            }
        }


        bool _ExitRending = false;

        public void StopRending()
        {
            _ExitRending = true;
        }

        public void StartRending(System.IO.DirectoryInfo baseTempDir, List<VocalUtau.Calculators.NoteListCalculator.NotePreRender> NList, string RendToWav = "") { ;}


        private long InitFile(out FileStream Fs, string TrackFileName)
        {
            Fs = new FileStream(TrackFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            _RendingFile = TrackFileName;
            byte[] head = IOHelper.GenerateHead();
            Fs.Write(head, 0, head.Length);
            Fs.Seek(head.Length, SeekOrigin.Begin);
            return head.Length;
        }
        long headSize = 0;
        public void StartRending(System.IO.DirectoryInfo baseTempDir, List<VocalUtau.Calculators.BarkerCalculator.BgmPreRender> BList, string RendToWav = "")
        {
            _IsRending = true;
            _ExitRending = false;
            if (RendingStateChange != null) RendingStateChange(this);

            string ProcessIDStr = Process.GetCurrentProcess().Id.ToString();
            DirectoryInfo tempDir = baseTempDir.CreateSubdirectory("temp");
            DirectoryInfo cacheDir = baseTempDir.CreateSubdirectory("cache");

            string TrackFileName = tempDir.FullName + "\\Bgm_" + CacheSignal + ".wav";

            FileStream Fs;
            headSize = InitFile(out Fs, TrackFileName);
            Semaphore semaphore = new Semaphore(1, 1, "VocalUtau.WavTool." + ProcessIDStr + ".Bgm_" + CacheSignal);
            for (int i = 0; i < BList.Count; i++)
            {
                if(BList[i].DelayTime>0)
                {
                    int ByteTime = (int)(IOHelper.NormalPcmMono16_Format.AverageBytesPerSecond * BList[i].DelayTime);
                    ByteTime -= ByteTime % 2;
                    byte[] byteL=new byte[ByteTime];
                    Array.Clear(byteL, 0, ByteTime);
                    Fs.Write(byteL, 0, ByteTime);
                }
                semaphore.WaitOne();
                try
                {
                    int ByteTime = (int)(IOHelper.NormalPcmMono16_Format.AverageBytesPerSecond * BList[i].PassTime);
                    ByteTime -= ByteTime % 2;
                    using(NAudio.Wave.AudioFileReader reader = new NAudio.Wave.AudioFileReader(BList[i].FilePath))
                    {
                        int JumpLoops = ByteTime / 2;
                        NAudio.Wave.Wave32To16Stream w16 = new NAudio.Wave.Wave32To16Stream(reader);
                        while (w16.Position < w16.Length)
                        {
                            if (_ExitRending) break;
                            byte[] by = new byte[2];
                            int rd = w16.Read(by, 0, 2);
                            if (JumpLoops > 0)
                            {
                                JumpLoops--;
                            }
                            else
                            {
                                Fs.Write(by, 0, 2);
                            }
                            for (int w = 1; w < w16.WaveFormat.Channels; w++)
                            {
                                int rdr = w16.Read(by, 0, 2);
                            }
                        }
                    }
                }
                catch { ;}
                Fs.Flush();
                semaphore.Release();
                if (_ExitRending) break;
            }
            _IsRending = false;
            long total = Fs.Length;
            byte[] head = IOHelper.GenerateHead((int)(total - headSize));
            Fs.Seek(0, SeekOrigin.Begin);
            Fs.Write(head, 0, head.Length);
            Fs.Flush();
            Fs.Close();
            _ExitRending = false;
            if (RendingStateChange != null) RendingStateChange(this);
            if (RendToWav != "")
            {
                File.Copy(TrackFileName, RendToWav, true);
                try
                {
                    File.Delete(TrackFileName);
                }
                catch { ;}
            }
        }
    }
}
