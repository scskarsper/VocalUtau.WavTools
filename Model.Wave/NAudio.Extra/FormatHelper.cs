using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools.Model.Wave.NAudio.Extra
{
    public class FormatHelper
    {
        WaveFormat wavformat;
        public FormatHelper()
        {
            wavformat = IOHelper.NormalPcmMono16_Format;
        }
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

        public List<MathHelper.SegmentLine> KV2SegmentLines(int totalFrames, List<KeyValuePair<double, double>> KVInput)
        {
            return KV2SegmentLines(totalFrames, totalFrames, KVInput);
        }
        public List<MathHelper.SegmentLine> KV2SegmentLines(int outputFrames, int totalFrames, List<KeyValuePair<double, double>> KVInput)
        {
            List<MathHelper.SegmentLine> ret = new List<MathHelper.SegmentLine>();
            List<KeyValuePair<double, double>> ControlPoint = new List<KeyValuePair<double, double>>();
            ControlPoint.AddRange(KVInput.ToArray());
            for (int i = ControlPoint.Count; i < 5; i++) { ControlPoint.Add(new KeyValuePair<double, double>(0, 0)); }

            SortedDictionary<double, double> ValuePoint = new SortedDictionary<double, double>();
            ValuePoint.Add(0, 0);//添加起点
            if (outputFrames == 0) outputFrames = 1;
            ValuePoint.Add(outputFrames, 0);//添加末点
            if(totalFrames>outputFrames)ValuePoint.Add(totalFrames, 0);//添加终点
            //PS+P1+P2+P5  --  P3+P4+PE
            //PS+CP[0]+CP[1]+CP[4]... -- CP[2]+CP[3]+PE;
            double AreaStart = 0;
            double AreaEnd = outputFrames;
            double tmp;
            //左边界
            //P1
            tmp = Ms2Samples(ControlPoint[0].Key);
            AreaStart = AreaStart + tmp;
            if (!ValuePoint.ContainsKey(AreaStart)) ValuePoint.Add(AreaStart, ControlPoint[0].Value);
            //右边界
            //P4
            tmp = Ms2Samples(ControlPoint[3].Key);
            AreaEnd = AreaEnd - tmp;
            if (AreaEnd < AreaStart) AreaEnd = AreaStart + 1;
            if (!ValuePoint.ContainsKey(AreaEnd)) ValuePoint.Add(AreaEnd, ControlPoint[3].Value);
            //左2边界
            //P2
            tmp = Ms2Samples(ControlPoint[1].Key);
            AreaStart = AreaStart + tmp;
            if (AreaStart > AreaEnd) AreaStart = AreaEnd - 1;
            if (!ValuePoint.ContainsKey(AreaStart)) ValuePoint.Add(AreaStart, ControlPoint[1].Value);
            //右2边界
            //P3
            tmp = Ms2Samples(ControlPoint[2].Key);
            AreaEnd = AreaEnd - tmp;
            if (AreaEnd < AreaStart) AreaEnd = AreaStart + 1;
            if (!ValuePoint.ContainsKey(AreaEnd)) ValuePoint.Add(AreaEnd, ControlPoint[2].Value);
            //中点
            for (int i = 4; i < ControlPoint.Count - 1; i++)
            {
                tmp = Ms2Samples(ControlPoint[i].Key);
                AreaStart = AreaStart + tmp;
                if (AreaStart < AreaEnd)
                {
                    if (!ValuePoint.ContainsKey(AreaStart)) ValuePoint.Add(AreaStart, ControlPoint[1].Value);
                }
            }
            //计算线段
            double[] KeyArray = ValuePoint.Keys.ToArray();
            MathHelper.SegmentLine.SegmentPoint sp = new MathHelper.SegmentLine.SegmentPoint(KeyArray[0], ValuePoint[KeyArray[0]]);
            for (int i = 1; i < KeyArray.Length; i++)
            {
                ret.Add(new MathHelper.SegmentLine(
                    new MathHelper.SegmentLine.SegmentPoint(KeyArray[i - 1], ValuePoint[KeyArray[i - 1]]),
                    new MathHelper.SegmentLine.SegmentPoint(KeyArray[i], ValuePoint[KeyArray[i]]),
                    i==1?MathHelper.SegmentLine.OverflowType.ReturnLimit:MathHelper.SegmentLine.OverflowType.ReturnNull,
                    i == KeyArray.Length - 1? MathHelper.SegmentLine.OverflowType.ReturnLimit : MathHelper.SegmentLine.OverflowType.ReturnNull
                    ));
            }
            return ret;
        }

        public float FramesMix_old(float framesA, float framesB)
        {
            Int16 a = (Int16)(framesA * 32768);
            Int16 b = (Int16)(framesB * 32768);

            int a_t;
            int b_t;
            int r1;
            Int16 rel = 0;

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
            rel = ((Int16)r1);

            float ret= rel / 32768f;
            return ret;
        }
        public float FramesMix(float framesA, float framesB)
        {
            float a_t = framesA + 1;
            float b_t = framesB + 1;
            float r_t = 1;
            float ret = 0;
            if (a_t <= 1 && b_t <= 1)
            {
                r_t = (a_t * b_t);
            }
            else
            {
                r_t = 2*(a_t + b_t) - (a_t * b_t) - 2;
                if (r_t >= 2)
                {
                    r_t = 2;
                }
            }
            ret = r_t - 1;
            return ret;
        }
               
    }
}
