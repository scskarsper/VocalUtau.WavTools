using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.FileFormats.Wav;

namespace WavtoolSharp.NAudio.Extra
{
    class IOHelper : WaveStream
    {
        WaveFormat waveFormat;
        private readonly BinaryWriter writer;
        private readonly bool ownInput;
        private readonly long dataPosition;
        private readonly long HeadLength;
        private readonly object lockObject = new object();
        private Stream waveStream;

        public IOHelper(Stream inputStream, WaveFormat waveformat, uint HeadLength = 0)
        {
            this.waveStream = inputStream;
            this.waveFormat = waveformat;
            writer = new BinaryWriter(inputStream);
            dataPosition = HeadLength;
            inputStream.Seek(HeadLength, SeekOrigin.Begin);
            Position = 0;
        }
        private long dataChunkLength
        {
            get
            {
                return waveStream.Length - HeadLength;
            }
        }
        /// <summary>
        /// Cleans up the resources associated with this WaveFileReader
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources.
                if (waveStream != null)
                {
                    // only dispose our source if we created it
                    if (ownInput)
                    {
                        waveStream.Dispose();
                    }
                    waveStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "WaveFileReader was not disposed");
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get
            {
                return waveFormat;
            }
        }

        /// <summary>
        /// This is the length of audio data contained in this WAV file, in bytes
        /// (i.e. the byte length of the data chunk, not the length of the WAV file itself)
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override long Length
        {
            get
            {
                return dataChunkLength;
            }
        }

        /// <summary>
        /// Number of Sample Frames  (if possible to calculate)
        /// This currently does not take into account number of channels
        /// Multiply number of channels if you want the total number of samples
        /// </summary>
        public long SampleCount
        {
            get
            {
                if (waveFormat.Encoding == WaveFormatEncoding.Pcm ||
                    waveFormat.Encoding == WaveFormatEncoding.Extensible ||
                    waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    return dataChunkLength / BlockAlign;
                }
                // n.b. if there is a fact chunk, you can use that to get the number of samples
                throw new InvalidOperationException("Sample count is calculated only for the standard encodings");
            }
        }

        /// <summary>
        /// Position in the WAV data chunk.
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                return waveStream.Position - dataPosition;
            }
            set
            {
                lock (lockObject)
                {
                    value = Math.Min(value, Length);
                    // make sure we don't get out of sync
                    value -= (value % waveFormat.BlockAlign);
                    waveStream.Position = value + dataPosition;
                }
            }
        }
        public override void Write(byte[] array, int offset, int count)
        {
            waveStream.Write(array, offset, count);
        }
        public override int Read(byte[] array, int offset, int count)
        {
            if (count % waveFormat.BlockAlign != 0)
            {
                throw new ArgumentException("Must read complete blocks: requested {" + count.ToString() + "");
            }
            lock (lockObject)
            {
                // sometimes there is more junk at the end of the file past the data chunk
                if (Position + count > dataChunkLength)
                {
                    count = (int)(dataChunkLength - Position);
                }
                return waveStream.Read(array, offset, count);
            }
        }
        public float[] ReadNextSampleFrame()
        {
            switch (waveFormat.Encoding)
            {
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.IeeeFloat:
                case WaveFormatEncoding.Extensible: // n.b. not necessarily PCM, should probably write more code to handle this case
                    break;
                default:
                    throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
            var sampleFrame = new float[waveFormat.Channels];
            int bytesToRead = waveFormat.Channels * (waveFormat.BitsPerSample / 8);
            byte[] raw = new byte[bytesToRead];
            int bytesRead = Read(raw, 0, bytesToRead);
            if (bytesRead == 0) return null; // end of file
            if (bytesRead < bytesToRead) throw new InvalidDataException("Unexpected end of file");
            int offset = 0;
            for (int channel = 0; channel < waveFormat.Channels; channel++)
            {
                if (waveFormat.BitsPerSample == 16)
                {
                    sampleFrame[channel] = BitConverter.ToInt16(raw, offset) / 32768f;
                    offset += 2;
                }
                else if (waveFormat.BitsPerSample == 24)
                {
                    sampleFrame[channel] = (((sbyte)raw[offset + 2] << 16) | (raw[offset + 1] << 8) | raw[offset]) / 8388608f;
                    offset += 3;
                }
                else if (waveFormat.BitsPerSample == 32 && waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    sampleFrame[channel] = BitConverter.ToSingle(raw, offset);
                    offset += 4;
                }
                else if (waveFormat.BitsPerSample == 32)
                {
                    sampleFrame[channel] = BitConverter.ToInt32(raw, offset) / (Int32.MaxValue + 1f);
                    offset += 4;
                }
                else
                {
                    throw new InvalidOperationException("Unsupported bit depth");
                }
            }
            return sampleFrame;
        }
        private readonly byte[] value24 = new byte[3];
        public void WriteSample(float sample)
        {
            if (WaveFormat.BitsPerSample == 16)
            {
                writer.Write((Int16)(Int16.MaxValue * sample));
            }
            else if (WaveFormat.BitsPerSample == 24)
            {
                var value = BitConverter.GetBytes((Int32)(Int32.MaxValue * sample));
                value24[0] = value[1];
                value24[1] = value[2];
                value24[2] = value[3];
                writer.Write(value24);
            }
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                writer.Write(UInt16.MaxValue * (Int32)sample);
            }
            else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                writer.Write(sample);
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        public static byte[] GenerateHead(WaveFormat format,int FileLengthWithoutHead=-1)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms, System.Text.Encoding.UTF8);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write((int)0); // placeholder
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            format.Serialize(writer);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write((int)0); // placeholder
            long HeadSize = -1;
            HeadSize = ms.Position;
            
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((UInt32)(FileLengthWithoutHead + HeadSize - 8));

            writer.Seek((int)HeadSize-4, SeekOrigin.Begin);
            writer.Write((UInt32)FileLengthWithoutHead);
            byte[] ret = ms.ToArray();
            ms.Dispose();
            return ret;
        }

        /// <summary>
        /// Writes 32 bit floating point samples to the Wave file
        /// They will be converted to the appropriate bit depth depending on the WaveFormat of the WAV file
        /// </summary>
        /// <param name="samples">The buffer containing the floating point samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of floating point samples to write</param>
        public void WriteSamples(float[] samples, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                WriteSample(samples[offset + n]);
            }
        }


    }
}
