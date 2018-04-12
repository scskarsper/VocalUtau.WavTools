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
        public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler PlayPaused;
        public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler PlayPlaying;
        public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler PlayProcessUpdate;

        MutiWave16StreamProvider mwsp = new MutiWave16StreamProvider();

        //WaveOut SoundOutputer = new WaveOut();
//        DirectSoundOut SoundOutputer = new DirectSoundOut();
        IWavePlayer SoundOutputer = new WasapiOut();
        

        Dictionary<int, IRender> clist;
        Dictionary<int, float> cvolume;
        public PlayCommander(Dictionary<int, IRender> TrackerCacherList)
        {
            clist = TrackerCacherList;
            SoundOutputer.Init(mwsp);
            mwsp.SoundProcessed += mwsp_SoundProcessed;
        }

        public void SetTrackerVolumes(Dictionary<int, float> Volumes)
        {
            cvolume = Volumes;
        }

        void mwsp_SoundProcessed(object sender)
        {
            Console.WriteLine("{0}/{1}", mwsp.PlayPosition, mwsp.CurrentDuration);
            if (mwsp.IsAllFinished)
            {
                if (PlayFinished != null) PlayFinished(this);
                SoundOutputer.Stop();
            }
            else
            {
                if (PlayProcessUpdate != null) PlayProcessUpdate(new object[] { mwsp.PlayPosition, mwsp.CurrentDuration });
            }
        }

        public void WaitRendingStart()
        {
            List<Task> tasklist = new List<Task>();
            foreach (KeyValuePair<int, IRender> CRK in clist)
            {
                Task t=Task.Factory.StartNew((object prm) =>
                {
                    IRender CRV = (IRender)prm;
                    while (CRV.getRendingFile() == "" || !System.IO.File.Exists(CRV.getRendingFile()))
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

            foreach (KeyValuePair<int, IRender> CRK in clist)
            {
                WaveStreamType wst = new WaveStreamType();
                wst.UnreadableTail = TailLength;
                if (cvolume.ContainsKey(CRK.Key))
                {
                    wst.Volume = cvolume[CRK.Key];
                }
                wst.WaveStream = new FileStream(CRK.Value.getRendingFile(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                mwsp.InputMap.Add(CRK.Key, wst);
            }
        }
        public void PlayAll()
        {
            SoundOutputer.Play();
            if (PlayPlaying != null) PlayPlaying(this);
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
            if (PlayFinished != null) PlayFinished(this);
        }
        public void PauseAll()
        {
            SoundOutputer.Pause();
            if (PlayPaused != null) PlayPaused(this);
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
