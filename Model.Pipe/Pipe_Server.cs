using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools.Model.Pipe
{
    public class Pipe_Server : MarshalByRefObject
    {
        public delegate void RecievePipeStreamHandler(long BufferLength, byte[] BufferData, string PipeName);
        public event RecievePipeStreamHandler RecievePipeStream;
        public delegate void RecieveEndSignalHandler(Int64 SignalData, string PipeName);
        public event RecieveEndSignalHandler RecieveEndSignal;

        string PipeName = "VocalUtau.WavTool";
        Stream bufferStream;
        BinaryWriter bufferWriter;
        BinaryReader bufferReader;
        long bufferPosition = 0;
        long bufferStartPosition = 0;
        bool Exit = false;
        NamedPipeServerStream pipeStream;
        IAsyncResult hand;

        public Pipe_Server(string PipeName)
        {
            bufferStream = new MemoryStream();
            bufferWriter = new BinaryWriter(bufferStream);
            bufferReader = new BinaryReader(bufferStream);
            this.PipeName = PipeName;
            bufferPosition = 0;
            this.bufferStartPosition = 0;
        }
        public Pipe_Server(string PipeName,Stream BaseStream,int bufferStartPosition=0)
        {
            bufferStream = BaseStream;
            bufferWriter = new BinaryWriter(bufferStream);
            bufferReader = new BinaryReader(bufferStream);
            bufferPosition = bufferStartPosition;
            this.bufferStartPosition = bufferStartPosition;
            this.PipeName = PipeName;
        }


        public void ExitServer()
        {
            if (pipeStream != null)
            {
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
            hand = pipeStream.BeginWaitForConnection(WaitForConnectionAsyncCallback, new object[]{pipeStream,PipeName});
        }
        private void WaitForConnectionAsyncCallback(IAsyncResult result)
        {
            long BufferSize = 0;
            long OvrSize = 0;
            Console.WriteLine("Client connected.");
            object[] objs=(object[])result.AsyncState;
            NamedPipeServerStream pipeStream = (NamedPipeServerStream)objs[0];
            string pipName = (string)objs[1];
            try
            {
                pipeStream.EndWaitForConnection(result);
                Int64 Signal = 0;
                Int64 SignalData = 0;
                byte[] bufdat = new byte[0];
                using (BinaryReader sr = new BinaryReader(pipeStream))
                {
                    Signal = sr.ReadInt64();
                    if (Signal == -1)
                    {
                        SignalData = sr.ReadInt64();
                    }
                    else if (Signal == -2)
                    {
                        OvrSize = sr.ReadInt64();
                        long bufsiz = bufferPosition - bufferStartPosition;
                        if (bufsiz < OvrSize)
                        {
                            using (BinaryWriter sw = new BinaryWriter(pipeStream))
                            {
                                sw.Write((Int64)(-3));//Sign:SendBack
                                sw.Write((Int64)0);
                            }
                        }
                        else
                        {
                            byte[] OvzBuf = new byte[OvrSize];
                            bufferWriter.BaseStream.Position = bufferPosition - OvrSize;
                            bufferReader.Read(OvzBuf, 0, (int)OvrSize);
                            bufferPosition = bufferWriter.BaseStream.Position;
                            using (BinaryWriter sw = new BinaryWriter(pipeStream))
                            {
                                sw.Write((Int64)(-3));//Sign:SendBack
                                sw.Write((Int64)OvrSize);
                                sw.Write(OvzBuf, 0, OvzBuf.Length);
                            }
                        }
                    }
                    else if (Signal == -4)
                    {
                        OvrSize = sr.ReadInt64();
                        BufferSize = sr.ReadInt64();
                        bufdat = new byte[BufferSize];
                        sr.Read(bufdat, 0, (int)BufferSize);
                    }
                }
                pipeStream.Dispose();
                hand = null;
                StartServer();
                if (Signal == -1)//ExitSignal
                {
                    if (RecieveEndSignal != null) RecieveEndSignal(SignalData, pipName);
                }
                else if (Signal == -2)//OvrSignal
                {
                }
                else if (Signal == -4)//ResponseSingal
                {
                    if (bufferPosition - OvrSize < bufferStartPosition)
                    {
                        bufferWriter.BaseStream.Position = bufferPosition;
                    }
                    else
                    {
                        bufferWriter.BaseStream.Position = bufferPosition - OvrSize;
                    }
                    bufferWriter.Write(bufdat, 0, bufdat.Length);
                    bufferWriter.Flush();
                    bufferWriter.Seek(0, SeekOrigin.Current);
                    bufferPosition = bufferWriter.BaseStream.Position;
                    if (RecievePipeStream != null) RecievePipeStream(BufferSize, bufdat, pipName);
                }
            }
            catch { ;}
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
                bufferReader.Dispose();
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
