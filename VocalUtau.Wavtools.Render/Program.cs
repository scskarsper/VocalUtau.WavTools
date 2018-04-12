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

                Dictionary<int, IRender> CplayerList = new Dictionary<int, IRender>();// = new CachePlayer();
                using (System.IO.FileStream ms = new System.IO.FileStream(baseDir.FullName + @"\\RendCmd.binary", System.IO.FileMode.Open))
                {
                    //序列化操作，把内存中的东西写到硬盘中
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter fomatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter(null, new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.File));
                    object obj = fomatter.Deserialize(ms);
                    ms.Flush();
                    BFS = (BinaryFileStruct)obj;
                }
                for (int i = 0; i < BFS.VocalTrackStructs.Count; i++)
                {
                    CplayerList.Add(i, new CacheRender());
                    CplayerList[i].RendingStateChange += (object sender) => {
                        IRender cr = (IRender)sender;
                        int Key=-1;
                        foreach(KeyValuePair<int,IRender> kv in CplayerList)
                        {
                            if (kv.Value == cr)
                            {
                                Key = kv.Key;
                                break;
                            }
                        }
                        if(cmder!=null)cmder.SetupRendingStatus(Key, cr.getIsRending());
                    };
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


                ProcessStr = "0/0";
                cmder = new PlayCommander(CplayerList);
                cmder.SetTrackerVolumes(BFS.VocalTrackVolumes);
                cmder.WaitRendingStart();
                cmder.PlayFinished += Program_PlayFinished;
                cmder.PlayProcessUpdate += cmder_PlayProcessUpdate;
                cmder.PlayPlaying += cmder_PlayPlaying;
                cmder.PlayPaused += cmder_PlayPaused;
                cmder.PlayAll();
                Console.ReadLine();
            }
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
            client.SendData_ASync(ProcessStr);
        }

        static Timer tme = new Timer(1000);
        static string ProcessStr = "0/0";
        static void cmder_PlayProcessUpdate(object sender)
        {
            try
            {
                object[] or = (object[])sender;
                string s1 = ((TimeSpan)or[0]).ToString();
                string s2 = ((TimeSpan)or[1]).ToString();
                ProcessStr = s1 + "/" + s2;
            }
            catch { ;}
        }
        static void cmdReciever_OnRecieve(string data)
        {
            try
            {
                switch (data)
                {
                    case "Cmd:Play": cmder.PlayAll(); break;
                    case "Cmd:Pause": cmder.PauseAll(); break;
                    case "Cmd:Stop": cmder.StopAll(); break;
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