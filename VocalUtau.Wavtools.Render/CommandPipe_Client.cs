using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace VocalUtau.Wavtools.Render
{
    public class CommandPipe_Client
    {
       // public CommandPipe_Client(int
        string PipeName = "";
        public CommandPipe_Client(int InstanceId)
        {
            PipeName = "CommandPipe_" + InstanceId.ToString();
        }

        public void SendData(string Data)
        {
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough))
            {
                try
                {
                    try
                    {
                        pipeStream.Connect(500);
                    }
                    catch (TimeoutException)
                    {
                        return;
                    }
                    string message = Data;

                    byte[] data = Encoding.UTF8.GetBytes(message);

                    pipeStream.Write(data, 0, data.Length);
                    pipeStream.Flush();
                    pipeStream.WaitForPipeDrain();
                }
                catch { ; }
            }
        }
    }
}
