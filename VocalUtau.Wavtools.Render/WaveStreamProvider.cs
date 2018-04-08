using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VocalUtau.Wavtools.Render
{
    public class WaveStreamProvider : IWaveProvider
    {
        public delegate void OnSyncPositionHandler(Stream Stream);
        public event OnSyncPositionHandler SyncPosition;

        private readonly WaveFormat waveFormat;

        private long _UnreadableHead = 0;

        public long UnreadableHead
        {
            get { return _UnreadableHead; }
            set { _UnreadableHead = value; }
        }
        private long _UnreadableTail = 0;

        public long UnreadableTail
        {
            get { return _UnreadableTail; }
            set { _UnreadableTail = value; }
        }
        private Stream _BasicStream = null;

        public Stream BasicStream
        {
            get { return _BasicStream; }
            set { _BasicStream = value; }
        }

        /// <summary>
        /// Creates a new buffered WaveProvider
        /// </summary>
        /// <param name="waveFormat">WaveFormat</param>
        public WaveStreamProvider(WaveFormat waveFormat, Stream BasicStream, long UnreadableHead = 0, long UnreadableTail = 0)
        {
            this.waveFormat = waveFormat;
            this._UnreadableHead = UnreadableHead;
            this._UnreadableTail = UnreadableTail;
            this._BasicStream = BasicStream;
        }

        long _StreamLength = long.MaxValue;

        public long StreamLength
        {
            get { return _StreamLength==long.MaxValue?this._BasicStream.Position:_StreamLength; }
            set { _StreamLength = value; }
        }


        public long AvaliableLength
        {
            get
            {
                long ret = this._BasicStream.Length - UnreadableTail - UnreadableHead;
                return ret;
            }
        }

        public long CurrentPosition
        {
            get
            {
                long ret=this._BasicStream.Position - UnreadableHead;
                return ret;
            }
        }

        public TimeSpan BufferPlayPosition
        {
            get
            {
                return TimeSpan.FromSeconds((double)CurrentPosition / waveFormat.AverageBytesPerSecond);
            }
        }

        public TimeSpan BufferDuration
        {
            get
            {
                return TimeSpan.FromSeconds((double)AvaliableLength / waveFormat.AverageBytesPerSecond);
            }
        }

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            if (BasicStream != null) // not yet created
            {
                if (SyncPosition != null) SyncPosition(_BasicStream);
                if (BasicStream.Position < UnreadableHead)
                {
                    BasicStream.Position = UnreadableHead;
                }
                if (CurrentPosition + count < AvaliableLength)
                {
                    read = BasicStream.Read(buffer, offset, count);
                }
                else if (CurrentPosition < AvaliableLength)
                {
                    read = BasicStream.Read(buffer, offset, (int)(AvaliableLength - CurrentPosition));
                }
            }
            if (read < count)
            {
                // zero the end of the buffer
                Array.Clear(buffer, offset + read, count - read);
                read = count;
            }
            return read;
        }
    }
}
