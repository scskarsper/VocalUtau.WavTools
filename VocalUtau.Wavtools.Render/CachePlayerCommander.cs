using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocalUtau.Wavtools.Render
{
    class CachePlayerCommander
    {
        Dictionary<int, CachePlayer> clist;
        public CachePlayerCommander(Dictionary<int, CachePlayer> Clist)
        {
            clist = Clist;
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
