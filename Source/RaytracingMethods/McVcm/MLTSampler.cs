using GraphicGlobal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingMethods.McVcm
{
    //Liefert die Zufallszahlen für ein einzelnen Subpfad oder für das DirectLighting
    //Am Anfang lege ich fest, ob er mir Large- oder SmallStep-Zahlen erzeugen soll
    //Man kann von Außen die SmallStepSize während der Laufzeit ändern
    [Serializable]
    internal class MLTSampler : IRandom
    {
        [Serializable]
        internal class PrimarySample
        {
            public double Value; //Wert von diesen Element (Hier wird die probe-Zufallszahl gepseichert)
            public int LastModificationIteration = -1; //Bei dieser Iteration erfolgte der letzte Zugriff
            public double ValueBackup; //Hier steht der zuletzt akzeptiere Wert
            public int ModifyBackup; //Sicherungswert für die Iteration

            public void Backup()
            {
                ValueBackup = Value;
                ModifyBackup = LastModificationIteration;
            }

            public void Restore()
            {
                Value = ValueBackup;
                LastModificationIteration = ModifyBackup;
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        private readonly IRandom rand; //Erzeugt gleichmäßige Zufallszahlen
        private readonly List<PrimarySample> X = new List<PrimarySample>();//Menge aller Zufallszahlen vom zuletzt akzeptierten Fullpfad als auch die Zahlen vom aktuell zu erstellenden probe-Fullpfad
        private int currentIteration = -1;//Anzahl der Accepted-Werte
        private bool largeStep = true; //Soll bei der aktuellen Iteration ein LargeStep gemacht werden?
        private int lastLargeStepIteration = 0;//Bei welcher Iteration erfolgte zuletzt ein LargeStep? Iteration 0 ist immer ein LargeStep da er ja initial erstmal ein Pfad erzeugen muss
        private int sampleIndex;//Die wie vielte Zufallszahl innerhalb des Streams(Subpfadsamplers) ist das?

        //Adaptivity (Anpassen der SmallStepSize so dass die Accept-Pdf 23% entspricht)
        private bool adaptivity;
        private float goalAcceptanceSmall = 0.234f;
        private int totalSmallMutations = 0; //Zählt wie viele Small-Steps es gab
        private int totalLargeMutations = 0; //Zählt wie viele Large-Steps es gab
        private int acceptedSmallMutations = 0;//Zählt wie viele Accepted-Small-Steps es gab
        private int acceptedLargeMutations = 0;//Zählt wie viele Accepted-Large-Steps es gab
        private int mutationUpdates = 1; //Zählt wie oft die SmallStepSize angepasst wurde

        //SmallStep-Size
        private double logRatio;
        private float smallMutationSize;

        public MLTSampler(IRandom rand, bool adaptivity)
        {
            this.rand = rand;
            this.adaptivity = adaptivity;

            float s1 = 1.0f / 1024.0f;
            float s2 = 1.0f / 64.0f;

            this.logRatio = -Math.Log(s2 / s1);
            this.smallMutationSize = s2;
        }

        #region IRandom
        public int Next()
        {
            return (int)(int.MaxValue * Get1D());
        }

        public int Next(int minValue, int maxValue)
        {
            return minValue + Next(maxValue - minValue);
        }

        public int Next(int maxValue)
        {
            return Math.Min((int)(Get1D() * maxValue), maxValue - 1);
        }

        public void NextBytes(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)Next(256);
        }

        public double NextDouble()
        {
            return Get1D();
        }

        public string ToBase64String()
        {
            return ObjectToStringConverter.ConvertObjectToString(this);
        }

        public static MLTSampler CreateFromBase64String(string base64String)
        {
            return ObjectToStringConverter.ConvertStringToObject<MLTSampler>(base64String);
        }
        #endregion

        public void StartIteration(bool doLargeStep)
        {
            this.largeStep = doLargeStep;
            this.currentIteration++;
            this.sampleIndex = 0;

            //Recompute SmallStepSize
            //https://cgg.mff.cuni.cz/~sik/meb/files/supplemental.pdf Formula (5)
            if (this.adaptivity && this.totalSmallMutations > 0)
            {
                float ratio = (this.acceptedSmallMutations + this.acceptedLargeMutations) / (float)(this.totalSmallMutations + this.totalLargeMutations);
                float newSize = this.smallMutationSize + (ratio - this.goalAcceptanceSmall) / this.mutationUpdates;
                if (newSize > 0 && newSize < 1)
                {
                    this.smallMutationSize = newSize;
                    this.mutationUpdates++;
                }
            }
        }

        private double Get1D()
        {
            int index = this.sampleIndex++;

            //Vergrößere X wenn es zu klein ist
            while (index >= this.X.Count())
            {
                this.X.Add(new PrimarySample());
            }
            PrimarySample Xi = this.X[index];
            
            if (this.largeStep)
            {
                Xi.Backup();
                Xi.Value = this.rand.NextDouble(); //Wir sind in einer LargeStep-Iteration. Also erzeuge neuen gleichmäßigen Wert
            }
            else
            {
                //Wenn ein SmallStep erfolgen soll, dann braucht man eine Zahl, auf der man aufbauend pertubieren kann. 
                //Wenn Xi seit dem letzten LargeStep nicht benutzt wurde, dann tue ich hier so, als ob diese LargeStep-Nutzung erfolgte (Reset-Prüfung)
                if (Xi.LastModificationIteration < this.lastLargeStepIteration)
                {
                    Xi.Value = this.rand.NextDouble(); //Reset mit LargeStep
                    Xi.LastModificationIteration = lastLargeStepIteration;
                }

                //Hole alle vermissten SmallSteps nach
                while(Xi.LastModificationIteration + 1 < this.currentIteration)
                {
                    Xi.Value = Mutate(Xi.Value); //Reset mit SmallStep
                    Xi.LastModificationIteration++;
                }

                Xi.Backup();
                Xi.Value = Mutate(Xi.Value);
            }

            //Merke dir, wann der letzte Small- oder LargeStep erfolgte (SmallSteps werden eh erneut überschrieben)
            Xi.LastModificationIteration = currentIteration;

            return Xi.Value;
        }

        private double Mutate(double value)
        {
            double sample = this.rand.NextDouble();
            bool add;

            if (sample < 0.5)
            {
                add = true;
                sample *= 2;
            }else
            {
                add = false;
                sample = 2 * (sample - 0.5);
            }

            //https://cgg.mff.cuni.cz/~sik/meb/files/supplemental.pdf Formula (4)
            double dv = this.adaptivity ? Math.Pow(sample, (1 / this.smallMutationSize) + 1f) :
                this.smallMutationSize * Math.Exp(sample * this.logRatio);

            if (add)
            {
                value += dv;
                if (value >= 1)
                    value -= 1;
            }
            else
            {
                value -= dv;
                if (value < 0)
                    value += 1;
            }
            return value;
        }

        //Akzeptiere am Ende der Iteration die Probe-Zufallszahlen welche in X.Value stehen
        public void Accept()
        {
            if (this.largeStep)
            {
                this.lastLargeStepIteration = this.currentIteration;
                this.acceptedLargeMutations++;
                this.totalLargeMutations++;
            }else
            {
                this.acceptedSmallMutations++;
                this.totalSmallMutations++;
            }                
        }

        //Verwerfe am Ende der Iteration all die Probe-Zufallzahlen, welcher innerhalb dieser Iteration erzeugt wurden. 
        //Tue so, als ob die aktuelle Iteration niemals statt gefunden hat
        public void Reject()
        {
            if (this.largeStep)
                this.totalLargeMutations++;
            else
                this.totalSmallMutations++;

            foreach (var Xi in this.X)
            {
                if (Xi.LastModificationIteration == this.currentIteration)
                    Xi.Restore();
            }
            this.currentIteration--;
        }

        #region MakeClassTestable
        internal bool IsLargeStep => this.largeStep;
        internal PrimarySample[] GetX()
        {
            return this.X.ToArray();
        }
        #endregion

        //Zu Analysezwecken um zu schauen, dass die AcceptRatio wirklich bei 23% liegt
        public float GetAcceptRatio()
        {
            return (this.acceptedSmallMutations + this.acceptedLargeMutations) / (float)(this.totalSmallMutations + this.totalLargeMutations) * 100;
        }
    }
}
