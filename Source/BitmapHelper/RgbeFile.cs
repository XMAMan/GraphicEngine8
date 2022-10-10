using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.IO;

namespace BitmapHelper
{
    //Dateiendung von Rgbe-Files: .hdr
    //http://www.graphics.cornell.edu/~bjw/rgbe.html
    public class RgbeFile
    {
        public Header HeaderField { get; private set; }
        public Rgbe[,] Data { get; private set; }
        #region Read from File

        public RgbeFile(ImageBuffer imageBuffer)
        {
            this.HeaderField = new Header(imageBuffer.Width, imageBuffer.Height);
            this.Data = new Rgbe[this.HeaderField.ImageWidth, this.HeaderField.ImageHeight];
            for (int y = 0; y < this.HeaderField.ImageHeight; y++)
                for (int x = 0; x < this.HeaderField.ImageWidth; x++)
                {
                    this.Data[x, y] = new Rgbe(imageBuffer[x, y]);
                }
        }


        public RgbeFile(string fileName)
        {
            byte[] compress = EncodeWithRunLength(new byte[] { 1, 2, 2, 2, 3, 4, 5, 5 });

            BinaryReader stream = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read));

            this.HeaderField = new RgbeFile.Header(stream);

            this.Data = new Rgbe[this.HeaderField.ImageWidth, this.HeaderField.ImageHeight];

            //run length encoding is not allowed so read flat
            if (this.HeaderField.ImageWidth < 8 || this.HeaderField.ImageWidth > 0x7FFF)
            {
                this.Data = ReadDataFlat(stream, this.HeaderField.ImageWidth, this.HeaderField.ImageHeight);
                return;
            }

            byte[] first4Bytes = stream.ReadBytes(4);
            stream.BaseStream.Position -= 4;

            if ((first4Bytes[0] != 2) || (first4Bytes[1] != 2) || (first4Bytes[2] & 0x80) != 0)
            {
                //this file is not run length encoded                
                this.Data = ReadDataFlat(stream, this.HeaderField.ImageWidth, this.HeaderField.ImageHeight);
            }
            else
            {
                //this file is run length encoded
                this.Data = ReadDataRunLengthEncoded(stream, this.HeaderField.ImageWidth, this.HeaderField.ImageHeight);
            }

            stream.Close();
        }

        private Rgbe[,] ReadDataFlat(BinaryReader stream, int width, int height)
        {
            Rgbe[,] data = new Rgbe[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    data[x, y] = ReadPixel(stream);
            return data;
        }


        private Rgbe[,] ReadDataRunLengthEncoded(BinaryReader stream, int width, int height)
        {
            Rgbe[,] data = new Rgbe[width, height];

            for (int y = 0; y < height; y++)
            {
                byte[] first4Bytes = stream.ReadBytes(4);
                int scanWidth = ((first4Bytes[2] << 8) | first4Bytes[3]);
                if (scanWidth != width) throw new Exception("wrong scanline width");

                //read each of the four channels for the scanline into the buffer
                byte[] r = ReadRunlengthEncodedBytes(stream, width);
                byte[] g = ReadRunlengthEncodedBytes(stream, width);
                byte[] b = ReadRunlengthEncodedBytes(stream, width);
                byte[] e = ReadRunlengthEncodedBytes(stream, width);
                for (int x = 0; x < width; x++)
                {
                    data[x, y] = new Rgbe(r[x], g[x], b[x], e[x]);
                }
            }

            return data;
        }


        //Decodiert eine Run-Length kodierte Datei in ein unkomprimiertes byte-Array
        private byte[] ReadRunlengthEncodedBytes(BinaryReader stream, int maxBytesToRead)
        {
            byte[] data = new byte[maxBytesToRead];

            int x = 0;
            while (x < maxBytesToRead)
            {
                byte[] twoBytes = stream.ReadBytes(2);

                if (twoBytes[0] > 128)
                {
                    //a run of the same value
                    int count = twoBytes[0] - 128;

                    if (count == 0 || x + count > maxBytesToRead) throw new Exception("bad scanline data");
                    while (count-- > 0) data[x++] = twoBytes[1];
                }
                else
                {
                    //a non-run
                    int count = twoBytes[0];

                    if (count == 0 || x + count > maxBytesToRead) throw new Exception("bad scanline data");
                    data[x++] = twoBytes[1];
                    while (count-- > 1)
                    {
                        data[x++] = stream.ReadByte();
                    }
                }
            }

            return data;
        }

        private byte[] EncodeWithRunLength(byte[] data)
        {
            int minRunLength = 2;

            List<byte> output = new List<byte>();

            int cur, beg_run, run_count, old_run_count, nonrun_count;
            cur = 0;
            while (cur < data.Length)
            {
                beg_run = cur;

                //find next run of length at least 4 if one exists
                run_count = old_run_count = 0;
                while ((run_count < minRunLength) && (beg_run < data.Length))
                {
                    beg_run += run_count;
                    old_run_count = run_count;
                    run_count = 1;
                    while ((beg_run + run_count < data.Length) && (run_count < 127)
                           && (data[beg_run] == data[beg_run + run_count]))
                        run_count++;
                }

                // if data before next big run is a short run then write it as such
                if ((old_run_count > 1) && (old_run_count == beg_run - cur))
                {
                    output.Add((byte)(128 + old_run_count)); //write short run
                    output.Add(data[cur]);
                    cur = beg_run;
                }

                //write out bytes until we reach the start of the next run
                while (cur < beg_run)
                {
                    nonrun_count = beg_run - cur;
                    if (nonrun_count > 128)
                        nonrun_count = 128;
                    output.Add((byte)nonrun_count);
                    for (int i = 0; i < nonrun_count; i++)
                        output.Add(data[cur + i]);
                    cur += nonrun_count;
                }

                //write out next run if one was found
                if (run_count >= minRunLength)
                {
                    output.Add((byte)(128 + run_count));
                    output.Add(data[beg_run]);
                    cur += run_count;
                }
            }

            return output.ToArray();
        }

        private Rgbe ReadPixel(BinaryReader stream)
        {
            return new Rgbe(stream.ReadByte(), stream.ReadByte(), stream.ReadByte(), stream.ReadByte());
        }
        #endregion

        public void WriteToFile(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);
            FileStream writerStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.None);
            BinaryWriter writer = new BinaryWriter(writerStream);

            this.HeaderField.WriteToStream(writer);

            if (this.HeaderField.ImageWidth < 8 || this.HeaderField.ImageWidth > 0x7fff)
            {
                //run length encoding is not allowed so write flat
                for (int y = 0; y < this.HeaderField.ImageHeight; y++)
                    for (int x = 0; x < this.HeaderField.ImageWidth; x++)
                    {
                        writer.Write(this.Data[x, y].As4Bytes);
                    }
            }
            else
            {
                //Write RunLength Encoded
                for (int y = 0; y < this.HeaderField.ImageHeight; y++)
                {
                    int w = this.HeaderField.ImageWidth;
                    byte[] first4Bytes = new byte[] { 2, 2, (byte)((w >> 8) & 0xFF), (byte)(w & 0xFF) };
                    writer.Write(first4Bytes);

                    byte[] r = new byte[w];
                    byte[] g = new byte[w];
                    byte[] b = new byte[w];
                    byte[] e = new byte[w];
                    for (int x = 0; x < w; x++)
                    {
                        r[x] = this.Data[x, y].RMantissa;
                        g[x] = this.Data[x, y].GMantissa;
                        b[x] = this.Data[x, y].BMantissa;
                        e[x] = this.Data[x, y].Exponent;
                    }

                    writer.Write(EncodeWithRunLength(r));
                    writer.Write(EncodeWithRunLength(g));
                    writer.Write(EncodeWithRunLength(b));
                    writer.Write(EncodeWithRunLength(e));
                }

            }

            writer.Close();
        }



        public class Header
        {
            public string Programtype { get; private set; } = "RADIANCE";
            public float Gamma { get; private set; } = 1;
            public float Exposure { get; private set; } = 1;
            public string Primaries { get; private set; } = "0 0 0 0 0 0 0 0";
            public string Format { get; private set; } = "32-bit_rle_rgbe";
            public int ImageWidth { get; private set; }
            public int ImageHeight { get; private set; }

            public Header(int width, int height)
            {
                this.ImageWidth = width;
                this.ImageHeight = height;
            }

            public void WriteToStream(BinaryWriter stream)
            {
                //Magic Header
                stream.Write((byte)0x23);
                stream.Write((byte)0x3F);

                stream.Write(StringToByteArray(this.Programtype));
                stream.Write((byte)0x0A);

                stream.Write(StringToByteArray($"GAMMA={(int)this.Gamma}"));
                stream.Write((byte)0x0A);

                stream.Write(StringToByteArray($"PRIMARIES={this.Primaries}"));
                stream.Write((byte)0x0A);

                stream.Write(StringToByteArray($"FORMAT={this.Format}"));
                stream.Write((byte)0x0A);

                stream.Write((byte)0x0A);

                stream.Write(StringToByteArray($"-Y {this.ImageHeight} +X {this.ImageWidth}"));
                stream.Write((byte)0x0A);
            }

            //Ließt den Header aus ein Binärstrom und verändert dabei die Leseposition des Stromes
            public Header(BinaryReader stream)
            {
                if (stream.ReadByte() != 0x23 || stream.ReadByte() != 0x3F) throw new Exception("File has to start with '#?'");

                this.Programtype = ByteArrayToString(ReadUpEndMarker(stream, 0x0A));

                byte[] dataLine;
                while ((dataLine = ReadUpEndMarker(stream, 0x0A)).Length > 0)
                {
                    string line = ByteArrayToString(dataLine);
                    var fields = line.Split('=');
                    switch (fields[0])
                    {
                        case "GAMMA":
                            this.Gamma = Convert.ToSingle(fields[1]); break;
                        case "PRIMARIES":
                            this.Primaries = fields[1]; break;
                        case "FORMAT":
                            this.Format = fields[1]; break;
                    }
                }

                string sizeString = ByteArrayToString(ReadUpEndMarker(stream, 0x0A));
                var fields1 = sizeString.Split(' ');
                this.ImageWidth = Convert.ToInt32(fields1[3]);
                this.ImageHeight = Convert.ToInt32(fields1[1]);
            }

            private byte[] ReadUpEndMarker(BinaryReader binaryReader, byte endmarker)
            {
                List<byte> data = new List<byte>();
                while (true)
                {
                    byte b = binaryReader.ReadByte();
                    if (b == endmarker) return data.ToArray();
                    data.Add(b);
                }
            }

            private byte[] StringToByteArray(string str)
            {
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                return enc.GetBytes(str);
            }

            private string ByteArrayToString(byte[] arr)
            {
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                return enc.GetString(arr);
            }
        }

        //Die 3 RGB-float-Zahlen werden in Mantisse und Exponent aufgesplittet. Der Exponent wird so eingestellt, dass er
        //bei allen 3 Zahlen gleich ist.
        public class Rgbe
        {
            public byte RMantissa { get; private set; }
            public byte GMantissa { get; private set; }
            public byte BMantissa { get; private set; }
            public byte Exponent { get; private set; }

            public Rgbe(byte r, byte g, byte b, byte e)
            {
                this.RMantissa = r;
                this.GMantissa = g;
                this.BMantissa = b;
                this.Exponent = e;
            }

            public byte[] As4Bytes
            {
                get
                {
                    return new byte[] { this.RMantissa, this.GMantissa, this.BMantissa, this.Exponent };
                }
            }

            /* standard conversion from float pixels to rgbe pixels */
            public Rgbe(Vector3D color)
            {
                float v;
                int e = 0;

                v = color.X;
                if (color.Y > v) v = color.Y;
                if (color.Z > v) v = color.Z;
                if (v < 1e-32f)
                {
                    this.RMantissa = this.GMantissa = this.BMantissa = this.Exponent = 0;
                }
                else
                {
                    v = (float)(MathHelper.frexp(v, ref e) * 256.0 / v);
                    this.RMantissa = (byte)(color.X * v);
                    this.GMantissa = (byte)(color.Y * v);
                    this.BMantissa = (byte)(color.Z * v);
                    this.Exponent = (byte)(e + 128);
                }
            }


            /* standard conversion from rgbe to float pixels */
            /* note: Ward uses ldexp(col+0.5,exp-(128+8)).  However we wanted pixels */
            /*       in the range [0,1] to map back into the range [0,1].            */
            public Vector3D ToRgb()
            {
                if (this.Exponent != 0)
                {
                    float f = (float)MathHelper.ldexp(1.0, this.Exponent - (int)(128 + 8));
                    return new Vector3D(this.RMantissa * f, this.GMantissa * f, this.BMantissa * f);
                }
                else
                {
                    return new Vector3D(0, 0, 0);
                }
            }
        }

        public ImageBuffer GetAsImageBuffer()
        {
            ImageBuffer buffer = new ImageBuffer(this.HeaderField.ImageWidth, this.HeaderField.ImageHeight);
            for (int y = 0; y < this.HeaderField.ImageHeight; y++)
                for (int x = 0; x < this.HeaderField.ImageWidth; x++)
                {
                    buffer[x, y] = this.Data[x, y].ToRgb();
                }
            return buffer;
        }
    }

    //https://github.com/MachineCognitis/C.math.NET
    class MathHelper
    {
        //Bit-mask used for extracting the exponent bits of a <see cref="Double"/> (<c>0x7ff0000000000000</c>).
        public const long DBL_EXP_MASK = 0x7ff0000000000000L;

        //The number of bits in the mantissa of a<see cref= "Double" />, excludes the implicit leading<c>1</c> bit (<c>52</c>).
        public const int DBL_MANT_BITS = 52;

        //Bit-mask used for clearing the exponent bits of a<see cref= "Double" /> (< c > 0x800fffffffffffff </ c >).
        public const long DBL_EXP_CLR_MASK = DBL_SGN_MASK | DBL_MANT_MASK;

        //Bit-mask used for extracting the sign bit of a <see cref="Double"/> (<c>0x8000000000000000</c>).
        public const long DBL_SGN_MASK = -1 - 0x7fffffffffffffffL;

        //Bit-mask used for extracting the mantissa bits of a <see cref="Double"/> (<c>0x000fffffffffffff</c>).
        public const long DBL_MANT_MASK = 0x000fffffffffffffL;

        //Bit-mask used for clearing the sign bit of a <see cref="Double"/> (<c>0x7fffffffffffffff</c>).
        public const long DBL_SGN_CLR_MASK = 0x7fffffffffffffffL;

        //Decomposes the given floating-point <paramref name="number"/> into a normalized fraction and an integral power of two.
        //https://machinecognitis.github.io/C.math.NET/html/bfa18b69-72e7-6b43-dab0-5f03e009b720.htm
        public static double frexp(double number, ref int exponent)
        {
            long bits = System.BitConverter.DoubleToInt64Bits(number);
            int exp = (int)((bits & MathHelper.DBL_EXP_MASK) >> MathHelper.DBL_MANT_BITS);
            exponent = 0;

            if (exp == 0x7ff || number == 0D)
                number += number;
            else
            {
                // Not zero and finite.
                exponent = exp - 1022;
                if (exp == 0)
                {
                    // Subnormal, scale number so that it is in [1, 2).
                    number *= System.BitConverter.Int64BitsToDouble(0x4350000000000000L); // 2^54
                    bits = System.BitConverter.DoubleToInt64Bits(number);
                    exp = (int)((bits & MathHelper.DBL_EXP_MASK) >> MathHelper.DBL_MANT_BITS);
                    exponent = exp - 1022 - 54;
                }
                // Set exponent to -1 so that number is in [0.5, 1).
                number = System.BitConverter.Int64BitsToDouble((bits & MathHelper.DBL_EXP_CLR_MASK) | 0x3fe0000000000000L);
            }

            return number;
        }

        //https://machinecognitis.github.io/C.math.NET/html/8615fb99-c43d-a9fc-3f64-5908f8034de8.htm#!
        public static double ldexp(double number, long exponent)
        {
            long bits = System.BitConverter.DoubleToInt64Bits(number);
            int exp = (int)((bits & MathHelper.DBL_EXP_MASK) >> MathHelper.DBL_MANT_BITS);
            // Check for infinity or NaN.
            if (exp == 0x7ff)
                return number;
            // Check for 0 or subnormal.
            if (exp == 0)
            {
                // Check for 0.
                if ((bits & MathHelper.DBL_MANT_MASK) == 0)
                    return number;
                // Subnormal, scale number so that it is in [1, 2).
                number *= System.BitConverter.Int64BitsToDouble(0x4350000000000000L); // 2^54
                bits = System.BitConverter.DoubleToInt64Bits(number);
                exp = (int)((bits & MathHelper.DBL_EXP_MASK) >> MathHelper.DBL_MANT_BITS) - 54;
            }
            // Check for underflow.
            if (exponent < -50000)
                return MathHelper.copysign(0D, number);
            // Check for overflow.
            if (exponent > 50000 || (long)exp + exponent > 0x7feL)
                return MathHelper.copysign(System.Double.PositiveInfinity, number);
            exp += (int)exponent;
            // Check for normal.
            if (exp > 0)
                return System.BitConverter.Int64BitsToDouble((bits & MathHelper.DBL_EXP_CLR_MASK) | ((long)exp << MathHelper.DBL_MANT_BITS));
            // Check for underflow.
            if (exp <= -54)
                return MathHelper.copysign(0D, number);
            // Subnormal.
            exp += 54;
            number = System.BitConverter.Int64BitsToDouble((bits & MathHelper.DBL_EXP_CLR_MASK) | ((long)exp << MathHelper.DBL_MANT_BITS));
            return number * System.BitConverter.Int64BitsToDouble(0x3c90000000000000L); // 2^-54
        }

        // Copies the sign of <paramref name="number2"/> to <paramref name="number1"/>.
        public static double copysign(double number1, double number2)
        {
            // If number1 is NaN, we have to store in it the opposite of the sign bit.
            long sign = (MathHelper.signbit(number2) == 1 ? MathHelper.DBL_SGN_MASK : 0L) ^ (System.Double.IsNaN(number1) ? MathHelper.DBL_SGN_MASK : 0L);
            return System.BitConverter.Int64BitsToDouble((System.BitConverter.DoubleToInt64Bits(number1) & MathHelper.DBL_SGN_CLR_MASK) | sign);
        }

        /// <summary>
        /// Gets the sign bit of the specified floating-point <paramref name="number"/>.
        /// </summary>
        /// <param name="number">A floating-point number.</param>
        /// <returns>The sign bit of the specified floating-point <paramref name="number"/>.</returns>

        public static int signbit(double number)
        {
            if (System.Double.IsNaN(number))
                return ((System.BitConverter.DoubleToInt64Bits(number) & MathHelper.DBL_SGN_MASK) != 0) ? 0 : 1;
            else
                return ((System.BitConverter.DoubleToInt64Bits(number) & MathHelper.DBL_SGN_MASK) != 0) ? 1 : 0;
        }
    }

}
