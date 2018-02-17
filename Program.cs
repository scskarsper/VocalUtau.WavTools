using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VocalUtau.WavTools
{
    class Program
    {
        static void StartFill(object pr)
        {
            object[] obj = (object[])pr;
            WavTool_Prg wtool = (WavTool_Prg)obj[0];
            System.IO.FileStream reader = (System.IO.FileStream)obj[1];
            while (true)
            {
                if (wtool.LengthTotals.Count > 2)
                {
                    int prr = wtool.LengthTotals.Count - (wtool.IsFinished ? 1 : 3);
                    byte[] byt = new byte[1024];
                    while (reader.Position < wtool.LengthTotals[prr] + 44)
                    {
                        reader.Read(byt, 0, byt.Length);
                        Debug_NPlayer.AddBytes(byt);
                        if (Debug_NPlayer.BufferFull) break;
                    }
                }
                Thread.Sleep(1000);
            }
        }
        static void Main(string[] args)
        {
            Debug_NPlayer.Init();

            ArgsStruct p=ArgsParser.parseArgs(args);
            if (p == null)
            {
                ArgsParser.printUsage();
                return;
            }
            
            WavTool_Prg wtool = new WavTool_Prg(p.Outputfilename);
            wtool.WavTool_Init(true);

            System.IO.FileStream reader = new System.IO.FileStream(p.Outputfilename, System.IO.FileMode.Open, System.IO.FileAccess.Read,System.IO.FileShare.ReadWrite);
            reader.Seek(44, System.IO.SeekOrigin.Begin);
            Thread th = new Thread(new ParameterizedThreadStart(StartFill));
            th.Start(new object[2]{wtool,reader});
            for (int w = 0; w < 5000; w++)
            {
                ArgsParser.printArgs(p);
                wtool.WavTool_Append(p);
                Console.WriteLine("===:" + wtool.LengthPieces.Count.ToString());
            }
            wtool.WavTool_Close();
            return;
        }
    }
}
