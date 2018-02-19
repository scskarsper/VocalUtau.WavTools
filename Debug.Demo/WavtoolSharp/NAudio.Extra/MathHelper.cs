using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WavtoolSharp.NAudio.Extra
{
    class MathHelper
    {
        public float floatAverage(float[] frames)
        {
            if (frames.Length == 0) return 0;
            double total = 0;
            foreach (float f in frames)
            {
                total += f;
            }
            return (float)(total / frames.Length);
        }
        public struct SegmentPoint
        {
            double X;
            double Y;
        }
        public class SegmentLine
        {
            SegmentPoint A;
            SegmentPoint B;
            public SegmentLine(SegmentPoint A, SegmentPoint B)
            {
                this.A = A;
                this.B = B;
            }
        }
    }
}
