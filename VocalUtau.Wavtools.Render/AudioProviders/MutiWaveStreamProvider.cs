using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.Wavtools.Render
{
    public class WaveStreamType
    {
        public WaveStreamType()
        {
            this.Volume = 1.0f;
            this.WaveStream = null;
            this.Tag = null;
            this.Finished = false;
        }
        public WaveStreamType(Stream BaseStream)
        {
            this.Volume = 1.0f;
            this.WaveStream = BaseStream;
            this.Tag = null;
            this.Finished = false;
        }
        public WaveStreamType(float Volume)
        {
            this.Volume = Volume;
            this.WaveStream = null;
            this.Tag = null;
            this.Finished = false;
        }
        public WaveStreamType(Stream BaseStream, float Volume)
        {
            this.Volume = Volume;
            this.WaveStream = BaseStream;
            this.Tag = null;
            this.Finished = false;
        }
        public object Tag { get; set; }
        public Stream WaveStream { get; set; }
        public float Volume { get; set; }

        public bool Finished
        {
            get
            {
                if (_UnreadableTail == 0)
                {
                    if (WaveStream != null)
                    {
                        if (WaveStream.Length - WaveStream.Position < 2)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            set { ;}
        }

        private long _UnreadableTail = 0;

        public long UnreadableTail
        {
            get { return _UnreadableTail; }
            set { _UnreadableTail = value; }
        }
    }
    public class MutiWave16StreamProvider : IWaveProvider
    {
        private readonly WaveFormat waveFormat = IOHelper.NormalPcmMono16_Format;

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        private Dictionary<int, WaveStreamType> _Map = new Dictionary<int, WaveStreamType>();

        public Dictionary<int, WaveStreamType> InputMap
        {
            get { return _Map; }
            set { _Map = value; }
        }

        short Remix(List<KeyValuePair<short, float>> Buffers)
        {
            int value = 0;
            int Count = Buffers.Count;
            for (int i = 0; i < Count; i++)
            {
                value = value + (int)((float)Buffers[i].Key * Buffers[i].Value);
            }
            return (short)(value / Count);
        }
        long EnableLength
        {
            get
            {
                long ReadAble = long.MaxValue;
                foreach (KeyValuePair<int, WaveStreamType> KP in _Map)
                {
                    if (!KP.Value.Finished && KP.Value.WaveStream != null)
                    {
                        long Abt = KP.Value.WaveStream.Length - KP.Value.UnreadableTail;
                        ReadAble = Math.Min(ReadAble, Abt);
                    }
                }
                if (ReadAble == long.MaxValue) return 0;
                return ReadAble;
            }
        }

        long CurrentPosition
        {
            get
            {
                long ReadAble = long.MaxValue;
                foreach (KeyValuePair<int, WaveStreamType> KP in _Map)
                {
                    if (!KP.Value.Finished && KP.Value.WaveStream != null)
                    {
                        long Abt = KP.Value.WaveStream.Position;
                        ReadAble = Math.Min(ReadAble, Abt);
                    }
                }
                if (ReadAble == long.MaxValue) return 0;
                return ReadAble;
            }
        }

        public bool IsAllFinished
        {
            get
            {
                bool isAllFinished = true;
                foreach (KeyValuePair<int, WaveStreamType> KP in _Map)
                {
                    if (!KP.Value.Finished)
                    {
                        isAllFinished = false;
                    }
                }
                return isAllFinished;
            }
        }

        bool _IsEmptyBuffer = false;
        public bool IsEmptyBuffer
        {
            get
            {
                return _IsEmptyBuffer;
            }
        }

        public TimeSpan PlayPosition
        {
            get
            {
                return TimeSpan.FromSeconds((double)CurrentPosition / waveFormat.AverageBytesPerSecond);
            }
        }

        public TimeSpan CurrentDuration
        {
            get
            {
                return TimeSpan.FromSeconds((double)EnableLength / waveFormat.AverageBytesPerSecond);
            }
        }


        public delegate void ProcessEventHandler(object sender);
        public event ProcessEventHandler SoundProcessed;
        public int Read(byte[] buffer, int offset, int count)
        {
            long TotalLen = EnableLength;
            long CurPos = CurrentPosition;
            int read = 0;
            if (CurPos < TotalLen)
            {
                int readCount = count;
                if (CurPos + readCount > TotalLen)
                {
                    readCount = (int)(TotalLen - CurPos);
                }

                foreach (KeyValuePair<int, WaveStreamType> KP in _Map)
                {
                    KP.Value.WaveStream.Position = CurPos;
                }
                for (int i = 0; i < readCount; i = i + 2)
                {
                    List<KeyValuePair<short, float>> SampleTab = new List<KeyValuePair<short, float>>();
                    foreach (KeyValuePair<int, WaveStreamType> KP in _Map)
                    {
                        byte[] Tmp = new byte[2];
                        KP.Value.WaveStream.Read(Tmp, 0, 2);
                        short sample = (short)((Tmp[1] << 8) | Tmp[0]);
                        var newSample = sample * KP.Value.Volume;
                        sample = (short)newSample;
                        SampleTab.Add(new KeyValuePair<short, float>(sample, KP.Value.Volume));
                    }
                    short MixedSample = 0;
                    if(SampleTab.Count>0)MixedSample=Remix(SampleTab);
                    if (MixedSample > Int16.MaxValue) MixedSample = Int16.MaxValue;
                    else if (MixedSample < Int16.MinValue) MixedSample = Int16.MinValue;

                    buffer[offset++] = (byte)(MixedSample & 0xFF);
                    buffer[offset++] = (byte)(MixedSample >> 8);
                    read = read + 2;
                }
            }

            if (read < count)
            {
                // zero the end of the buffer
                Array.Clear(buffer, read, count - read);
                read = count;
                _IsEmptyBuffer = true;
            }
            else
            {
                _IsEmptyBuffer = false;
            }
            if(SoundProcessed!=null)SoundProcessed(this);
            return read;
        }
    }
}
