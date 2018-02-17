using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools
{
    class WavFile_Heads
    {
        const int wfh_samplerate=44100;
        const int wfh_channels = 1;
        const int wfh_bits = 16;

        const int wfh_length = 44;

        public static void wfh_init(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
            wfh_init(fs);
            fs.Close();
        }
        public static void wfh_init(Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            //bw.Seek(0, SeekOrigin.Begin);
            bw.Write(new char[4] { 'R', 'I', 'F', 'F' });
            //Num4
//            bw.Write((uint)0);
            bw.Write((uint)(uint.MaxValue));//初始化超长流媒体特性
            bw.Write(new char[4] { 'W', 'A', 'V', 'E' });
            bw.Write(new char[4] { 'f', 'm', 't', ' ' });
            bw.Write((uint)16);
            bw.Write((ushort)1);
            bw.Write((ushort)wfh_channels);
            bw.Write((uint)wfh_samplerate);
            bw.Write((uint)(wfh_samplerate * (wfh_bits / 8 * wfh_channels)));
            bw.Write((ushort)(wfh_bits / 8 * wfh_channels));
            bw.Write((ushort)wfh_bits);
            bw.Write(new char[4] { 'd', 'a', 't', 'a' });
            //Num44
            //bw.Write((uint)0);
            bw.Write((uint)uint.MaxValue-36);
            bw.Close();
        }

        public static bool wfh_checkIslegal(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open);
            bool ret = wfh_checkIslegal(fs);
            fs.Close();
            return ret;
        }
        public static bool wfh_checkIslegal(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            string hd = new string(br.ReadChars(4));
            if (hd != "RIFF") { br.Close(); return false; }
            stream.Seek(4, SeekOrigin.Current);

            hd = new string(br.ReadChars(4));
            if (hd != "WAVE") { br.Close(); return false; }

            hd = new string(br.ReadChars(4));
            if (hd != "fmt ") { br.Close(); return false; }

            int hds = (int)br.ReadUInt32();
            stream.Seek(hds, SeekOrigin.Current);

            hd = new string(br.ReadChars(4));
            if (hd != "data") { br.Close(); return false; }

            return true;
        }

        public static int wfh_getlength(string filename)
        {
            if (!wfh_checkIslegal(filename)) return 0;
            FileStream fs = new FileStream(filename, FileMode.Open);
            int ret = wfh_getlength(fs);
            fs.Close();
            return ret;
        }
        public static int wfh_getlength(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            stream.Seek(40, SeekOrigin.Begin);
            int len = (int)br.ReadUInt32();
            br.Close();
            return len;
        }

        public static int wfh_putlength(string filename, int length)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite);
            int ret=wfh_putlength(fs,length);
            fs.Close();
            return ret;
        }
        public static int wfh_putlength(Stream stream, int length)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            stream.Seek(4, SeekOrigin.Begin);
            //4+4+4+ 4+4+2+2+4+4+2+2+4+4+ length -8
            bw.Write((uint)(length + 36));
            stream.Seek(40, SeekOrigin.Begin);
            bw.Write((uint)length);
            bw.Close();
            return 0;
        }
    }
}
