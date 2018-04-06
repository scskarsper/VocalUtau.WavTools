using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VocalUtau.WavTools.Model.Wave
{
    public class FileStreamIO
    {   
        public static int StreamRead(Stream baseStream,long Position,ref byte[] buff)
        {
            long Post = baseStream.Position;
            baseStream.Seek(Position, SeekOrigin.Begin);
            int ret = baseStream.Read(buff, 0, buff.Length);
            baseStream.Seek(Post, SeekOrigin.Begin);
            return ret;
        }
    }
}
