
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using VocalUtau.WavTools;
using VocalUtau.WavTools.Model.Pipe;
using VocalUtau.WavTools.Model.Player;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.Wavtools.Render
{
    public class CachePlayer
    {
        Timer timer = new Timer(100);
        long headSize = 0;
        const double defprebufftime = 1000;
        double prebufftime = 1000;
        Pipe_Server pserver;
        FileStream Fs;
        FileStream Bs;

        WaveOut waveOut;
        BufferedWaveProvider bufferedWaveProvider = null;

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
            timer.Elapsed += timer_Elapsed;
            timer.Enabled = false;
            _PlayingStatus = NAudio.Wave.PlaybackState.Stopped;

            waveOut = new WaveOut();
            bufferedWaveProvider = new BufferedWaveProvider(IOHelper.NormalPcmMono16_Format);
            waveOut.Init(bufferedWaveProvider);
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                FormatHelper fh = new FormatHelper(IOHelper.NormalPcmMono16_Format);
                long TailLength=fh.Ms2Bytes(prebufftime);
                if(Bs.Position==0)
                {
                    Bs.Seek(IOHelper.NormalPcmMono16_HeadLength,SeekOrigin.Begin);
                }
                while (bufferedWaveProvider.BufferedDuration.TotalMilliseconds<4000 && Bs.Position + TailLength < Bs.Length)
                {
                    byte[] buf = new byte[1024];
                    int len=Bs.Read(buf, 0, buf.Length);
                    bufferedWaveProvider.AddSamples(buf, 0, len);
                }
                Console.WriteLine(bufferedWaveProvider.BufferedDuration.ToString());
                if (bufferedWaveProvider.BufferedDuration.TotalMilliseconds == 0 && TailLength == 0 && Bs.Position == Bs.Length)
                {
                    Stop();
                }
            }
            catch { ;}
        }
        bool _ExitRending = false;
        public void Play()
        {
            _ExitRending = false;
            _PlayingStatus = NAudio.Wave.PlaybackState.Playing;
            waveOut.Play();
            timer.Enabled = true;
        }
        public void Stop()
        {
            _ExitRending = true;
            _PlayingStatus = NAudio.Wave.PlaybackState.Stopped;
            timer.Enabled = false;
            waveOut.Stop();
            Bs.Close();
        }

        private long InitFile(string TrackFileName)
        {
            Fs = new FileStream(TrackFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            Bs = new FileStream(TrackFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] head = IOHelper.GenerateHead();
            Fs.Write(head, 0, head.Length);
            Fs.Seek(head.Length, SeekOrigin.Begin);
            return head.Length;
        }
        public void StartStaticWave(string wavfile)
        {
            Bs = new FileStream(wavfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        public void StartRending(List<VocalUtau.Calculators.NoteListCalculator.NotePreRender> NList)
        {
            prebufftime = defprebufftime;
            string ProcessIDStr = Process.GetCurrentProcess().Id.ToString();
            string temp = System.Environment.GetEnvironmentVariable("TEMP");
            DirectoryInfo info = new DirectoryInfo(temp);
            DirectoryInfo baseDir = info.CreateSubdirectory("hymn1");
            DirectoryInfo tempDir = baseDir.CreateSubdirectory("temp");
            DirectoryInfo cacheDir = baseDir.CreateSubdirectory("cache");

            string TrackFileName = tempDir.FullName + "\\Track_" + CacheSignal + ".wav";

            headSize = InitFile(TrackFileName);
            pserver = new Pipe_Server("VocalUtau.WavTool." + ProcessIDStr + ".Track_" + CacheSignal, Fs, (int)headSize);
            pserver.StartServer();

            for (int i = 0; i < NList.Count; i++)
            {
                if (_ExitRending) break;
                string MidFileName = "R.wav";
                ProcessStartInfo psi = null;
                if (NList[i].ResamplerArg != null)
                {
                    string resStr = String.Join(" ", NList[i].ResamplerArgList);
                    string MD5Str = MD5Helper.GetMD5HashString(NList[i].Resampler + resStr);
                    MidFileName = cacheDir.FullName + "\\" + NList[i].Note + "_" + MD5Str + ".wav";
                    if (!System.IO.File.Exists(MidFileName))
                    {
                        resStr = resStr.Replace("{RESAMPLEROUTPUT}", MidFileName);
                        psi = new ProcessStartInfo();
                        psi.FileName = NList[i].Resampler;
                        psi.Arguments = resStr;
                        psi.UseShellExecute = false;
                        psi.RedirectStandardInput = true;
                        psi.RedirectStandardOutput = true;
                        psi.RedirectStandardError = true;
                        psi.CreateNoWindow = true;
                        psi.WindowStyle = ProcessWindowStyle.Hidden;
                    }
                }
                if (psi != null)
                {
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();
                    Console.WriteLine("Resampling[" + i.ToString() + "]:" + NList[i].Note + "  -  " + NList[i].OtoAtom.PhonemeSymbol);
                    while (!p.HasExited)
                    {
                        if (_ExitRending) break;
                        p.WaitForExit(100);
                    }
                }

                string wavStr = String.Join(" ", NList[i].WavtoolArgList);
                Pipe_Client pclient = new Pipe_Client("VocalUtau.WavTool." + ProcessIDStr + ".Track_" + CacheSignal, 2000);
                pclient.LockWavFile();
                double delay = 0;
                if (NList[i].passTime > 0) delay = NList[i].passTime;
                VocalUtau.WavTools.Model.Args.ArgsStruct parg = VocalUtau.WavTools.Model.Args.ArgsParser.parseArgs(NList[i].WavtoolArgList, false);
                pclient.Append(MidFileName, parg.Offset, parg.Length, parg.Ovr, parg.PV, delay);
                pclient.Flush();
                pclient.UnLockWavFile();
                pclient.Dispose();
            }
            prebufftime = 0;
            pserver.ExitServer();
            long total = Fs.Length;
            byte[] head = IOHelper.GenerateHead((int)(total - headSize));
            Fs.Seek(0, SeekOrigin.Begin);
            Fs.Write(head, 0, head.Length);
            Fs.Flush();
            Fs.Close();
            _ExitRending = false;
        }
    }
}
