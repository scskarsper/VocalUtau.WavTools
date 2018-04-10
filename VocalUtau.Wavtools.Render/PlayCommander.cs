using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.Wavtools.Render
{
    class PlayCommander
    {
        double prebufftime = 1000;

        public double PreBuffTime
        {
            get { return prebufftime; }
            set { prebufftime = value; }
        }

        public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler PlayFinished;

        MutiWave16StreamProvider mwsp = new MutiWave16StreamProvider();

        //WaveOut SoundOutputer = new WaveOut();
//        DirectSoundOut SoundOutputer = new DirectSoundOut();
        IWavePlayer SoundOutputer = new WasapiOut();
        

        Dictionary<int, CacheRender> clist;
        public PlayCommander(Dictionary<int, CacheRender> Clist)
        {
            clist = Clist;
            SoundOutputer.Init(mwsp);
            mwsp.SoundProcessed += mwsp_SoundProcessed;
        }

        void mwsp_SoundProcessed(object sender)
        {
            Console.WriteLine("{0}/{1}", mwsp.PlayPosition, mwsp.CurrentDuration);
            if (mwsp.IsAllFinished)
            {
                if (PlayFinished != null) PlayFinished(this);
                SoundOutputer.Stop();
            }
        }

        public void WaitRendingStart()
        {
            List<Task> tasklist = new List<Task>();
            foreach (KeyValuePair<int, CacheRender> CRK in clist)
            {
                Task t=Task.Factory.StartNew((object prm) =>
                {
                    CacheRender CRV = (CacheRender)prm;
                    while (CRV.RendingFile == "" || !System.IO.File.Exists(CRV.RendingFile))
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }, CRK.Value);
                tasklist.Add(t);
            }
            Task.WaitAll(tasklist.ToArray());
            
            mwsp.InputMap.Clear();
            FormatHelper fh = new FormatHelper(IOHelper.NormalPcmMono16_Format);
            long TailLength = fh.Ms2Bytes(1000);

            foreach (KeyValuePair<int, CacheRender> CRK in clist)
            {
                WaveStreamType wst = new WaveStreamType();
                wst.UnreadableTail = TailLength;
                wst.WaveStream = new FileStream(CRK.Value.RendingFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                mwsp.InputMap.Add(CRK.Key, wst);
            }
        }
        public void PlayAll()
        {
            SoundOutputer.Play();
        }
        public void StopAll()
        {
            SoundOutputer.Stop();
            if (mwsp != null && mwsp.InputMap != null)
            {
                foreach (KeyValuePair<int, WaveStreamType> CRK in mwsp.InputMap)
                {
                    try
                    {
                        mwsp.InputMap[CRK.Key].WaveStream.Position = 0;
                    }catch{;}
                }
            }
        }
        public void PauseAll()
        {
            SoundOutputer.Pause();
        }

        public void SetupRendingStatus(int Key,bool IsRending)
        {
            if (mwsp.InputMap.ContainsKey(Key))
            {
                if (IsRending)
                {
                    FormatHelper fh = new FormatHelper(IOHelper.NormalPcmMono16_Format);
                    long TailLength = fh.Ms2Bytes(prebufftime);
                    mwsp.InputMap[Key].UnreadableTail = TailLength;
                }
                else
                {
                    mwsp.InputMap[Key].UnreadableTail = 0;
                }
            }
        }
    }
}
