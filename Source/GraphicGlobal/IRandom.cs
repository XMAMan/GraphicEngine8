using System;
using System.Collections.Generic;

namespace GraphicGlobal
{
    //Macht die Random-Klasse mockbar
    public interface IRandom
    {
        int Next();
        int Next(int minValue, int maxValue);
        int Next(int maxValue);
        void NextBytes(byte[] buffer);
        double NextDouble();
        string ToBase64String();
    }

    public class Rand : IRandom
    {
        private readonly Random rand;
        public Rand()
        {
            this.rand = new Random();
        }

        public Rand(int seed)
        {
            this.rand = new Random(seed);
        }

        public Rand(string base64String)
        {
            this.rand = ObjectToStringConverter.ConvertStringToObject<Random>(base64String);
        }

        public int Next()
        {
            return this.rand.Next();
        }

        public int Next(int minValue, int maxValue)
        {
            return this.rand.Next(minValue, maxValue);
        }

        public int Next(int maxValue)
        {
            return this.rand.Next( maxValue);
        }

        public void NextBytes(byte[] buffer)
        {
            this.rand.NextBytes(buffer);
        }

        public double NextDouble()
        {
            return this.rand.NextDouble();
        }

        public string ToBase64String()
        {
            return ObjectToStringConverter.ConvertObjectToString(this.rand);
        }
    }

    public class RandMock : IRandom
    {
        private readonly List<double> returnValues;
        private int index = 0;

        public RandMock(List<double> returnValues)
        {
            this.returnValues = returnValues;
        }

        private double GetNextValue()
        {
            if (this.index >= this.returnValues.Count) throw new Exception("There are no Randomnumbers anymore");
            double v = this.returnValues[this.index++];
            return v;
        }

        public int Next()
        {
            return (int)GetNextValue();
        }

        public int Next(int minValue, int maxValue)
        {
            return (int)GetNextValue();
        }

        public int Next(int maxValue)
        {
            return (int)GetNextValue();
        }

        public void NextBytes(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)GetNextValue();
        }

        public double NextDouble()
        {
            return GetNextValue();
        }

        public string ToBase64String()
        {
            throw new NotImplementedException();
        }
    }
}
