using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools
{
    class Debug_NPlayer
    {
        static WaveOut waveOut;
        static BufferedWaveProvider bufferedWaveProvider = null;
        public static void Init()
        {
            waveOut = new WaveOut();
            bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100,1));
            waveOut.Init(bufferedWaveProvider);
            waveOut.Play();   
        }
        public static void AddBytes(byte[] bytes)
        {
            bufferedWaveProvider.AddSamples(bytes, 0, bytes.Length);
        }
        public static bool BufferFull
        {
            get
            {
                return bufferedWaveProvider.BufferedDuration.Ticks>= bufferedWaveProvider.BufferDuration.Ticks/2;
            }
        }
        public static void Stop()
        {
            waveOut.Stop();
        }
    }
}
