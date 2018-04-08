using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace VocalUtau.Wavtools.Render
{
    class CachePlayerCommander
    {
        public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler BufferEmpty_Pause;
        public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler BufferEmpty_Resume;
        public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler PlayFinished;

        Timer timer = new Timer(100);

        Dictionary<int, CachePlayer> clist;
        public CachePlayerCommander(Dictionary<int, CachePlayer> Clist)
        {
            clist = Clist;

            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                kv.Value.SyncPosition+=Value_SyncPosition;
            }
           // timer.Elapsed += timer_Elapsed;
        }

        void Value_SyncPosition(System.IO.Stream Stream)
        {
            long P = long.MaxValue;

            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                if(!kv.Value.IsFull) P = Math.Min(P, kv.Value.Position);
            }

            Stream.Position=P;
        }
        /*
         
         * 
        public void Timer_Elapse(long Position)
        {
            try
            {
                Bs.Position = Position;
                FormatHelper fh = new FormatHelper(IOHelper.NormalPcmMono16_Format);
                long TailLength = fh.Ms2Bytes(prebufftime);
                if (Bs.Position == 0)
                {
                    Bs.Seek(IOHelper.NormalPcmMono16_HeadLength, SeekOrigin.Begin);
                }
                while (bufferedWaveProvider.BufferedDuration.TotalMilliseconds < 4000 && Bs.Position + TailLength < Bs.Length)
                {
                    byte[] buf = new byte[1024];
                    int len = Bs.Read(buf, 0, buf.Length);
                    bufferedWaveProvider.AddSamples(buf, 0, len);
                }
            }
            catch { ;}
        }
         
         */
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            /*bool ShouldFill=false;
            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                VocalUtau.Wavtools.Render.CachePlayer.CacheStatus CS = kv.Value.GetStatus();
                if (CS != CachePlayer.CacheStatus.Full && CS != CachePlayer.CacheStatus.BufferResume)
                {
                    ShouldFill = true;
                    break;
                }
            }
            if (ShouldFill)
            {
                long MinSync = MinSyncPosition;
                if (MinSync >= 0)
                {
                    foreach (KeyValuePair<int, CachePlayer> kv in clist)
                    {
                      //  kv.Value.Timer_Elapse(MinSync);
                    }
                }
            }
            int FinishCount = 0;
            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                VocalUtau.Wavtools.Render.CachePlayer.CacheStatus CS = kv.Value.GetStatus();
                switch (CS)
                {
                    case CachePlayer.CacheStatus.BufferEmpty:
                        if (BufferEmpty_Pause != null) BufferEmpty_Pause(clist);
                       // PauseAll();
                        return;
                    case CachePlayer.CacheStatus.BufferResume:
                        if (BufferEmpty_Resume != null) BufferEmpty_Resume(clist);
                       // PlayAll();
                        return;
                    case CachePlayer.CacheStatus.Finished:
                        FinishCount++;
                        break;
                }
            }
            if (FinishCount == clist.Count)
            {
                if (PlayFinished != null) PlayFinished(clist);
                StopAll();
            }*/
           
        }


        public void SyncAll()
        {
        /*    bool Synced = true;
            long MinValue = long.MaxValue;
            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                MinValue = Math.Min(MinValue,kv.Value.Position);
                Synced = Synced && (MinValue == kv.Value.Position);
            }
            if (!Synced)
            {
                foreach (KeyValuePair<int, CachePlayer> kv in clist)
                {
                    kv.Value.ResetPosition(MinValue);
                }
                Console.WriteLine("Synced");
            }*/
        }
        public void PlayAll()
        {
            timer.Enabled = true;
            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                kv.Value.Play();
            }
        }
        public void StopAll()
        {
            timer.Enabled = false;
            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                kv.Value.Stop();
            }
        }
        public void PauseAll()
        {
            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                kv.Value.Pause();
            }
        }
    }
}
