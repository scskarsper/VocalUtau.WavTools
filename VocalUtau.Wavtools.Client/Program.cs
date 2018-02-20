using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VocalUtau.WavTools.Model.Args;
using VocalUtau.WavTools.Model.Pipe;

namespace VocalUtau.Wavtools.Client
{
    class Program
    {
        static void SendEnd()
        {
            Console.WriteLine("---- Work As Pipe ----");
            Pipe_Client pclient = new Pipe_Client("VocalUtau.WavTool.PPC", 2000);
            pclient.LockWavFile();
            pclient.SendEndSignal(-1);
            pclient.UnLockWavFile();
            pclient.Dispose();
        }
        static void Main(string[] args)
        {
            ArgsStruct p = ArgsParser.parseArgs(args,false);
            if (p == null)
            {
                ArgsParser.printUsage();
                Console.WriteLine("Commands:");
                Console.WriteLine("\t--command-flush\tSend a End Signal to tell server all is finished");
                return;
            }
            if (p.Commands.Contains("flush"))
            {
                SendEnd();
                return;
            }
            ArgsParser.printArgs(p);
            Console.WriteLine("---- Work As Pipe ----");
            Pipe_Client pclient = new Pipe_Client("VocalUtau.WavTool.PPC", 2000);
            pclient.LockWavFile();
            pclient.Append(p.Inputfilename, p.Offset, p.Length, p.Ovr, p.PV);
            pclient.Flush();
            pclient.UnLockWavFile();
            pclient.Dispose();
        }
    }
}
