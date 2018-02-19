using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WavtoolSharp.NAudio.Extra
{
    class FormatHelper
    {
        WaveFormat wavformat;
        public FormatHelper(WaveFormat waveformat)
        {
            wavformat = waveformat;
        }
        public int Ms2Samples(double time)
        {
            double samples_f;
            int samples;
            samples_f = ((double)wavformat.SampleRate) * time / 1000.0;
            samples = (int)samples_f;
            return samples;
        }
        public long Samples2Bytes(uint Samples)
        {
            if (Samples == 0) return 0;
            long ret = 0;
            ret = wavformat.Channels * (wavformat.BitsPerSample / 8) * Samples;
            return ret;
        }
        public long Ms2Bytes(double time)
        {
            long ret = 0;
            int frames = Ms2Samples(time);
            if (frames > 0)
            {
                ret=Samples2Bytes((uint)frames);
            }
            return ret;
        }
        public float frameAverage(float[] frames)
        {
            if (frames.Length == 0) return 0;
            double total = 0;
            foreach (float f in frames)
            {
                total += f;
            }
            return (float)(total/frames.Length);
        }
    }
}
