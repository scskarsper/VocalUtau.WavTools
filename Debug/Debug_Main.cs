using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VocalUtau.WavTools.Debug
{
    class Debug_Main
    {
        static BufferedPlayer bplayer;
        static void StartFill(object pr)
        {
            object[] obj = (object[])pr;
            Debug_WavTool wtool = (Debug_WavTool)obj[0];

            while (true)
            {
                if (wtool.LengthTotals.Count > 1)
                {
                    long Utall = wtool.LengthPieces[wtool.LengthPieces.Count - 1];
                    if (wtool.IsFinished) Utall = -1;
                    bplayer.FillPlayState(Utall);
                    bplayer.FillBuffer(Utall);
                }
                Thread.Sleep(100);
            }
        }
        public static void Main(string[] args)
        {
            ArgsStruct p=ArgsParser.parseArgs(args);
            if (p == null)
            {
                ArgsParser.printUsage();
                return;
            }
            
            Debug_WavTool wtool = new Debug_WavTool(p.Outputfilename);
            wtool.WavTool_Init(true);

            System.IO.FileStream reader = new System.IO.FileStream(p.Outputfilename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            reader.Seek(44, System.IO.SeekOrigin.Begin);
            bplayer = new BufferedPlayer(reader);
            bplayer.InitPlayer();
            bplayer.Buffer_Play();

            Thread th = new Thread(new ParameterizedThreadStart(StartFill));
            th.Start(new object[2]{wtool,reader});

            for (int w = 0; w < 500000; w++)
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
