using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools
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
        bool buffHavehead = false;
        public BufferedPlayer(Stream InputStream,bool haveWavHead=false)
        {
            buffer = InputStream;
            _playbackState = NAudio.Wave.PlaybackState.Stopped;
            buffHavehead = haveWavHead;
        }
        public void InitPlayer()
        {
            waveOut = new WaveOut();
            bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(WavFile_Heads.Wfh_samplerate,WavFile_Heads.Wfh_channels));
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
            long offset = UnreadableTall;
            if (buffHavehead)
            {
                if (buffer.Position < WavFile_Heads.Wfh_length)
                {
                    if (buffer.Length > WavFile_Heads.Wfh_length)
                    {
                        buffer.Seek(WavFile_Heads.Wfh_length, SeekOrigin.Begin);
                    }
                }
                offset = offset + WavFile_Heads.Wfh_length;
            }
            while (buffer.Length - offset > buffer.Position)
            {
                byte[] buf = new byte[1024];
                int len = buffer.Read(buf, 0, buf.Length);
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
            switch (_playbackState)
            {
                case NAudio.Wave.PlaybackState.Stopped: break;
                case NAudio.Wave.PlaybackState.Paused: break;
                case NAudio.Wave.PlaybackState.Playing:
                    if (waveOut.PlaybackState==NAudio.Wave.PlaybackState.Playing && BufferEmpty)
                    {
                        if (buffer.Position == buffer.Length && UnreadableTall==0)
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
    }
}
