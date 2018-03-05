using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools.Model.Args
{
    public class ArgsParser : MarshalByRefObject
    {
        public static void printUsage()
        {
            Console.WriteLine("wavtoolSharp.exe [Options/Commands] <outfile> <infile> offset length");
            Console.WriteLine("             p1 p2 p3 v1 v2 v3 v4 overlap [p4] [p5] [v4] [v5]");
            Console.WriteLine("             [p6] [v6] [p7] [v7] [p8] [v8] ... ");
        }
        public static void printExtendUsage()
        {
            Console.WriteLine("OptionsFormat:  --options-<optionName>:<optionName>");
            Console.WriteLine("             Forexample: --options-pipeName:10086");
            Console.WriteLine("CommandsFormat: --command-<commandName>");
            Console.WriteLine("             Forexample: --command-exit");
            Console.WriteLine("             Special:The Options or Commands must before <outfile>,CaseSensitive.");
        }

        public static double parseLength(string lenstr)
        {
            double duration, tempo, plustime;
            string curstr = lenstr;
            double ret = 0.0;

            string[] AtSpt = lenstr.Split('@');
            if (AtSpt.Length > 0)
            {
                if (AtSpt[0].IndexOfAny(new char[] { '+', '-' }) > 0)
                {
                    return -1.0;
                }
                else
                {
                    duration = Conversion.Val(AtSpt[0]);
                    curstr = AtSpt[1];
                }
            }
            else
            {
                ret = Conversion.Val(lenstr);
                return ret > 0 ? ret : 0.0;
            }
            
            int indexofPlus = curstr.IndexOfAny(new char[] { '+', '-' });
            if (indexofPlus == -1)
            {
                plustime = 0;
                tempo = Conversion.Val(curstr);
            }
            else
            {
                string s1 = curstr.Substring(0,indexofPlus);
                string s2 = curstr.Substring(indexofPlus);
                tempo = Conversion.Val(s1);
                plustime = Conversion.Val(s2);
            }

            if (tempo != 0.0)
            {
                ret = (1000.0 * (60.0 / tempo)) * duration / 480.0 + plustime;
            }
            else
            {
                ret = plustime;
            }
            if (ret < 0.0)
            {
                ret = 0.0;
            }
            return ret;
        }

        public static ArgsStruct parseArgs(string[] args, bool OptionOrCommandCaseSensitive=true)
        {
            Dictionary<string, string> Options = new Dictionary<string, string>(); 
            List<string> Commands = new List<string>();
            List<string> argList = new List<string>();
            argList.AddRange(args);
            if (argList.Count == 0) return null;
            while (argList.Count > 0 && (argList[0].Length > 10 && (argList[0].Substring(0, 10).ToLower() == "--options-" || argList[0].Substring(0, 10).ToLower() == "--command-")))
            {
                string ostr = argList[0];
                argList.RemoveAt(0);
                string tyr = ostr.Substring(0, 10).ToLower();
                ostr = ostr.Substring(10);
                if (tyr == "--options-")
                {
                    int spt = ostr.IndexOf(":");
                    string k = ostr;
                    string v = "true";
                    if (spt > 0)
                    {
                        k = ostr.Substring(0, spt);
                        v = ostr.Substring(spt + 1);
                    }
                    if (!OptionOrCommandCaseSensitive) k=k.ToLower();
                    if (!Options.ContainsKey(k)) Options.Add(k, v);
                }
                else if (tyr == "--command-")
                {
                    if (!OptionOrCommandCaseSensitive) ostr = ostr.ToLower();
                    Commands.Add(ostr);
                }
            }
            ArgsStruct ret=parseArgs_wavtool(argList.ToArray());
            ret.Options=Options;
            ret.Commands = Commands;
            return ret;
        }
        private static ArgsStruct parseArgs_wavtool(string[] args)
        {
            /*
            Console.WriteLine("wavtool.net <outfile> <infile> offset length");
            Console.WriteLine("             p1 p2 p3 v1 v2 v3 v4 ovr p4 p5 v5");
            Console.WriteLine("             <extend args>");
             */
            if (args.Length < 2)return null;
            ArgsStruct ret = new ArgsStruct();

            ret.Outputfilename = args[0];
            if(System.IO.File.Exists(args[1])) ret.Inputfilename = args[1];
            if (args.Length > 2) ret.Offset = Conversion.Val(args[2]); else return ret;
            if (args.Length > 3) ret.Length = parseLength(args[3]); else return ret;

            double v4 = 0.0;
            if (args.Length > 10)
            {
                ret.PV.Add(new KeyValuePair<double, double>(Conversion.Val(args[4]), Conversion.Val(args[7])));//p1,v1
                ret.PV.Add(new KeyValuePair<double, double>(Conversion.Val(args[5]), Conversion.Val(args[8])));//p2,v2
                ret.PV.Add(new KeyValuePair<double, double>(Conversion.Val(args[6]), Conversion.Val(args[9])));//p3,v3
                v4 = Conversion.Val(args[10]);
            }
            else
            {
                ret.PV.Add(new KeyValuePair<double, double>(0, 0));
                ret.PV.Add(new KeyValuePair<double, double>(5, 100));
                ret.PV.Add(new KeyValuePair<double, double>(35, 100));
                ret.PV.Add(new KeyValuePair<double, double>(0, 0));
                return ret;
            }
            if (args.Length > 11)
            {
                ret.Ovr = Conversion.Val(args[11]);
            } else return ret;

            if (args.Length > 12)
            {
                double p4 = Conversion.Val(args[12]);
                ret.PV.Add(new KeyValuePair<double,double>(p4, v4));//p4,v4
            }
            else
            {
                ret.PV.Add(new KeyValuePair<double, double>(0, v4));//0,v4
                return ret;
            }
            if (args.Length > 14)
            {
                ret.PV.Add(new KeyValuePair<double, double>(Conversion.Val(args[13]), Conversion.Val(args[14])));//p3,v3
            }
            else return ret;
            int aft = 2;
            while (args.Length > 14 + aft)
            {
                ret.PV.Add(new KeyValuePair<double, double>(Conversion.Val(args[14+aft-1]), Conversion.Val(args[14+aft])));//p3,v3
                aft += 2;
            }
            return ret;
        }

        public static void printArgs(ArgsStruct p) {
            Console.WriteLine("Output: {0}", p.Outputfilename);
            Console.WriteLine("Input: {0}", p.Inputfilename);
            Console.WriteLine("Offset: {0}", p.Offset);
            Console.WriteLine("Length: {0}, Ovr: {1}", p.Length,p.Ovr);
            int d=1;
            //PS+CP[0]+CP[1]+CP[4]... -- CP[2]+CP[3]+PE;
            foreach (KeyValuePair<double, double> kv in p.PV)
            {
                string SeekStr = "Format:";
                if (d == 1)
                {
                    SeekStr = @"Point" + d.ToString() + ".X:{START}+" + kv.Key.ToString();
                }
                else if (d == 2)
                {
                    SeekStr = @"Point" + d.ToString() + ".X:{Point1.X}+" + kv.Key.ToString();
                }
                else if (d == 3)
                {
                    SeekStr = @"Point" + d.ToString() + ".X:{Point4.X}-" + kv.Key.ToString();
                }
                else if (d == 4)
                {
                    SeekStr = @"Point"+d.ToString()+".X:{END}-" + kv.Key.ToString();
                }
                else if (d == 5)
                {
                    SeekStr = @"Point" + d.ToString() + ".X:{Point2.X}+" + kv.Key.ToString();
                }
                else
                {
                    SeekStr = @"Point" + d.ToString() + ".X:{Point"+(d-1).ToString()+".X}+" + kv.Key.ToString();
                }
                Console.WriteLine("EnvPoint{0}: ({1},{2})\t{3}",d, kv.Key,kv.Value,SeekStr);
                d++;
            }
        }
    }
}
