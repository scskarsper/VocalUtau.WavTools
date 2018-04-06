using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocalUtau.Wavtools.Render
{
    class Program
    {
        static void Main(string[] args)
        {
            CachePlayer Cplayer = new CachePlayer();
            List<VocalUtau.Calculators.NoteListCalculator.NotePreRender> NList = new List<Calculators.NoteListCalculator.NotePreRender>();
            using (System.IO.FileStream ms = new System.IO.FileStream(@"D:\\temp.binary", System.IO.FileMode.Open))
            {
                //序列化操作，把内存中的东西写到硬盘中
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter fomatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter(null, new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.File));
                object obj=fomatter.Deserialize(ms);
                ms.Flush();
                NList = (List<VocalUtau.Calculators.NoteListCalculator.NotePreRender>)obj;
            }
            Cplayer.Play();
            Cplayer.StartRending(NList);
        }
    }
}
/*//100
           using (FileStream fs = new FileStream(@"D:\\test-t"+tracker.getIndex().ToString()+".bat", FileMode.Create))
           {
               using (StreamWriter sw=new StreamWriter(fs))
               {
                   sw.WriteLine("mkdir \"%temp%\\utaubk\"");
                   for (int i = 0; i < nlc.NotePreRenderList.Count; i++)
                   {
                       //"{RESAMPLEROUTPUT}", "{WAVOUTPUT}");
                       if (nlc.NotePreRenderList[i].ResamplerArg != null)
                       {
                           string resStr = String.Join(" ", nlc.NotePreRenderList[i].ResamplerArgList);
                           resStr = resStr.Replace("{RESAMPLEROUTPUT}", @"temp$$$.wav");
                           sw.WriteLine(@"D:\VocalUtau\VocalUtau.DebugExampleFiles\UTAUKernel\resampler.exe " + resStr);
                       }
                       string wavStr = String.Join(" ", nlc.NotePreRenderList[i].WavtoolArgList);
                       wavStr = wavStr.Replace("{RESAMPLEROUTPUT}", @"temp$$$.wav");
                       wavStr = wavStr.Replace("{WAVOUTPUT}", @"temp.wav");
                       sw.WriteLine(@"D:\VocalUtau\VocalUtau.DebugExampleFiles\UTAUKernel\wavtool.exe " + wavStr);
                   }
               }
           }


           //101
           using (FileStream fs = new FileStream(@"D:\\test-b" + tracker.getIndex().ToString() + ".txt", FileMode.Create))
           {
               using (StreamWriter sw = new StreamWriter(fs))
               {
                   for (int i = 0; i < nlc.NotePreRenderList.Count; i++)
                   {
                       //"{RESAMPLEROUTPUT}", "{WAVOUTPUT}");
                       if (nlc.NotePreRenderList[i].ResamplerArg != null)
                       {
                           string resStr = String.Join(" ", nlc.NotePreRenderList[i].ResamplerArgList);
                           resStr = resStr.Replace("{RESAMPLEROUTPUT}", @"temp$$$.wav");
                           sw.WriteLine(@"resampler.exe " + resStr.Replace(@"D:\VocalUtau\VocalUtau\bin\Debug\voicedb\YongQi_CVVChinese_Version2\",""));
                       }
                       string wavStr = String.Join(" ", nlc.NotePreRenderList[i].WavtoolArgList);
                       wavStr = wavStr.Replace("{RESAMPLEROUTPUT}", @"temp$$$.wav");
                       wavStr = wavStr.Replace("{WAVOUTPUT}", @"temp.wav");
                       sw.WriteLine(@"wavtool.exe " + wavStr.Replace(@"D:\VocalUtau\VocalUtau\bin\Debug\voicedb\YongQi_CVVChinese_Version2\", ""));
                   }
               }
           }*/