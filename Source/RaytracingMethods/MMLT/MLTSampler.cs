using GraphicGlobal;
using GraphicGlobal.MathHelper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaytracingMethods.MMLT
{
    //Liefert die Zufallszahlen, um ein Fullpfad zu erzeugen. Diese Zufallszahlen werden entweder gleichmäßig
    //im 0..1-Bereich erzeugt wenn ein neuer Large-Step-Fullpfad erzeugt werden soll. Oder es werden die Zufalls-
    //zahlen vom letzten Fullpfad genommen und dann per Normalverteilung um den letzten erzeugten Wert gestreut (Small Step).
    //Bei einen small-Step kann es passieren, dass sich die Subpfadlänge ändert. Damit eine Änderung der Eyepfadlänge
    //keine unbeabsichtigte Änderung der Lightsubpfad-Zufallszahlen provoziert werden die zu erzeugenden Zufallszahlen
    //in so genannte Streams unterteilt. Es gibt 3 Streams: EyeSubpfad, LightSubpfad, FullpathSampler
    //Intern werden immer nur die Zufallszahlen vom zuletzt erzeugten Fullpfad gespeichert. Man kann Zahlen für ein
    //neuen Probe-Fullpfad erzeugen und dann entweder sagen, dass ich sie mit Accept mir merken will oder mit Reject 
    //kann ich diese Probe-Zufallszahlen verwerfen

    //Verwendung: Vor der ersten initialen Large-Step-Iteration darf kein StartIteration gerufen werden. Ansonsten muss das immer erfolgen. Siehe MLTSamplerTest
    //Dokumentation: Siehe Dokumentation.odt->"Metropolis-Light-Transport – Die MLTSampler-Klasse"
    [Serializable]
    internal class MLTSampler : IRandom
    {
        private readonly IRandom rand; //Erzeugt gleichmäßige Zufallszahlen
        private readonly float sigma;   //Streuweite von der Normalverteilung für den SmallStep
        public readonly float LargeStepProbability;//Wahrscheinlichkeit, dass ich beim nächsten zu erzeugenden Fullpfad neue Zufallszahlen verwende
        private readonly int streamCount; //streamCount = 3 (Eyesubpfad, Lightsubpfad, ISingleFullPathSampler)
        private readonly List<PrimarySample> X = new List<PrimarySample>();//Menge aller Zufallszahlen vom zuletzt akzeptierten Fullpfad als auch die Zahlen vom aktuell zu erstellenden probe-Fullpfad
        private int currentIteration = 0;//Zählt wie viele Fullpaths hiermit erstellt wurden. Dabei ist egal ob der neu erstellte Pfad akzeptiert wurde oder man immer wieder den alten verwendet.
        private bool largeStep = true; //Soll bei der aktuellen Iteration ein LargeStep gemacht werden?
        private int lastLargeStepIteration = 0;//Bei welcher Iteration erfolgte zuletzt ein LargeStep? Iteration 0 ist immer ein LargeStep da er ja initial erstmal ein Pfad erzeugen muss
        private int streamIndex;//StreamIndex → 0=EyeSubpfad, 1=LightSubpfad, 2=DirectLighting (LightSourcesampling) / Lighttracing (Kamerasampling)
        private int sampleIndex;//Die wie vielte Zufallszahl innerhalb des Streams(Subpfadsamplers) ist das?
        private static double Sqrt2 = Math.Sqrt(2);

        public MLTSampler(IRandom rand, //Erzeugt gleichmäßig verteilte Zufallszahlen
            float sigma, //sigma ist die Streuweite von der Normalverteilung für den SmallStep
            float largeStepProbability, //Wahrscheinlichkeit, dass ich beim nächsten zu erzeugenden Fullpfad neue Zufallszahlen verwende
            int streamCount) //streamCount = 3 (Eyesubpfad, Lightsubpfad, ISingleFullPathSampler)
        {
            this.rand = rand;
            this.sigma = sigma;
            this.LargeStepProbability = largeStepProbability;
            this.streamCount = streamCount;
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

        //Listenelement von X.
        //Hier werden einerseits die zuletzt akzeptieren Zufallszahlen für ein Fullpath gespeichert als auch die neuen noch nicht akzeptieren
        //probe-Zufallszahlen
        [Serializable]
        internal class PrimarySample //Wenn ich das als Struct anstatt als class mache, dann kann ich mit Xi = X[index]; Xi.Value= Kein Wert in X[] verändern
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


        //Starte mit der Erzeugung vom nächsten Fullpath
        //Dabei wird entweder ein LargeStep(neuer zufälliger Pfad) oder SmallStep(Alten Pfad mutieren) gemacht
        //Achtung: Der erste Fullpfad bei Iteration 0 muss ohne vorherigen Aufruf von StartIteration erzeugt werden! Dieser initiale Iteration-0-Pfad-ohne-StartIteration
        //wird ein LargeStep-Pfad werden, da largeStep initial mit true angelegt wird und hier bei StartIteration (Ab Iteration 1) bekommt es dann zufällig einen Wert
        public void StartIteration()
        {
            this.currentIteration++;
            if (this.currentIteration > 0) //Wenn currentIteration hier 0 ist bedeutet dass das die erste LargeStep-Iteration Rejected wurde und nun zwansweise ein neuer LargeStep kommen muss
                this.largeStep = this.rand.NextDouble() < this.LargeStepProbability;
        }

        //Erzeuge neuen Subpfad/Fullpath-Connection-Schritt
        public void StartStream(int index)
        {
            this.streamIndex = index;
            this.sampleIndex = 0;
        }

        //Erzeugt für den aktuellen Stream die nächste Zufallszahl
        private double Get1D()
        {
            int index = GetNextIndex();
            EnsureReady(index);
            return this.X[index].Value;
        }

        //Index für X ermitteln/hochzählen
        private int GetNextIndex()
        {
            return this.streamIndex + this.streamCount * this.sampleIndex++;
        }

        //Erzeugt eine Zufallszahl und speichert sie unter X[index].value ab
        private void EnsureReady(int index)
        {
            //Vergrößere X wenn es zu klein ist
            while (index >= this.X.Count())
            {
                this.X.Add(new PrimarySample());
            }
            PrimarySample Xi = this.X[index];

            //Wenn ein SmallStep erfolgen soll, dann braucht man eine Zahl, auf der man aufbauend pertubieren kann. 
            //Wenn Xi seit dem letzten LargeStep nicht benutzt wurde, dann tue ich hier so, als ob diese LargeStep-Nutzung erfolgte (Reset-Prüfung)
            //Das Reset muss vor dem Backup erfolgen, da es sonst Rejected werden kann und man somit unnötig erneut ein Reset machen müsste
            if (this.largeStep == false && Xi.LastModificationIteration < this.lastLargeStepIteration)
            {
                Xi.Value = this.rand.NextDouble(); //Reset mit LargeStep
                Xi.LastModificationIteration = lastLargeStepIteration;
            }

            Xi.Backup();
            if (this.largeStep)
            {
                Xi.Value = this.rand.NextDouble(); //Wir sind in einer LargeStep-Iteration. Also erzeuge neuen gleichmäßigen Wert
            }else
            {
                //So viele SmallSteps erfolgten seit dem letzten LargeStep
                int nSmall = this.currentIteration - Xi.LastModificationIteration;

                //Erzeugte normalverteilte Zufallszahl N(0,1) mit Sigma=1 und Mü=0(Streue um die 0)
                double normalSample = MLTSampler.Sqrt2 * MathExtensions.InverseErrorFunction(2 * this.rand.NextDouble() - 1);

                //Wichte die Zufallszahl mit Sqrt(nSmall)*Sigma um somit nSmall SmallSteps zu simulieren
                double effSigma = this.sigma * Math.Sqrt(nSmall);
                Xi.Value += normalSample * effSigma;

                //Entferne den Ganzzahlanteil um somit immer im Bereich von 0 bis 1 zu bleiben
                Xi.Value -= Math.Floor(Xi.Value);
            }

            //Merke dir, wann der letzte Small- oder LargeStep erfolgte (SmallSteps werden eh erneut überschrieben)
            Xi.LastModificationIteration = currentIteration;
        }

        //........

        //Akzeptiere am Ende der Iteration die Probe-Zufallszahlen welche in X.Value stehen
        public void Accept()
        {
            if (this.largeStep) 
                this.lastLargeStepIteration = this.currentIteration;
        }

        //Verwerfe am Ende der Iteration all die Probe-Zufallzahlen, welcher innerhalb dieser Iteration erzeugt wurden. 
        //Tue so, als ob die aktuelle Iteration niemals statt gefunden hat
        public void Reject()
        {
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
    }
}
