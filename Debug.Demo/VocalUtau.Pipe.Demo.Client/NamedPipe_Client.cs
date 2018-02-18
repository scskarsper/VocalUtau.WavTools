using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace VocalUtau.Pipe.Demo.Client
{
    class NamedPipe_Client
    {
        public static void SendRequest(string NameSign, string[] args)
        {
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", "VocaUtau." + NameSign,
                PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                try
                {
                    pipeStream.Connect(500);
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Timeout error!");
                    return;
                }
                StreamWriter sw = new StreamWriter(pipeStream);
                sw.AutoFlush = true;
                string jon=String.Join("\\|\\", args);
                sw.WriteLine(jon);
                sw.Close();
                Console.WriteLine("Sended OK!");
            }

        }   
    }
}
