using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.WavTools.Model.Wave
{
    public class WavAppender
    {
        public static void AppendWork(Stream OutputStream, string InputFilename, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV, double DropTime, uint HeadLength = 0)
        {
            FileStream fs = null;
            if (System.IO.File.Exists(InputFilename))
            {
                fs=new FileStream(InputFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            AppendWork(OutputStream, fs, offset, length, ovr, KV, DropTime, HeadLength);
            if(fs!=null)fs.Close();
        }
        public static void AppendWork(Stream OutputStream, Stream InputStream, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV,double droptime,uint HeadLength=0)
        {

            //Init
            WaveFormat outputFormat = new WaveFormat(44100, 1);
            FormatHelper fhelper = new FormatHelper(outputFormat);
            MathHelper mhelper = new MathHelper();


            //IOInit
            WaveFileReader ifh = null;
            try { ifh = new WaveFileReader(InputStream); }
            catch { ;}
            IOHelper ofh = new IOHelper(OutputStream, outputFormat,HeadLength);

            //Prepare
            OutputStream.Seek(0, SeekOrigin.End);
            int outputFrames = fhelper.Ms2Samples(length);
            int overlapFrames = fhelper.Ms2Samples(ovr);
            int offsetFrames = fhelper.Ms2Samples(offset);
            List<MathHelper.SegmentLine> EnvlopeLines = fhelper.KV2SegmentLines(outputFrames, KV);

            //CheckDelay
            int dropFrames = fhelper.Ms2Samples(droptime);
            if (dropFrames < 0) dropFrames = 0;
            if (dropFrames > 0)
            {
                if (dropFrames > outputFrames - overlapFrames)
                {
                    return;
                }
            }

            //SeekInput
            if (ifh != null)
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
                if (OutputStream.Length >= seekMap)
                {
                    OutputStream.Seek((-1) * seekMap, SeekOrigin.End);
                }
            }
            else if (overlapFrames < 0)
            {
                OutputStream.Seek(0, SeekOrigin.End);
                for (int i = 0; i < -overlapFrames; i++)
                {
                    if (dropFrames > 0)
                    {
                        dropFrames--;
                    }else
                    {
                        ofh.WriteSample(0.0f);
                    }
                }
                overlapFrames = 0;
            }
            for (int currentFrame = 0; currentFrame < outputFrames; currentFrame++)
            {
                float[] SampleFrame = new float[1] { 0.0f };

                if (ifh != null && ifh.Position < ifh.Length)
                {
                    SampleFrame = ifh.ReadNextSampleFrame();
                    if (SampleFrame == null)
                    {
                        SampleFrame = new float[1] { 0.0f };
                    }
                }
                float SampleMono = SampleFrame.Length == 1 ? SampleFrame[0] : mhelper.floatAverage(SampleFrame);
                if (SampleMono != 0)
                {
                    //修复：切断音
                    double percent = MathHelper.SegmentLine.SegmentsGraphic(EnvlopeLines, currentFrame);
                    SampleMono = (float)(SampleMono * (percent / 100.0));
                }


                //OverFloat
                if (overlapFrames > 0)
                {
                    float oldFrame = 0.0f;

                    float[] oft = ofh.ReadNextSampleFrame();
                    if (ofh.Length > 0)
                    {
                        oldFrame = oft[0];
                        OutputStream.Seek(-1 * fhelper.Samples2Bytes(1), SeekOrigin.Current);
                        overlapFrames--;
                        float a2 = fhelper.FramesMix(SampleMono, oldFrame);
                        float SampleMix = a2;
                        if (dropFrames > 0)
                        {
                            ofh.WriteSample(oldFrame);//放弃Seek
                            dropFrames--;
                        }
                        else
                        {
                            ofh.WriteSample(SampleMix);
                        }
                    }
                    else
                    {
                        if (dropFrames > 0)
                        {
                            dropFrames--;
                        }
                        else
                        {
                            ofh.WriteSample(SampleMono);
                        }
                        overlapFrames = 0;
                    }
                }
                else
                {
                    if (dropFrames > 0)
                    {
                        dropFrames--;
                    }
                    else
                    {
                        ofh.WriteSample(SampleMono);
                    }
                }
            }
        }
    }
}
