using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.Tools.ImageConvergence
{
    class CsvFile
    {
        public readonly string FileName;
        public CsvFile(string fileName)
        {
            this.FileName = fileName;
        }

        public void AddLine(Line line)
        {
            File.AppendAllLines(this.FileName, new string[] { line.ToString() });
        }

        public Line[] ReadAllLines()
        {
            return File
                .ReadAllLines(this.FileName)
                .Select(x => new Line(x))
                .ToArray();
        }

        public class Line
        {
            public UInt32 TimeToStart; //[Unit: Seconds]
            public byte Error; //[Unit: Percent: 0..100]

            public Line(UInt32 timeToStart, byte error)
            {
                if (error > 100) throw new ArgumentOutOfRangeException($"{nameof(error)} must be in range of 0..100");

                this.TimeToStart = timeToStart;
                this.Error = error;
            }

            public Line(string line)
            {
                var fields = line.Split('\t');
                this.TimeToStart = UInt32.Parse(fields[0]);
                this.Error = byte.Parse(fields[1]);
            }

            public override string ToString()
            {
                return this.TimeToStart + "\t" + this.Error;
            }
        }
    }
}
