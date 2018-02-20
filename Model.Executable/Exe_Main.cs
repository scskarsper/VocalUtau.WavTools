using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VocalUtau.WavTools.Model.Args;

namespace VocalUtau.WavTools.Model.Executable
{
    public class Exe_Main
    {
        public static bool SplitHeader = false;
        public static void Main(string[] args)
        {
            ArgsStruct p = ArgsParser.parseArgs(args);
            if (p == null)
            {
                ArgsParser.printUsage();
                Console.WriteLine("Options:");
                Console.WriteLine("\t--options-split:true/false\tSet If SplitHeader As AnotherFile");
                return;
            }
            ArgsParser.printArgs(p);
            if (p.Options.ContainsKey("split"))
            {
                string sph = p.Options["split"];
                bool.TryParse(sph, out SplitHeader);
            }

            Console.WriteLine("---- Work Renew ----");
            Console.WriteLine("SplitHeader:{0}", SplitHeader);

            string mainformat = p.Outputfilename;
            string headformat = p.Outputfilename;
            uint HeadLength = 0;
            long DataLength = 0;
            if (SplitHeader)
            {
                mainformat += ".dat";
                headformat += ".whd";
                HeadLength = (uint)Wave.NAudio.Extra.IOHelper.NormalPcmMono16_HeadLength;
            }
            using (System.IO.FileStream ofs = new FileStream(mainformat, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                Wave.WavAppender.AppendWork(ofs, p.Inputfilename, p.Offset, p.Length, p.Ovr, p.PV, HeadLength);
                DataLength = ofs.Length - HeadLength;
            }

            using (System.IO.FileStream hfs = new FileStream(headformat, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                byte[] br = Wave.NAudio.Extra.IOHelper.GenerateHead(Wave.NAudio.Extra.IOHelper.NormalPcmMono16_Format, (int)DataLength);
                hfs.Write(br, 0, br.Length);
            }
        }
    }
}
