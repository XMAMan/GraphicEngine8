using System.Collections.Generic;
using System.Linq;

namespace RaytracingRandom
{
    public interface IRussiaRolleteValue
    {
        float Weight { get; }      //Wird zum ausrechnen der Sample-Pdf benötigt
        float RunningWeight { get; }    //Wird zum sampeln benötig
    }

    public class RussiaResult<T>
    {
        public T ResultValue;
        public float Pmf; //Auswahlwahrscheinlichkeit
    }

    //Samplet eine CDS
    public class RussiaRollete<T> where T : IRussiaRolleteValue
    {
        public List<T> Values { get; private set; }

        private readonly float lastWeight;

        public RussiaRollete(List<T> values)
        {
            this.Values = values;
            this.lastWeight = this.Values.Last().RunningWeight;
        }

        public RussiaResult<T> GetSample(double u1)
        {
            float r = (float)(u1 * this.lastWeight);
            foreach (var v in this.Values)
            {
                if (v.RunningWeight > r) return new RussiaResult<T>() { ResultValue = v, Pmf = v.Weight / this.lastWeight };
            }

            return new RussiaResult<T>() { ResultValue = this.Values.Last(), Pmf = this.Values.Last().Weight / this.lastWeight };
        }

        public float Pmf(T value)
        {
            return value.Weight / this.lastWeight;
        }
    }
}
