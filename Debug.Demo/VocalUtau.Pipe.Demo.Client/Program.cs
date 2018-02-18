using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VocalUtau.Pipe.Demo.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string Serbig = "";
            System.IO.FileInfo fi=new System.IO.FileInfo(Application.ExecutablePath);
            string UName=fi.Name;
            Serbig = UName;
            if (UName.IndexOf("wavtool", StringComparison.CurrentCultureIgnoreCase)!=-1)
            {
                Serbig = "WavTool";
            }
            else if (UName.IndexOf("resampler", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                Serbig = "Resampler";
            }
            Console.WriteLine(" --- HandleWork As {0}---", Serbig);
            Console.WriteLine(" ArgLine：{0}", String.Join(" ",args));
            NamedPipe_Client.SendRequest(Serbig+".test", args);
        }
    }
}
