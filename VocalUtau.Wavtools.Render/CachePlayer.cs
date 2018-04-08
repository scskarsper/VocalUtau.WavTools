
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VocalUtau.WavTools;
using VocalUtau.WavTools.Model.Pipe;
using VocalUtau.WavTools.Model.Player;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.Wavtools.Render
{
    internal class CachePlayer
    {
        long headSize = 0;
        const double defprebufftime = 1000;
        double prebufftime = 1000;
        Pipe_Server pserver;
        FileStream Fs;
        FileStream Bs;
        string PlayingFile = "";

        WaveStreamProvider wsp;
        WaveOut waveOut;
        //BufferedWaveProvider _bufferedWaveProvider = null;

        //public BufferedWaveProvider BufferedWaveProvider
        //{
        //    get { return _bufferedWaveProvider; }
        //}
        public event VocalUtau.Wavtools.Render.WaveStreamProvider.OnSyncPositionHandler SyncPosition;

        public void FillBufferSample()
        {
            byte[] buf = new byte[1024];
            int len = Bs.Read(buf, 0, buf.Length);
        //    _bufferedWaveProvider.AddSamples(buf, 0, len);
        }

        internal event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler CallTimer_Stop;
        internal event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler CallTimer_Start;

        NAudio.Wave.PlaybackState _PlayingStatus = NAudio.Wave.PlaybackState.Stopped;

        string CacheSignal = "";
        public CachePlayer(string CacheSignal="")
        {
            if (CacheSignal == "")
            {
                this.CacheSignal = CacheSignal = Guid.NewGuid().ToString();
            }
            else
            {
                this.CacheSignal = CacheSignal;
            }
            //timer.Elapsed += timer_Elapsed;
            //timer.Enabled = false;
            _PlayingStatus = NAudio.Wave.PlaybackState.Stopped;

            waveOut = new WaveOut();
            FormatHelper fh = new FormatHelper(IOHelper.NormalPcmMono16_Format);
            long TailLength = fh.Ms2Bytes(prebufftime);
            wsp = new WaveStreamProvider(IOHelper.NormalPcmMono16_Format, Bs, IOHelper.NormalPcmMono16_HeadLength, TailLength);
            wsp.SyncPosition += wsp_SyncPosition;
            waveOut.Init(wsp);
          //  _bufferedWaveProvider = new BufferedWaveProvider(IOHelper.NormalPcmMono16_Format);
          //  waveOut.Init(_bufferedWaveProvider);
        }

        void wsp_SyncPosition(Stream Stream)
        {
            if (SyncPosition != null) SyncPosition(Stream);
        }

        void Timer_Stop()
        {
            if (CallTimer_Stop != null) CallTimer_Stop(this);
        }
        void Timer_Start()
        {
            if (CallTimer_Start != null) CallTimer_Start(this);
        }

        public bool IsFull
        {
            get { return (Bs.Position >= Bs.Length) && (wsp.UnreadableTail == 0); }
        }
        public long Position
        {
            get
            {
                return Bs.Position;
            }
            set
            {
                if (Bs.Position < Bs.Length) Bs.Position = value;
            }
        }

        public bool CheckBufferStatus(long Position)
        {
            FormatHelper fh = new FormatHelper(IOHelper.NormalPcmMono16_Format);
            long TailLength = fh.Ms2Bytes(prebufftime);
            return (+TailLength < Bs.Length);
        }

        public void ResetPosition(long Value)
        {
            Position = Value;
           // _bufferedWaveProvider.ClearBuffer();
        }

        public enum CacheStatus
        {
            Normal,
            Finished,
            BufferEmpty,
            BufferResume,
            Full
        }

        bool _ExitRending = false;
        bool _isBufferEmpty = false;

        public bool IsBufferEmpty
        {
            get { return _isBufferEmpty; }
        }
        public void Play()
        {
            if (_PlayingStatus == NAudio.Wave.PlaybackState.Paused)
            {
                _PlayingStatus = NAudio.Wave.PlaybackState.Playing;
                waveOut.Play();
            }
            else
            {
                _ExitRending = false;
                _PlayingStatus = NAudio.Wave.PlaybackState.Playing;
                waveOut.Play();
            }
            _isBufferEmpty = false;
        }
        public void Stop()
        {
            _isBufferEmpty = false;
            _ExitRending = true;
            _PlayingStatus = NAudio.Wave.PlaybackState.Stopped;
            //timer.Enabled = false;
            waveOut.Stop();
            Bs.Close();
            try
            {
                File.Delete(PlayingFile);
            }
            catch { ;}
            PlayingFile = "";
        }
        public void Pause()
        {
            if (_PlayingStatus == NAudio.Wave.PlaybackState.Playing)
            {
                waveOut.Pause();
                _PlayingStatus = NAudio.Wave.PlaybackState.Paused;
            }

        }
        public bool isReady
        {
            get
            {
                try
                {
                    return !pserver.isListening;
                }
                catch { return true; }
            }
        }

        private long InitFile(string TrackFileName)
        {
            Fs = new FileStream(TrackFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            Bs = new FileStream(TrackFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            PlayingFile = TrackFileName;
            byte[] head = IOHelper.GenerateHead();
            Fs.Write(head, 0, head.Length);
            Fs.Seek(head.Length, SeekOrigin.Begin);
            return head.Length;
        }
        public void StartStaticWave(string wavfile)
        {
            Bs = new FileStream(wavfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        Dictionary<int, string> ResamplerCacheDic = new Dictionary<int, string>();
        int PanStep = 2;
        private void DoResampler(System.IO.DirectoryInfo cacheTempDir, List<VocalUtau.Calculators.NoteListCalculator.NotePreRender> NList)
        {
            int i = 0;
            while (i < NList.Count)
            {
                if (_ExitRending) break;
                int ThreadCount = 0;
                Task[] TaskArray = new Task[PanStep];
                while (ThreadCount < PanStep && i < NList.Count)
                {
                    if (_ExitRending) break;
                    string MidFileName = "R.wav";
                    ProcessStartInfo psi = null;
                    if (NList[i].ResamplerArg != null)
                    {
                        string resStr = String.Join(" ", NList[i].ResamplerArgList);
                        string MD5Str = MD5Helper.GetMD5HashString(NList[i].Resampler + resStr);
                        MidFileName = cacheTempDir.FullName + "\\" + NList[i].Note + "_" + MD5Str + ".wav";
                        if (!System.IO.File.Exists(MidFileName))
                        {
                            resStr = resStr.Replace("{RESAMPLEROUTPUT}", MidFileName);
                            psi = new ProcessStartInfo();
                            psi.FileName = NList[i].Resampler;
                            psi.Arguments = resStr;
                            psi.CreateNoWindow = true;
                            psi.WindowStyle = ProcessWindowStyle.Hidden;
                            TaskArray[ThreadCount] = Task.Factory.StartNew((object idx) =>
                            {
                                object[] ox = (object[])idx;
                                ProcessStartInfo psi2 = (ProcessStartInfo)ox[0];
                                int i2 = (int)ox[1];
                                if (psi2 != null)
                                {
                                    Process p = new Process();
                                    p.StartInfo = psi2;
                                    p.Start();
                                    Console.WriteLine("Resampling[" + i2.ToString() + "/" + (NList.Count - 1).ToString() + "]:" + NList[i2].Note + "  -  " + NList[i2].OtoAtom.PhonemeSymbol);
                                    p.WaitForExit();
                                }
                                lock (ResamplerCacheDic)
                                {
                                    ResamplerCacheDic.Add(i2, MidFileName);
                                }
                            }, new object[] { psi, i });
                            ThreadCount++;
                        }
                        else
                        {
                            //Have
                            lock (ResamplerCacheDic)
                            {
                                ResamplerCacheDic.Add(i, MidFileName);
                            }
                        }
                    }
                    else
                    {
                        //R
                        lock (ResamplerCacheDic)
                        {
                            ResamplerCacheDic.Add(i, MidFileName);
                        }
                    }
                    i++;
                }
                try
                {
                    Task.WaitAll(TaskArray);
                }
                catch { ;}
            }
        }

        public void StartRending(System.IO.DirectoryInfo baseTempDir,List<VocalUtau.Calculators.NoteListCalculator.NotePreRender> NList,string RendToWav="")
        {
            _ExitRending = false;
            prebufftime = defprebufftime;
            FormatHelper fh = new FormatHelper(IOHelper.NormalPcmMono16_Format);
            long TailLength = fh.Ms2Bytes(prebufftime);
            wsp.UnreadableTail = TailLength;
            string ProcessIDStr = Process.GetCurrentProcess().Id.ToString();
            DirectoryInfo tempDir = baseTempDir.CreateSubdirectory("temp");
            DirectoryInfo cacheDir = baseTempDir.CreateSubdirectory("cache");

            string TrackFileName = tempDir.FullName + "\\Track_" + CacheSignal + ".wav";

            headSize = InitFile(TrackFileName);
            wsp.BasicStream = Bs;
            pserver = new Pipe_Server("VocalUtau.WavTool." + ProcessIDStr + ".Track_" + CacheSignal, Fs, (int)headSize);
            pserver.NoShowText = true;
            pserver.StartServer();

            ResamplerCacheDic.Clear();
            Task t = Task.Factory.StartNew(() =>
            {
                DoResampler(cacheDir, NList);
            });
            for (int i = 0; i < NList.Count; i++)
            {
                while (!ResamplerCacheDic.ContainsKey(i))
                {
                    if (_ExitRending) break;
                    System.Threading.Thread.Sleep(100);
                }
                if (_ExitRending) break;
                string MidFileName = ResamplerCacheDic[i];
                string wavStr = String.Join(" ", NList[i].WavtoolArgList);
                Pipe_Client pclient = new Pipe_Client("VocalUtau.WavTool." + ProcessIDStr + ".Track_" + CacheSignal, 2000);
                pclient.NoShowText = true;
                pclient.LockWavFile();
                double delay = 0;
                if (NList[i].passTime > 0) delay = NList[i].passTime;
                VocalUtau.WavTools.Model.Args.ArgsStruct parg = VocalUtau.WavTools.Model.Args.ArgsParser.parseArgs(NList[i].WavtoolArgList, false);
                Console.WriteLine("WaveAppending[" + i.ToString() + "/"+(NList.Count-1).ToString()+"]:" + NList[i].Note +(NList[i].Note=="{R}"?"":"  -  " + NList[i].OtoAtom.PhonemeSymbol));
                pclient.Append(MidFileName, parg.Offset, parg.Length, parg.Ovr, parg.PV, delay);
                pclient.Flush();
                pclient.UnLockWavFile();
                pclient.Dispose();
            }
            prebufftime = 0;
            wsp.UnreadableTail = 0;
            pserver.ExitServer();
            long total = Fs.Length;
            byte[] head = IOHelper.GenerateHead((int)(total - headSize));
            Fs.Seek(0, SeekOrigin.Begin);
            Fs.Write(head, 0, head.Length);
            Fs.Flush();
            Fs.Close();
            t.Wait();
            _ExitRending = false;
            if (RendToWav != "")
            {
                if (_PlayingStatus == NAudio.Wave.PlaybackState.Playing || _PlayingStatus==NAudio.Wave.PlaybackState.Paused)
                {
                    Stop();
                }
                else
                {
                    Bs.Close();
                }
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
