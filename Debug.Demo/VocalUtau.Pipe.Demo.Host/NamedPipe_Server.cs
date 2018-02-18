using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace VocalUtau.Pipe.Demo.Host
{
    class NamedPipe_Server
    {
        public delegate void RecieveArgsHandler(string[] Args);
        public event RecieveArgsHandler RecieveArgs;

        string NamedSign="WT";
        bool Exit=false;
        public NamedPipe_Server(string NamedSign)
        {
            this.NamedSign=NamedSign;
        }
        public void ExitServer()
        {
            if (pipeStream != null)
            {
                pipeStream.EndWaitForConnection(hand);
            }
            Exit=true;
        }
        NamedPipeServerStream pipeStream;
        IAsyncResult hand;
        public void StartServer()
        { 
            pipeStream = new NamedPipeServerStream("VocaUtau."+NamedSign,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous|PipeOptions.WriteThrough);
            hand=pipeStream.BeginWaitForConnection(WaitForConnectionAsyncCallback,pipeStream);
        }
        private void WaitForConnectionAsyncCallback(IAsyncResult result)
        {
            string[] args=new string[0];
            Console.WriteLine("Client connected.");
            NamedPipeServerStream pipeStream = (NamedPipeServerStream)result.AsyncState;
            pipeStream.EndWaitForConnection(result);
            using (StreamReader sr = new StreamReader(pipeStream))
            {
                string srr = sr.ReadLine();
                args = srr.Split(new string[] { "\\|\\" }, StringSplitOptions.None);
            }
            pipeStream.Dispose();
            hand = null;
            StartServer();
            if(RecieveArgs!=null)RecieveArgs(args);
        }
    }
}
