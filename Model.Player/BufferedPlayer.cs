using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VocalUtau.WavTools.Model.Wave;

namespace VocalUtau.WavTools.Model.Player
{
    public class BufferedPlayer
    {
        public delegate void BufferEventHandler(object sender);
        public event BufferEventHandler Inited;
        public event BufferEventHandler FilledBuffer;
        public event BufferEventHandler BufferEmpty_Pause;
        public event BufferEventHandler BufferEmpty_Resume;
        public event BufferEventHandler Player_Pause;
        public event BufferEventHandler Player_Resume;
        public event BufferEventHandler Player_Stop;
        public event BufferEventHandler Player_Play;

        Stream buffer; 
        WaveOut waveOut;
        BufferedWaveProvider bufferedWaveProvider = null;
        long headLengthInBuffer = 0;
        long bufferPosition = 0;
        long Untall = 1024;
        public BufferedPlayer(Stream InputStream,long HeadLengthInBuffer=0)
        {
            buffer = InputStream;
            _playbackState = NAudio.Wave.PlaybackState.Stopped;
            headLengthInBuffer = HeadLengthInBuffer;
            bufferPosition = 0;
        }
        public void InitPlayer()
        {
            waveOut = new WaveOut();
            bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100,1));
            waveOut.Init(bufferedWaveProvider);
            _playbackState = NAudio.Wave.PlaybackState.Stopped;
            if (Inited != null) Inited(this);
        }
        public void DisposePlayer()
        {
            waveOut.Stop();
            bufferedWaveProvider.ClearBuffer();
            waveOut.Dispose();
            waveOut = null;
            bufferedWaveProvider = null;
        }
        
        PlaybackState _playbackState;
        public PlaybackState PlaybackState
        {
          get { return _playbackState; }
        }
        public void FillBuffer(long UnreadableTall = -1)
        {
            if (UnreadableTall < 0)
            {
                UnreadableTall = 0;
            }
            Untall = UnreadableTall;
            long offset = UnreadableTall;
            if (headLengthInBuffer>0)
            {
                if (bufferPosition < headLengthInBuffer)
                {
                    if (buffer.Length > headLengthInBuffer)
                    {
                        buffer.Seek(headLengthInBuffer, SeekOrigin.Begin);
                        bufferPosition = headLengthInBuffer;
                    }
                }
                offset = offset + headLengthInBuffer;
            }
            while (buffer.Length - offset > bufferPosition)
            {
                byte[] buf = new byte[1024];
                buffer.Seek(bufferPosition, SeekOrigin.Begin);
                int len = buffer.Read(buf, 0, buf.Length);
                bufferPosition = buffer.Position;
                bufferedWaveProvider.AddSamples(buf, 0, len);
                if (BufferReady)
                {
                    break;
                }
            }
            if(FilledBuffer!=null)FilledBuffer(this);
        }
        public void FillPlayState(long UnreadableTall = -1)
        {
            if (UnreadableTall < 0)
            {
                UnreadableTall = 0;
            }
            Untall = UnreadableTall;
            switch (_playbackState)
            {
                case NAudio.Wave.PlaybackState.Stopped: break;
                case NAudio.Wave.PlaybackState.Paused: break;
                case NAudio.Wave.PlaybackState.Playing:
                    if (waveOut.PlaybackState==NAudio.Wave.PlaybackState.Playing && BufferEmpty)
                    {
                        if (bufferPosition == buffer.Length && UnreadableTall==0)
                        {
                            break;
                        }else
                        {
                            waveOut.Pause();
                            if(BufferEmpty_Pause!=null)BufferEmpty_Pause(this);
                        }
                    }
                    else if (waveOut.PlaybackState == NAudio.Wave.PlaybackState.Paused && BufferReady)
                    {
                        waveOut.Resume();
                        if (BufferEmpty_Resume != null) BufferEmpty_Resume(this);
                    }
                    break;
            }
        }
        public void Buffer_Play()
        {
            if (_playbackState == NAudio.Wave.PlaybackState.Paused && waveOut.PlaybackState == NAudio.Wave.PlaybackState.Paused)
            {
                waveOut.Resume();
                _playbackState = NAudio.Wave.PlaybackState.Playing;
                if (Player_Resume != null) Player_Resume(this);
            }
            else if (waveOut.PlaybackState != NAudio.Wave.PlaybackState.Playing)
            {
                waveOut.Play();
                _playbackState = NAudio.Wave.PlaybackState.Playing;
                if (Player_Play != null) Player_Play(this);
            }
        }
        public void Buffer_Pause()
        {
            waveOut.Pause();
            _playbackState = NAudio.Wave.PlaybackState.Paused;
            if (Player_Pause != null) Player_Pause(this);
        }
        public void Buffer_Stop()
        {
            waveOut.Stop();
            _playbackState = NAudio.Wave.PlaybackState.Stopped;
            if (Player_Stop != null) Player_Stop(this);
        }
        public bool BufferReady
        {
            get
            {
                return BufferPercent > 0.8;
            }
        }
        public bool BufferEmpty
        {
            get
            {
                return BufferPercent < 0.1;
            }
        }
        public double BufferPercent
        {
            get
            {
                return (double)bufferedWaveProvider.BufferedDuration.Ticks / (double)bufferedWaveProvider.BufferDuration.Ticks;
            }
        }
        public double StreamPercent
        {
            get
            {
                try
                {
                    return (double)bufferPosition / (double)buffer.Length;
                }
                catch { return 0; }
            }
        }
        public double UntallPercent
        {
            get
            {
                if (Untall <= 0) return 1;
                long upc=0;
                try
                {
                    upc = buffer.Length - bufferPosition;
                }catch{;}
                double pcf = (double)upc / (double)Untall;
                return pcf;
            }
        }
    }
}
