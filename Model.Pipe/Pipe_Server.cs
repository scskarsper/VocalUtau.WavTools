using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools.Model.Pipe
{
    public class Pipe_Server
    {
        public delegate void RecievePipeStreamHandler(long BufferLength,byte[] BufferData);
        public event RecievePipeStreamHandler RecievePipeStream;
        public delegate void RecieveEndSignalHandler(Int64 SignalData);
        public event RecieveEndSignalHandler RecieveEndSignal;

        string PipeName = "VocalUtau.WavTool";
        Stream bufferStream;
        BinaryWriter bufferWriter;
        long bufferPosition = 0;
        bool Exit = false;
        NamedPipeServerStream pipeStream;
        IAsyncResult hand;

        public Pipe_Server(string PipeName)
        {
            bufferStream = new MemoryStream();
            bufferWriter = new BinaryWriter(bufferStream);
            this.PipeName = PipeName;
            bufferPosition = 0;
        }
        public Pipe_Server(string PipeName,Stream BaseStream,int bufferStartPosition=0)
        {
            bufferStream = BaseStream;
            bufferWriter = new BinaryWriter(bufferStream);
            bufferPosition = bufferStartPosition;
            this.PipeName = PipeName;
        }


        public void ExitServer()
        {
            if (pipeStream != null)
            {
                pipeStream.EndWaitForConnection(hand);
                try
                {
                    pipeStream.Dispose();
                }
                catch { ; }
            }
            pipeStream = null;
            Exit = true;
        }
        public void StartServer()
        {
            pipeStream = new NamedPipeServerStream(PipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            hand = pipeStream.BeginWaitForConnection(WaitForConnectionAsyncCallback, pipeStream);
        }
        private void WaitForConnectionAsyncCallback(IAsyncResult result)
        {
            long BufferSize = 0;
            Console.WriteLine("Client connected.");
            NamedPipeServerStream pipeStream = (NamedPipeServerStream)result.AsyncState;
            pipeStream.EndWaitForConnection(result);
            bool isEndSignal = false;
            Int64 SignalData = 0;
            byte[] buf=new byte[0];
            using (BinaryReader sr = new BinaryReader(pipeStream))
            {
                BufferSize=sr.ReadInt64();
                if (BufferSize == -1)
                {
                    SignalData = sr.ReadInt64();
                    isEndSignal = true;
                }
                else
                {
                    buf = new byte[BufferSize];
                    sr.Read(buf, 0, (int)BufferSize);
                }
            }
            pipeStream.Dispose();
            hand = null ;
            StartServer();
            if (isEndSignal)
            {
                if (RecieveEndSignal != null) RecieveEndSignal(SignalData);
            }
            else
            {
                bufferWriter.BaseStream.Position = bufferPosition;
                bufferWriter.Write(buf, 0, buf.Length);
                bufferWriter.Flush();
                bufferWriter.Seek(0, SeekOrigin.Current);
                bufferPosition = bufferWriter.BaseStream.Position;
                if (RecievePipeStream != null) RecievePipeStream(BufferSize, buf);
            }
        }

        public void Dispose()
        {
            bufferPosition = 0;
            try
            {
                pipeStream.Dispose();
            }
            catch { ;}
            try
            {
                bufferWriter.Dispose();
            }
            catch { ;}
            try
            {
                bufferStream.Dispose();
            }
            catch { ;}
        }
    }
}
