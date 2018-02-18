using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using VocalUtau.WavTools.Model.Wave;

namespace VocalUtau.WavTools.Model.Pipe
{
    public class Pipe_Client
    {
        string PipeName = "VocalUtau.WavTool";
        int Timeout = 500;
        MemoryStream bufferstream;
        public Pipe_Client(string PipeName,int Timeout=500)
        {
            this.PipeName = PipeName;
            this.Timeout = Timeout;
            bufferstream = new MemoryStream();
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
        }


        public void Append(string WavFileName, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV)
        {
            WavFile_Datas.wfd_append(bufferstream, WavFileName, offset, length,
                ovr, KV);
        }
        public void Append(Stream InputRiffStream, double offset, double length,
        double ovr, List<KeyValuePair<double, double>> KV)
        {
            WavFile_Datas.wfd_append(bufferstream, InputRiffStream, offset, length,
                ovr, KV);
        }

        public void Flush()
        {
            SendBuffer();
            Renew();
        }
        private void SendBuffer()
        {
            Semaphore semaphore = new Semaphore(1, 1, PipeName);
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                semaphore.WaitOne();
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
                    sw.Write(ByteLength);
                    sw.Write(byt, 0, byt.Length);
                    sw.Flush();
                    sw.Close();
                    Console.WriteLine("Sended OK! BufferSize:{0},TotalStream:{1}",ByteLength,ByteLength+8);
                }
                catch { semaphore.Release();}
            }
        }

        public void SendEndSignal(Int64 SignalData=-1)
        {
            Semaphore semaphore = new Semaphore(1, 1, PipeName);
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                semaphore.WaitOne();
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
                    sw.Write((Int64)(-1));
                    sw.Write(SignalData);
                    sw.Flush();
                    sw.Close();
                    Console.WriteLine("Sended OK! End Signal,SignalCode:-1,Data:{0}",SignalData);
                }
                catch { semaphore.Release(); }
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
