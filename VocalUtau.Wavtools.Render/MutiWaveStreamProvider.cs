using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.Wavtools.Render
{
    public class MutiWave16StreamProvider
    {
        //MixingSampleProvider
       // MixingSampleProvider msp=new MixingSampleProvider(
        //List<WaveStreamProvider> ls = new List<WaveStreamProvider>();
        //WaveMixerStream32 wms = new WaveMixerStream32(ls,false);
        
        private readonly WaveFormat waveFormat=IOHelper.NormalPcmMono16_Format;

        static void short2Byte(short a, byte[] b, int offset)
        {
            b[offset] = (byte)(a >> 8);
            b[offset + 1] = (byte)(a);
        }  

        static short byte2Short(byte[] b, int offset)
        {
            return (short)(((b[offset] & 0xff) << 8) | (b[offset + 1] & 0xff));
        }  

        short Remix(List<KeyValuePair<short,float>> Buffers)
        {
            int value = 0;
            int Count = Buffers.Count;
            for (int i = 0; i < Count; i++)
            {
                value = value + (int)((float)Buffers[i].Key * Buffers[i].Value);
            }
            return (short)(value / Count);
        }

        byte[] Remix(List<KeyValuePair<byte[], float>> Buffers)
        {
            List<byte> Result = new List<byte>();




            return Result.ToArray();
        }

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }
    }
}
