using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VocalUtau.WavTools.Model.Args;
using VocalUtau.WavTools.Model.Player;

namespace VocalUtau.WavTools.Debug
{
    class Debug_Main
    {
        public static void Main(string[] args)
        {
            ArgsStruct p=ArgsParser.parseArgs(args);
            if (p == null)
            {
                ArgsParser.printUsage();
                return;
            }
            
            Debug_WavTool wtool = new Debug_WavTool(p.Outputfilename,false);
            wtool.WavTool_Init(false);
                ArgsParser.printArgs(p);
                wtool.WavTool_Append(p);
            wtool.WavTool_Close();
            return;
        }
    }
}
