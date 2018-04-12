using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocalUtau.Wavtools.Render
{
    internal interface IRender
    {
        event VocalUtau.WavTools.Model.Player.BufferedPlayer.BufferEventHandler RendingStateChange;

        bool getIsRending();
        string getRendingFile();

        void StartRending(System.IO.DirectoryInfo baseTempDir, List<VocalUtau.Calculators.NoteListCalculator.NotePreRender> NList, string RendToWav = "");
        void StartRending(System.IO.DirectoryInfo baseTempDir, List<VocalUtau.Calculators.BarkerCalculator.BgmPreRender> BList, string RendToWav = "");

        void StopRending();
    }
}
