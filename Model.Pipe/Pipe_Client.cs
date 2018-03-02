using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using VocalUtau.WavTools.Model.Wave;
using VocalUtau.WavTools.Model.Wave.NAudio.Extra;

namespace VocalUtau.WavTools.Model.Pipe
{
    public class Pipe_Client
    {
        string PipeName = "VocalUtau.WavTool";
        int Timeout = 500;
        MemoryStream bufferstream;
        long OvrBufferSize = 0;
        bool isFilled = false;
        Semaphore semaphore;
        public Pipe_Client(string PipeName,int Timeout=500)
        {
            this.PipeName = PipeName;
            this.Timeout = Timeout;
            bufferstream = new MemoryStream();
            isFilled = false;
            semaphore = new Semaphore(1, 1, PipeName);
        }
        public void Renew()
        {
            try
            {
                bufferstream.Close();
            }
            catch { ;}
            try
            {
                bufferstream.Dispose();
            }
            catch { ;}
            bufferstream = new MemoryStream(); 
            isFilled = false;
        }
        public void LockWavFile()
        {
            Console.WriteLine("WaitSemaphore");
            semaphore.WaitOne();
            Console.WriteLine("GetSemaphore");
        }
        public void UnLockWavFile()
        {
            try
            {
                semaphore.Release();
            }
            catch { ;}
            Console.WriteLine("ReleaseSemaphore");
        }

        public void Append(string WavFileName, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV, double DropTime = 0)
        {
            Console.WriteLine("Ask For Overlap time:{0}", ovr);
            FillOvr(ovr);
            Console.WriteLine("Wav Appending");
            WavAppender.AppendWork(bufferstream, WavFileName, offset, length,
                ovr, KV,DropTime);
        }
        public void Append(Stream InputRiffStream, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV,double DropTime=0)
        {
            Console.WriteLine("Ask For Overlap time:{0}", ovr);
            FillOvr(ovr);
            Console.WriteLine("Wav Appending");
            WavAppender.AppendWork(bufferstream, InputRiffStream, offset, length,
                ovr, KV,DropTime);
        }
        private void FillOvr(double ovr)
        {
            if (isFilled) return;
            FormatHelper fhelper = new FormatHelper(IOHelper.NormalPcmMono16_Format);
            OvrBufferSize = fhelper.Ms2Bytes(ovr);
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                try
                {
                    try
                    {
                        pipeStream.Connect(Timeout);
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Timeout error!");
                        return;
                    }
                    BinaryReader sr = new BinaryReader(pipeStream);
                    BinaryWriter sw = new BinaryWriter(pipeStream);
                    sw.Write((Int64)(-2));
                    sw.Write(OvrBufferSize);
                    sw.Flush();
                    pipeStream.WaitForPipeDrain();
                    Int64 Response=sr.ReadInt64();
                    byte[] ResponByte = new byte[0];
                    if (Response == -3)
                    {
                        Int64 OvrBS = sr.ReadInt64();
                        if (OvrBS > 0)
                        {
                            ResponByte = new byte[OvrBS];
                            sr.Read(ResponByte, 0, (int)OvrBS);
                        }
                    }
                    try
                    {
                        sr.Close();
                    }
                    catch { ;}
                    try
                    {
                        sw.Close();
                    }
                    catch { ;}
                    bufferstream.Write(ResponByte, 0, ResponByte.Length);
                    isFilled = true;
                    Console.WriteLine("Asked OK! BufferSize:{0},TotalRecieve:{1}", OvrBufferSize,ResponByte.Length);
                }
                catch { ;}
            }
        }
        public void Flush()
        {
            Console.WriteLine("Buffer Flushing");
            SendBuffer(OvrBufferSize);
            Renew();
        }
        private void SendBuffer(long OvrSize)
        {
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                try
                {
                    try
                    {
                        pipeStream.Connect(Timeout);
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Timeout error!");
                        semaphore.Release();
                        return;
                    }
                    BinaryWriter sw = new BinaryWriter(pipeStream);
                    byte[] byt = bufferstream.ToArray();
                    Int64 ByteLength = byt.Length;
                    sw.Write((Int64)(-4));
                    sw.Write(OvrSize);
                    sw.Write(ByteLength);
                    sw.Write(byt, 0, byt.Length);
                    sw.Flush();
                    sw.Close();
                    Console.WriteLine("Sended OK! BufferSize:{0},TotalStream:{1}",ByteLength,ByteLength+4+8+8);
                }
                catch {}
            }
        }

        public void SendEndSignal(Int64 SignalData=-1)
        {
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                try
                {
                    try
                    {
                        pipeStream.Connect(Timeout);
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Timeout error!");
                        return;
                    }
                    BinaryWriter sw = new BinaryWriter(pipeStream);
                    sw.Write((Int64)(-1));
                    sw.Write(SignalData);
                    sw.Flush();
                    sw.Close();
                    Console.WriteLine("Sended OK! End Signal,SignalCode:-1,Data:{0}",SignalData);
                }
                catch { ; }
            }
        }
        
        public void Dispose()
        {
            try
            {
                bufferstream.Close();
            }
            catch { ;}
            try
            {
                bufferstream.Dispose();
            }
            catch { ;}
        }
    }
}
