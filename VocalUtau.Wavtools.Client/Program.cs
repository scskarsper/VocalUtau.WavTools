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
            pclient.SendEndSignal(-1);
            pclient.Dispose();
        }
        static void Main(string[] args)
        {
            if (args.Length>0 && args[0].ToLower() == "-e")
            {
                SendEnd();
                return;
            }
            ArgsStruct p = ArgsParser.parseArgs(args);
            if (p == null)
            {
                ArgsParser.printUsage();
                Console.WriteLine("\nIf you want to send a End Signal to tell server all is finished,Command:\nwavtool.net -e");
                return;
            }
            ArgsParser.printArgs(p);
            Console.WriteLine("---- Work As Pipe ----");
            Pipe_Client pclient = new Pipe_Client("VocalUtau.WavTool.PPC",2000);
            pclient.Append(p.Inputfilename, p.Offset, p.Length, p.Ovr, p.PV);
            pclient.Flush();
            pclient.Dispose();
        }
    }
}
