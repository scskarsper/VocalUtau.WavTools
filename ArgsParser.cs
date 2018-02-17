using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools
{
    class ArgsParser
    {
        public static void printUsage()
        {
            Console.WriteLine("wavtool.net <outfile> <infile> offset length");
            Console.WriteLine("             p1 p2 p3 v1 v2 v3 v4 ovr p4 p5 v4 v5");
            Console.WriteLine("             <extend args>");
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

        public static ArgsStruct parseArgs(string[] args)
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
            return ret;
        }

        public static void printArgs(ArgsStruct p) {
            Console.WriteLine("Output: {0}", p.Outputfilename);
            Console.WriteLine("Input: {0}", p.Inputfilename);
            Console.WriteLine("Offset: {0}", p.Offset);
            Console.WriteLine("Length: {0}, Ovr: {1}", p.Length,p.Ovr);
            int d=5;
            foreach (KeyValuePair<double, double> kv in p.PV)
            {
                Console.WriteLine("p: {0}, v: {1}", kv.Key,kv.Value);
                d--;
                if (d == 0) break;
            }
        }
    }
}
