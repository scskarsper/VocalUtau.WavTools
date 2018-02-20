using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools.Model.Args
{
    public class ArgsStruct
    {
        public ArgsStruct()
        {
            _pv = new List<KeyValuePair<double, double>>();
        }

        public Dictionary<string, string> Options = new Dictionary<string, string>();
        public List<string> Commands = new List<string>();

        double _offset;

        public double Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }
        double _length;

        public double Length
        {
            get { return _length; }
            set { _length = value; }
        }
        List<KeyValuePair<double, double>> _pv;

        public List<KeyValuePair<double, double>> PV
        {
            get { return _pv; }
            set { _pv = value; }
        }

        double _ovr;

        public double Ovr
        {
            get { return _ovr; }
            set { _ovr = value; }
        }
        string _inputfilename;

        public string Inputfilename
        {
            get { return _inputfilename; }
            set { _inputfilename = value; }
        }
        string _outputfilename;

        public string Outputfilename
        {
            get { return _outputfilename; }
            set { _outputfilename = value; }
        }
    }
}
