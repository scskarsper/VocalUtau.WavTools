
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VocalUtau.WavTools;
using VocalUtau.WavTools.Model.Pipe;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.Wavtools.Render
{
    public class CachePlayer
    {
        long headSize = 0;
        const double defprebufftime = 1000;
        double prebufftime = 1000;
        double delaybufftime = 0;//3000;
        Pipe_Server pserver;
        AppWrapper bplayer;
        FileStream Fs;
        FileStream Bs;

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
        }
        public void Play()
        {
        }

        private long InitFile(string TrackFileName)
        {
            Fs = new FileStream(TrackFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            byte[] head = IOHelper.GenerateHead(0);
            Fs.Write(head, 0, head.Length);
            Fs.Seek(head.Length, SeekOrigin.Begin);
            return head.Length;
        }
        public void StartRending(List<VocalUtau.Calculators.NoteListCalculator.NotePreRender> NList)
        {
            prebufftime = defprebufftime;
            string temp = System.Environment.GetEnvironmentVariable("TEMP");
            DirectoryInfo info = new DirectoryInfo(temp);
            DirectoryInfo child = info.CreateSubdirectory("hymn1");
            string TrackFileName = child.FullName + "\\Track_" + CacheSignal + ".wav";

            headSize = InitFile(TrackFileName);
            pserver = new Pipe_Server("VocalUtau.WavTool.Track_" + CacheSignal, Fs, (int)headSize);
            pserver.StartServer();

            for (int i = 0; i < NList.Count; i++)
            {
                string MidFileName = "R.wav";
                ProcessStartInfo psi = null;
                if (NList[i].ResamplerArg != null)
                {
                    string resStr = String.Join(" ", NList[i].ResamplerArgList);
                    string MD5Str = MD5Helper.GetMD5HashString(NList[i].Resampler+resStr);
                    MidFileName = child.FullName + "\\" + NList[i].Note+"_"+MD5Str + ".wav";
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
                    Console.WriteLine("Resampling["+i.ToString()+"]:"+NList[i].Note+"  -  "+NList[i].OtoAtom.PhonemeSymbol);
                    p.WaitForExit();
                }

                string wavStr = String.Join(" ", NList[i].WavtoolArgList);
                //wavStr = wavStr.Replace("{RESAMPLEROUTPUT}", MidFileName);
                //wavStr = wavStr.Replace("{WAVOUTPUT}", TrackFileName);
                //
                Pipe_Client pclient = new Pipe_Client("VocalUtau.WavTool.Track_" + CacheSignal, 2000);
                pclient.LockWavFile();
                double delay = 0;
                if (NList[i].passTime > 0) delay = NList[i].passTime;

                VocalUtau.WavTools.Model.Args.ArgsStruct parg = VocalUtau.WavTools.Model.Args.ArgsParser.parseArgs(NList[i].WavtoolArgList,false);
                pclient.Append(MidFileName, parg.Offset, parg.Length, parg.Ovr, parg.PV, delay);
                pclient.Flush();
                pclient.UnLockWavFile();
                pclient.Dispose();
                //
            }
            prebufftime = 0;
            pserver.ExitServer(); 
            long total = Fs.Length;
            byte[] head = IOHelper.GenerateHead((int)(total - headSize));
            Fs.Seek(0, SeekOrigin.Begin);
            Fs.Write(head, 0, head.Length);
            Fs.Flush();
            Fs.Close();
        }
    }
}
