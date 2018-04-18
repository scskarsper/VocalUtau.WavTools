using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VocalUtau.Calculators;

namespace VocalUtau.Wavtools.Render
{
    class Program
    {
        /// <summary>
        /// ISSUE:发现问题002：同步之后，偶见连接异常。
        /// 建议：取消PipeC/S架构通信，直接内部渲染
        /// </summary>

        static CommandPipe_Client client = null;
        static PlayCommander cmder;
        static CommandPipe_Server cmdReciever;


        static Dictionary<int, IRender> CplayerList = new Dictionary<int, IRender>();// = new CachePlayer();
        static bool _EmptyProgram = false;
        static double StartTimeMs;
        static void Main(string[] args)
        {
            tme.Elapsed += tme_Elapsed;
            tme.Enabled = false;
            Console.WriteLine("CreateNamedPipe:" + System.Diagnostics.Process.GetCurrentProcess().Id);
            int Instance = int.Parse(args[0]);
            if (Instance > 0)
            {
                cmdReciever = new CommandPipe_Server(System.Diagnostics.Process.GetCurrentProcess().Id);
                cmdReciever.OnRecieve += cmdReciever_OnRecieve;
                client = new CommandPipe_Client(Instance);

                string temp = System.Environment.GetEnvironmentVariable("TEMP");
                DirectoryInfo info = new DirectoryInfo(temp);
                DirectoryInfo baseDir = info.CreateSubdirectory("Chorista\\Instance." + Instance);

                BinaryFileStruct BFS = new BinaryFileStruct();

                CplayerList.Clear();
                using (System.IO.FileStream ms = new System.IO.FileStream(baseDir.FullName + @"\\RendCmd.binary", System.IO.FileMode.Open))
                {
                    //序列化操作，把内存中的东西写到硬盘中
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter fomatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter(null, new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.File));
                    object obj = fomatter.Deserialize(ms);
                    ms.Flush();
                    BFS = (BinaryFileStruct)obj;
                }

                StartTimeMs = (BFS.StartTimePosition * 1000);

                client.SendData_ASync("Status:Buffering");
                _EmptyProgram = false;
                for (int i = 0; i < BFS.VocalTrackStructs.Count; i++)
                {
                    CplayerList.Add(i, new CacheRender());
                    CplayerList[i].RendingStateChange += Program_RendingStateChange;
                    Task.Factory.StartNew((object prm) => { 
                        object[] prms = (object[])prm;
                        IRender cplayer = (IRender)prms[0];
                        DirectoryInfo TempDir=(DirectoryInfo)prms[1];
                        List<Calculators.NoteListCalculator.NotePreRender> NList = (List<Calculators.NoteListCalculator.NotePreRender>)prms[2];
                        cplayer.StartRending(TempDir, NList);
                    }, new object[] { CplayerList[i], baseDir, BFS.VocalTrackStructs[i] });
                }

                for (int j = 0; j < BFS.BarkerTrackStructs.Count; j++)
                {
                    int i = BFS.VocalTrackStructs.Count + j;
                    CplayerList.Add(i, new BgmRender());
                    CplayerList[i].RendingStateChange += (object sender) =>
                    {
                        IRender cr = (IRender)sender;
                        int Key = -1;
                        foreach (KeyValuePair<int, IRender> kv in CplayerList)
                        {
                            if (kv.Value == cr)
                            {
                                Key = kv.Key;
                                break;
                            }
                        }
                        if (cmder != null) cmder.SetupRendingStatus(Key, cr.getIsRending());
                    };
                    Task.Factory.StartNew((object prm) =>
                    {
                        object[] prms = (object[])prm;
                        IRender cplayer = (IRender)prms[0];
                        DirectoryInfo TempDir = (DirectoryInfo)prms[1];
                        List<VocalUtau.Calculators.BarkerCalculator.BgmPreRender> NList = (List<VocalUtau.Calculators.BarkerCalculator.BgmPreRender>)prms[2];
                        cplayer.StartRending(TempDir, NList);
                    }, new object[] { CplayerList[i], baseDir, BFS.BarkerTrackStructs[j] });
                }

                cmder = new PlayCommander(CplayerList);
                cmder.SetTrackerVolumes(BFS.TrackVolumes);
                cmder.SetGlobalVolume(BFS.GlobalVolume);
                cmder.WaitRendingStart();
                cmder.PlayFinished += Program_PlayFinished;
                cmder.PlayProcessUpdate += cmder_PlayProcessUpdate;
                cmder.PlayPlaying += cmder_PlayPlaying;
                cmder.PlayPaused += cmder_PlayPaused;
                ProcessStr = "0/0/False";
                //如果为空项目，则渲染未开始即结束
                if (_EmptyProgram)
                {
                    cmder.StopAll();
                }
                else
                {
                    cmder.PlayAll();
                }
                _EmptyProgram = false;
                Console.ReadLine();
            }
        }

        static void Program_RendingStateChange(object sender)
        {
            IRender cr = (IRender)sender;
            int Key = -1;
            foreach (KeyValuePair<int, IRender> kv in CplayerList)
            {
                if (kv.Value == cr)
                {
                    Key = kv.Key;
                    break;
                }
            }
            _EmptyProgram = !cr.getIsRending();
            if (cmder != null) cmder.SetupRendingStatus(Key, cr.getIsRending());
        }


        static void cmder_PlayPaused(object sender)
        {
            client.SendData_ASync("Status:Paused");
            tme.Enabled = false;
        }

        static void cmder_PlayPlaying(object sender)
        {
            client.SendData_ASync("Status:Playing");
            tme.Enabled = true;
        }

        static void tme_Elapsed(object sender, ElapsedEventArgs e)
        {
            client.SendData_ASync("Postion:"+ProcessStr);
        }

        static Timer tme = new Timer(100);
        static string ProcessStr = "0/0/False";
        static void cmder_PlayProcessUpdate(object sender)
        {
            try
            {
                object[] or = (object[])sender;
                double t1 = (double)or[0];
                t1+=StartTimeMs;
                double t2 = (double)or[1];
                bool b3=(bool)or[2];
                t2 += StartTimeMs;
                string s1 = (t1).ToString();
                string s2 = (t2).ToString();
                ProcessStr = s1 + "/" + s2+"/"+b3.ToString();
            }
            catch { ;}
        }
        static void cmdReciever_OnRecieve(string data)
        {
            try
            {
                string[] cm = data.Split(':');
                if (cm[0] == "Cmd")
                {
                    switch (cm[1])
                    {
                        case "Play": cmder.PlayAll(); break;
                        case "Pause": cmder.PauseAll(); break;
                        case "Stop": cmder.StopAll(); break;
                    }
                }
                else if (cm[0] == "Volume")
                {
                    switch (cm[1])
                    {
                        case "Global": float fl = float.Parse(cm[2]); cmder.SetGlobalVolume(fl); break;
                        case "Track": int tl = int.Parse(cm[2]); float ftl = float.Parse(cm[3]); cmder.SetTrackVolume(tl, ftl); break;
                    }
                }
            }
            catch { ;}
        }

        static void Program_PlayFinished(object sender)
        {
            ProcessStr = "0/0";
            client.SendData_ASync("Status:Finished");
            tme.Enabled = false;
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
/*//100
           using (FileStream fs = new FileStream(@"D:\\test-t"+tracker.getIndex().ToString()+".bat", FileMode.Create))
           {
               using (StreamWriter sw=new StreamWriter(fs))
               {
                   sw.WriteLine("mkdir \"%temp%\\utaubk\"");
                   for (int i = 0; i < nlc.NotePreRenderList.Count; i++)
                   {
                       //"{RESAMPLEROUTPUT}", "{WAVOUTPUT}");
                       if (nlc.NotePreRenderList[i].ResamplerArg != null)
                       {
                           string resStr = String.Join(" ", nlc.NotePreRenderList[i].ResamplerArgList);
                           resStr = resStr.Replace("{RESAMPLEROUTPUT}", @"temp$$$.wav");
                           sw.WriteLine(@"D:\VocalUtau\VocalUtau.DebugExampleFiles\UTAUKernel\resampler.exe " + resStr);
                       }
                       string wavStr = String.Join(" ", nlc.NotePreRenderList[i].WavtoolArgList);
                       wavStr = wavStr.Replace("{RESAMPLEROUTPUT}", @"temp$$$.wav");
                       wavStr = wavStr.Replace("{WAVOUTPUT}", @"temp.wav");
                       sw.WriteLine(@"D:\VocalUtau\VocalUtau.DebugExampleFiles\UTAUKernel\wavtool.exe " + wavStr);
                   }
               }
           }


           //101
           using (FileStream fs = new FileStream(@"D:\\test-b" + tracker.getIndex().ToString() + ".txt", FileMode.Create))
           {
               using (StreamWriter sw = new StreamWriter(fs))
               {
                   for (int i = 0; i < nlc.NotePreRenderList.Count; i++)
                   {
                       //"{RESAMPLEROUTPUT}", "{WAVOUTPUT}");
                       if (nlc.NotePreRenderList[i].ResamplerArg != null)
                       {
                           string resStr = String.Join(" ", nlc.NotePreRenderList[i].ResamplerArgList);
                           resStr = resStr.Replace("{RESAMPLEROUTPUT}", @"temp$$$.wav");
                           sw.WriteLine(@"resampler.exe " + resStr.Replace(@"D:\VocalUtau\VocalUtau\bin\Debug\voicedb\YongQi_CVVChinese_Version2\",""));
                       }
                       string wavStr = String.Join(" ", nlc.NotePreRenderList[i].WavtoolArgList);
                       wavStr = wavStr.Replace("{RESAMPLEROUTPUT}", @"temp$$$.wav");
                       wavStr = wavStr.Replace("{WAVOUTPUT}", @"temp.wav");
                       sw.WriteLine(@"wavtool.exe " + wavStr.Replace(@"D:\VocalUtau\VocalUtau\bin\Debug\voicedb\YongQi_CVVChinese_Version2\", ""));
                   }
               }
           }*/