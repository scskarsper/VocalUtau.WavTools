using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VocalUtau.WavTools.Model.Args;
using WavtoolSharp.NAudio.Extra;

namespace WavtoolSharp
{
    class Program
    {
        static void KV2PVArray(WaveFormat fileFormat, int totalFrames, List<KeyValuePair<double, double>> KVInput, out int[] p_f, out double[] v_f)
        {
            FormatHelper fhelper = new FormatHelper(fileFormat);
            List<double> p = new List<double>();
            List<double> v = new List<double>();
            foreach (KeyValuePair<double, double> kv in KVInput) { p.Add(kv.Key); v.Add(kv.Value); }
            for (int i = KVInput.Count; i < 5; i++) { p.Add(0); v.Add(0); }
            
            p_f = new int[7]{ 0, 0, 0, 0, 0, 0, 0 };
            v_f = new double[7]{ 0, 0, 0, 0, 0, 0, 0 };
            p_f[0] = 0;
            for (int i = 0; i < 2; i++)
            {
                p_f[i + 1] = fhelper.Ms2Samples(p[i]) + p_f[i];
            }
            p_f[3] = fhelper.Ms2Samples(p[4]) + p_f[2];
            p_f[6] = totalFrames;
            p_f[5] = p_f[6] - fhelper.Ms2Samples(p[3]);
            p_f[4] = p_f[5] - fhelper.Ms2Samples(p[2]);

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
        }
        static double append_linear_volume(int frame, int[] p, double[] v)
        {
            double ret = 0.0;
            int i;
            for (i = 0; i < 6; i++)
            {
                if (p[i] <= frame && frame < p[i + 1])
                {
                    ret = v[i] + (v[i + 1] - v[i]) * (((double)(frame - p[i])) / (((double)(p[i + 1] - p[i]))));
                    break;
                }
            }
            return ret;
        }
        static void AppendWork(Stream OutputStream, Stream InputStream, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV)
        {
            int[] p_f; double[] v_f;

            //Init
            WaveFormat outputFormat=new WaveFormat(44100, 1);
            FormatHelper fhelper = new FormatHelper(outputFormat);

            //IOInit
            WaveFileReader ifh=null;
            try{ ifh = new WaveFileReader(InputStream); }catch { ;}
            IOHelper ofh = new IOHelper(OutputStream, outputFormat, 46);

            //Prepare
            OutputStream.Seek(0, SeekOrigin.End);
            int outputFrames = fhelper.Ms2Samples(length);
            int overlapFrames = fhelper.Ms2Samples(ovr);
            int offsetFrames = fhelper.Ms2Samples(offset);
            int totalFrames = ifh != null ? Math.Max(outputFrames, ((int)ifh.SampleCount)) : outputFrames;
            KV2PVArray(outputFormat, totalFrames, KV, out p_f, out v_f);
            
            //SeekInput
            if (ifh!=null)
                if (offsetFrames > 0)
                {
                    ifh.Seek(fhelper.Samples2Bytes((uint)offsetFrames), SeekOrigin.Begin);
                }
                else
                {
                    ifh.Seek(0, SeekOrigin.Begin);
                }


            //SetWritePoint
            if (overlapFrames > 0)
            {
                long seekMap = fhelper.Samples2Bytes((uint)overlapFrames);
                if (OutputStream.Length > seekMap)
                {
                    OutputStream.Seek((-1) * seekMap, SeekOrigin.End);
                }
            }
            else if (overlapFrames < 0)
            {
                OutputStream.Seek(0, SeekOrigin.End);
                for (int i = 0; i < -overlapFrames; i++)
                {
                    ofh.WriteSample(0.0f);
                }
                overlapFrames = 0;
            }

            for (int currentFrame = 0; currentFrame < totalFrames; currentFrame++)
            {
                float[] SampleFrame = new float[1] { 0.0f };

                /*
                SampleFrame = ifh.ReadNextSampleFrame();
                ofh.WriteSample(SampleFrame[0]);
                continue;*/

                if (ifh != null && ifh.Position < ifh.Length)
                {
                    SampleFrame = ifh.ReadNextSampleFrame();
                    if (SampleFrame == null)
                    {
                        SampleFrame = new float[1] { 0.0f };
                    }
                }
                float SampleMono = SampleFrame.Length == 1 ? SampleFrame[0] : fhelper.frameAverage(SampleFrame);
                if (SampleMono != 0)
                {
                    //修复：切断音
                    double percent = 100.0;// append_linear_volume(currentFrame, p_f, v_f);
                    SampleMono = (float)(SampleMono * (percent / 100.0));
                }


                //OverFloat
                if (false && overlapFrames > 0)
                {
                }
                else
                {
                    ofh.WriteSample(SampleMono);
                }
            }


            /*
            IOHelper vfw = new IOHelper(OutputStream, new WaveFormat(44100, 1),46);
            float[] frame = wfr.ReadNextSampleFrame();
            while (frame != null)
            {
                vfw.WriteSample(0);
                frame = wfr.ReadNextSampleFrame();
            }
            long ara = vfw.Position;
            wfr.Position = 0;
            frame = wfr.ReadNextSampleFrame();
            while (frame != null)
            {
                vfw.WriteSample(frame[0]);
                frame = wfr.ReadNextSampleFrame();
            }

            byte[] aam = new byte[ara];
            vfw.Position = ara;
            vfw.Read(aam, 0, aam.Length);

            vfw.Position = 0;
            vfw.Write(aam, 0, aam.Length);

            wfr.Close();
            vfw.Close();

            */
            byte[] head = IOHelper.GenerateHead(outputFormat);
            OutputStream.Position = 0;
            OutputStream.Write(head, 0, head.Length);
        }
        static void Main(string[] args)
        {
            ArgsStruct p = ArgsParser.parseArgs(args);
            if (p == null)
            {
                ArgsParser.printUsage();
                return;
            }
            ArgsParser.printArgs(p);
            Console.WriteLine("---- Work Renew ----");

            using (System.IO.FileStream ifs = new FileStream(p.Inputfilename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (System.IO.FileStream ofs = new FileStream(p.Outputfilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    AppendWork(ofs, ifs, p.Offset, p.Length, p.Ovr, p.PV);
                }
            }
            
        }
    }
}
