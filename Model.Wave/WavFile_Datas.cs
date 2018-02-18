using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools.Model.Wave
{
    public class WavFile_Datas
    {
        const int wfh_samplerate = 44100;
        const int wfh_channels = 1;
        const int wfh_bits = 16;

        private static int wfh_length = 44;//BrHead=0;

        public static void wfd_init(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
            fs.Close();
            wfh_length = 0;
        }

        public static int wfd_ms2samples(double time)
        {
            double samples_f;
            int samples;
            samples_f = ((double)wfh_samplerate) * time / 1000.0;
            samples = (int)samples_f;
            return samples;
        }
        public static void wfd_skip(Stream stream, double time)
        {
            int samples;
            samples = wfd_ms2samples(time);
            if (stream.Position < wfh_length) stream.Seek(wfh_length, SeekOrigin.Begin);
            stream.Seek(samples * wfh_channels * (wfh_bits / 8), SeekOrigin.Current);
        }

        public static double wfd_append_linear_volume(int frame, int[] p, double[] v) {
          double ret=0.0;
          int i;
          for (i=0; i<6; i++) {
            if (p[i] <= frame && frame < p[i+1]) {
              ret = v[i] + (v[i+1]-v[i]) * (((double)(frame-p[i])) / (((double)(p[i+1]-p[i]))));
              break;
            }
          }
          return ret;
        }
        public static Int16 wfd_mix(Int16 a, Int16 b)
        {
            int a_t;
            int b_t;
            int r1;
            Int16 ret = 0;

            a_t = ((int)a) + 32768;
            b_t = ((int)b) + 32768;

            if (a_t <= 32768 && b_t <= 32768)
            {
                r1 = (a_t * b_t) / 32768;
            }
            else
            {
                r1 = 2 * (a_t + b_t) - (a_t * b_t) / 32768 - 65536;
                if (r1 >= 65536)
                {
                    r1 = 65535;
                }
            }
            r1 = r1 - 32768;
            ret = ((Int16)r1);
            return ret;
        }
        public static int MsTime2BytesCount(double time)
        {
            if (time <= 0) return 0;
            int ret=0;
            int frames=wfd_ms2samples(time);
            if (frames > 0)
            {
                ret = wfh_channels * (wfh_bits / 8) * frames;
            }
            return ret;
        }
        public static int wfd_append(string outputfilename,string inputfilename,double offset, double length,
		double ovr, List<KeyValuePair<double, double>> KV)
        {
            FileStream ofs = new FileStream(outputfilename, FileMode.OpenOrCreate);
            FileStream ifs = new FileStream(inputfilename, FileMode.Open);
            int ret = wfd_append(ofs, ifs, offset, length, ovr, KV);
            ifs.Close();
            ofs.Close();
            return ret;
        }
        
        public static int wfd_append(Stream OutputStream, string inputfilename, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV)
        {
            Stream ifs=null;
            try
            {
                ifs = new FileStream(inputfilename, FileMode.Open,FileAccess.Read,FileShare.Read);
            }
            catch {
                ifs = new MemoryStream();
                byte[] rfs = WavFile_Heads.Init_EmptyFile();
                ifs.Write(rfs, 0, rfs.Length);
                ifs.Position = 0;
                ;}
            int ret = wfd_append(OutputStream, ifs, offset, length, ovr, KV);
            ifs.Close();
            return ret;
        }
        public static int wfd_append(string outputfilename, Stream InputStream, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV)
        {
            FileStream ofs = new FileStream(outputfilename, FileMode.OpenOrCreate);
            int ret = wfd_append(ofs, InputStream, offset, length, ovr, KV);
            ofs.Close();
            return ret;
        }
        public static int wfd_append(Stream OutputStream, Stream InputStream, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV)
        {
            List<double> p = new List<double>();
            List<double> v = new List<double>();
            foreach (KeyValuePair<double, double> kv in KV) { p.Add(kv.Key); v.Add(kv.Value); }
            for (int i = KV.Count; i < 5; i++) { p.Add(0); v.Add(0); }

            OutputStream.Seek(0, SeekOrigin.End);//SeekToEnd
            int outputFrames=wfd_ms2samples(length);
            int currentFrame = 0;

            Int16 sum;
            int c1, c2;

            WaveFileReader wfr = new WaveFileReader(InputStream);

            /* handle offset */
            if(wfd_ms2samples(offset)>0)
            {
                Emu_Sf_Seek(wfr, wfd_ms2samples(offset), SeekOrigin.Begin);
            }

            int[] p_f = { 0, 0, 0, 0, 0, 0, 0 };
            double[] v_f = { 0, 0, 0, 0, 0, 0, 0 };
            /* pre-calculate volume */
            p_f[0] = 0;
            for (int i = 0; i < 2; i++)
            {
                p_f[i + 1] = wfd_ms2samples(p[i]) + p_f[i];
            }
            p_f[3] = wfd_ms2samples(p[4]) + p_f[2];
            p_f[6] = outputFrames;
            p_f[5] = p_f[6] - wfd_ms2samples(p[3]);
            p_f[4] = p_f[5] - wfd_ms2samples(p[2]);

            v_f[0] = 0.0;
            for (int i = 0; i < 2; i++)
            {
                v_f[i + 1] = v[i];
            }
            v_f[3] = v[4];
            v_f[4] = v[2];
            v_f[5] = v[3];
            v_f[6] = 0.0;

            if (p_f[1] == p_f[2])
            {
                v_f[1] = v_f[2];
            }
            if (p_f[0] == p_f[1])
            {
                v_f[0] = v_f[1];
            }
            if (p_f[5] == p_f[4])
            {
                v_f[5] = v_f[4];
            }
            if (p_f[6] == p_f[5])
            {
                v_f[6] = v_f[5];
            }
            int ovrFrames = wfd_ms2samples(ovr);
            if (ovrFrames > 0)
            {
                int seekMap = wfh_channels * (wfh_bits / 8) * ovrFrames;
                if (OutputStream.Length-wfh_length > seekMap)
                {
                    OutputStream.Seek((-1) * seekMap, SeekOrigin.End);
                }
            }
            else if (ovr < 0.0)
            {
                /* output blank samples */
                int ovrSamples = 0;
                int i, j, k;
                ovrSamples = wfd_ms2samples(-ovr);
                for (i = 0; i < ovrSamples; i++)
                {
                    for (j = 0; j < wfh_channels; j++)
                    {
                        for (k = 0; k < (wfh_bits / 8); k++)
                        {
                            OutputStream.WriteByte((byte)'\0');
                        }
                    }
                }
                OutputStream.Flush();
                OutputStream.Seek(0, SeekOrigin.Current);
                ovr = 0.0;
                ovrFrames = 0;
            }

            /* output */

            Int16[] Buf=new Int16[1]{0};

            currentFrame = 0;
            for (; outputFrames > 0; outputFrames--)
            {
                if (wfr.CanRead)
                {
                    Buf = Emu_Sf_Readf_Short(wfr, 1);
                    if (Buf.Length < 1)
                    {
                        Buf = new Int16[(int)(wfr.WaveFormat.BlockAlign/2)];
                        for (int j = 0; j < (int)(wfr.WaveFormat.BlockAlign / 2); j++)
                        {
                            Buf[j] = 0;
                        }
                        wfr.Close();
                    }
                }
                /* simple mix if there are multi-channels */
                sum = Buf[0];
                /* modify the volume */
                if (wfr.WaveFormat.Channels > 0)
                {
                    for (int i = 0; i < wfr.WaveFormat.Channels; i++)
                    {
                        sum += Buf[i];
                    }
                    double vf;
                    sum = (Int16)(sum / wfr.WaveFormat.Channels);
                    vf = wfd_append_linear_volume(currentFrame, p_f, v_f);
                    sum = (Int16)(((double)sum) * (vf / 100.0));
                }
                else
                {
                    sum = 0;
                }
                if (OutputStream.Position < wfh_length)
                {
                    OutputStream.Seek(wfh_length, SeekOrigin.Begin);
                }
                if (ovrFrames > 0)
                {
                    Int16 d, r;
                    c1 = OutputStream.ReadByte();
                    if (c1==-1)
                    {
                        ovrFrames = 0;
                        goto wfd_append_normal;
                    }
                    c2 = OutputStream.ReadByte();
                    if (c2 == -1)
                    {
                        ovrFrames = 0;
                        OutputStream.Seek(-1, SeekOrigin.Current);
                        goto wfd_append_normal;
                    }
                    OutputStream.Seek(-2, SeekOrigin.Current);
                    d = (Int16)((c1 & (0x00ff)) | (((c2 & 0x00ff) << 8) & 0xff00));
                    r = wfd_mix(sum, d);
                    OutputStream.WriteByte((byte)((r) & (0x00ff)));
                    OutputStream.WriteByte((byte)((r >> 8) & 0x00ff));
                    OutputStream.Flush();
                    OutputStream.Seek(0, SeekOrigin.Current);
                    ovrFrames--;
                wfd_append_normal:
                    OutputStream.WriteByte((byte)((sum) & (0x00ff)));
                    OutputStream.WriteByte((byte)((sum >> 8) & 0x00ff));
                    OutputStream.Flush();
                    OutputStream.Seek(0, SeekOrigin.Current);
                }
                else
                {
                    OutputStream.WriteByte((byte)((sum) & (0x00ff)));
                    OutputStream.WriteByte((byte)((sum >> 8) & 0x00ff));
                    OutputStream.Flush();
                    OutputStream.Seek(0, SeekOrigin.Current);
                }
                currentFrame++;
            }

            if (!wfr.CanRead)
            {
                wfr.Close();
            }
            long ret=OutputStream.Length;
            OutputStream.Close();
            return (int)ret;
        }
        private static void Emu_Sf_Seek(WaveFileReader wfr, int SamplesNum, SeekOrigin seekOrigin)
        {
            wfr.Seek(SamplesNum * wfr.BlockAlign, seekOrigin);
        }
        private static Int16[] Emu_Sf_Readf_Short(WaveFileReader wfr, int frameNum)
        {
            Int16[] ret = new Int16[frameNum];
            int step = (int)(wfr.WaveFormat.BlockAlign / 2);
            for (int i = 0; i < frameNum; i=i+step)
            {
                byte[] Buffer = new byte[wfr.WaveFormat.BlockAlign];
                for (int j = 0; j < wfr.WaveFormat.BlockAlign; j++) Buffer[j] = 0;
                wfr.Read(Buffer, 0, wfr.WaveFormat.BlockAlign);
                for (int j = 0; j < step; j++)
                {
                    Int16 rt = (Int16)(Buffer[2*j+1] * 0x100 + Buffer[2*j+0]);
                    ret[i+j] = rt;
                }
            }
            return ret;
        }
    }
}
