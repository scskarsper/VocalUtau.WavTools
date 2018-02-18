using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools.Debug
{
    class Debug_WavTool
    {
        string outfile_wavhdr;
        string outfile_wavdat;
        string outfile;
        List<long> LengthStruct = new List<long>();
        List<long> LengthPoints = new List<long>();
        bool splitHeader = false;
        bool _isfinished = false;

        public bool IsFinished
        {
            get { return _isfinished; }
        }

        public Debug_WavTool(string Outputfilename,bool splitHeader=false)
        {
           outfile_wavhdr = Outputfilename + ".whd";
           outfile_wavdat = Outputfilename + ".dat";
           outfile = Outputfilename;
           this.splitHeader = splitHeader;
           LengthStruct = new List<long>();
           LengthPoints = new List<long>();
        }

        public void WavTool_Init(bool ForceNew=false)
        {
            if (splitHeader)
            {
                if (ForceNew || !System.IO.File.Exists(outfile_wavhdr))
                {
                    WavFile_Heads.wfh_init(outfile_wavhdr);
                }
                if (ForceNew || !System.IO.File.Exists(outfile_wavdat))
                {
                    WavFile_Datas.wfd_init(outfile_wavdat);
                }
            }
            else
            {
                if (ForceNew || !System.IO.File.Exists(outfile))
                {
                    WavFile_Heads.wfh_init(outfile);
                }
            }
            _isfinished = false;
        }

        public void WavTool_Append(ArgsStruct p)
        {
            string pfile = outfile;
            if (splitHeader)
            {
                pfile = outfile_wavdat;
            }
            int len = 0;
            len = WavFile_Datas.wfd_append(pfile, p.Inputfilename, p.Offset, p.Length, p.Ovr, p.PV);
            long Piece = len - (LengthPoints.Count == 0 ? 0 : LengthPoints[LengthPoints.Count - 1]);
            LengthStruct.Add(Piece);
            LengthPoints.Add(len);
        }
        public void WavTool_Close()
        {
            _isfinished = true;
            string pfile = outfile;
            if (splitHeader)
            {
                pfile = outfile_wavhdr;
            }
            int len = (int)(LengthPoints.Count == 0 ? 0 : LengthPoints[LengthPoints.Count - 1]);
            if (WavFile_Heads.wfh_checkIslegal(pfile))
            {
                int result = WavFile_Heads.wfh_putlength(pfile, len);
            }
        }

        public List<long> LengthPieces
        {
            get { return LengthStruct; }
        }
        public List<long> LengthTotals
        {
            get { return LengthPoints; }
        }
    }
}
