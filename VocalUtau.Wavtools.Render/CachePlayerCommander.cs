using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace VocalUtau.Wavtools.Render
{
    class CachePlayerCommander
    {
        /* public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler BufferEmpty_Pause;
        * public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler BufferEmpty_Resume;*/
        public event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler PlayFinished;

        Dictionary<int, CachePlayer> clist;
        public CachePlayerCommander(Dictionary<int, CachePlayer> Clist)
        {
            clist = Clist;

            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                kv.Value.SyncPosition+=Value_SyncPosition;
            }
        }

        void Value_SyncPosition(System.IO.Stream Stream)
        {
            long P = long.MaxValue;

            bool NoFull = false;
            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                if (!kv.Value.IsFull)
                {
                    P = Math.Min(P, kv.Value.Position);
                    NoFull = true;
                }
            }

            Stream.Position=P;

            if (!NoFull)
            {
                StopAll();
                if (PlayFinished != null)
                {
                    PlayFinished(clist);
                }
            }
        }
        
        public void PlayAll()
        {
            foreach (KeyValuePair<int, CachePlayer> kv in clist)
            {
                kv.Value.Play();
            }
        }
        public void StopAll()
        {
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
