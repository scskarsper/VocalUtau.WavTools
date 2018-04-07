using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace VocalUtau.Wavtools.Render
{
    public class CommandPipe_Server
    {
        public delegate void PipeRecieveEventHandler(string data);
        public event PipeRecieveEventHandler OnRecieve;
        NamedPipeServerStream _pipe;
        string PipeName = "";
        public CommandPipe_Server(int InstanceId)
        {
            PipeName = "CommandPipe_" + InstanceId.ToString();
            CreateNewPipe();
        }

        private void CreateNewPipe()
        {
            _pipe = new NamedPipeServerStream
                (
                    PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                    1024,
                    1024
                 );
            _pipe.BeginWaitForConnection(WaitForConnectionCallback, _pipe);
        }

        private void WaitForConnectionCallback(IAsyncResult ar)
        {
            var pipeServer = (NamedPipeServerStream)ar.AsyncState;

            pipeServer.EndWaitForConnection(ar);

            var data = new byte[1024];

            var count = pipeServer.Read(data, 0, 1024);
            string message = "";
            if (count > 0)
            {
                // 通信双方可以约定好传输内容的形式，例子中我们传输简单文本信息。

                message = Encoding.UTF8.GetString(data, 0, count);


                //收到的信息
            }

            pipeServer.Close();
            pipeServer.Dispose();

            CreateNewPipe();

            if (count > 0) if (OnRecieve != null) OnRecieve(message);
        }

        public void Send(string Data)
        {
            if (_pipe.IsConnected)
            {
                try
                {
                    string message = Data;

                    byte[] data = Encoding.UTF8.GetBytes(message);

                    _pipe.Write(data, 0, data.Length);
                    _pipe.Flush();
                    _pipe.WaitForPipeDrain();
                }
                catch { }
            }
        }
    }
}
