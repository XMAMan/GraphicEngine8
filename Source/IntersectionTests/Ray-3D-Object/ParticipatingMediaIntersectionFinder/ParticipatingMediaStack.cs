using ParticipatingMedia.Media;
using System.Collections.Generic;
using System.Linq;

namespace IntersectionTests
{
    //Zeigt an, in welchen Medium der Strahl(Anfangspunkt des Strahls) sich gerade befindet
    class ParticipatingMediaStack
    {
        public IParticipatingMedia CurrentMedium { get; private set; }

        private readonly List<IIntersectableRayDrawingObject> borders = new List<IIntersectableRayDrawingObject>();

        private readonly IParticipatingMedia lowestPriorityMedia; //GlobalMedia

        public bool StackIsEmpty { get { return this.borders.Any() == false; } }

        public ParticipatingMediaStack(IParticipatingMedia startMedium)
        {
            this.lowestPriorityMedia = startMedium;
            UpdateCurrentMedia();
        }

        public ParticipatingMediaStack(ParticipatingMediaStack copy)
        {
            this.lowestPriorityMedia = copy.lowestPriorityMedia;
            this.borders = copy.borders.ToList();
            this.CurrentMedium = copy.CurrentMedium;
        }

        public bool IsInSomeMediaObject
        {
            get
            {
                return this.borders.Count > 0;
            }
        }

        //Ich stehe am MediaBorder-Punkt und habe noch nicht CrossBorder aufgerufen. Diese Funktion gibt zurück, in welchen Medium
        //man landen würde, nachdem man CrossBorder aufgerufen hat
        public IParticipatingMedia GetNextMediaAfterCrossingBorder(IIntersectableRayDrawingObject rayHeightFromBorderPoint)
        {
            var mediaBorder = rayHeightFromBorderPoint.Media;
            bool isBorderEntryPoint = this.borders.Contains(rayHeightFromBorderPoint) == false;

            if (isBorderEntryPoint)
            {
                if (mediaBorder.Priority > this.CurrentMedium.Priority)
                    return mediaBorder;
                else
                    return this.CurrentMedium;
            }else
            {
                if (this.borders.Count == 1) return this.lowestPriorityMedia;

                //Schaue, welche Medium übrig bleibt, wenn man das mediaBorder-Objekt aus der Liste entfernt
                int maxPrio = -1;
                IParticipatingMedia maxMedia = this.lowestPriorityMedia;
                foreach (var b in this.borders)
                {
                    if (b == rayHeightFromBorderPoint) continue; //Beachte nicht den Border-Austrittspunkt, weil der ist ja dann weg
                    if (b.Media.Priority > maxPrio)
                    {
                        maxPrio = b.Media.Priority;
                        maxMedia = b.Media;
                    }
                }
                return maxMedia;
            }
        }

        //Wird aufgerufen, wenn der Strahl eine Medium-Grenze übertritt oder gebrochen wird
        public void CrossBorder(IIntersectableRayDrawingObject rayHeigh)
        {
            if (this.borders.Contains(rayHeigh) == false)
            {
                EnterMedium(rayHeigh);
            }
            else
            {
                LeaveMedium(rayHeigh);
            }
        }

        private void EnterMedium(IIntersectableRayDrawingObject rayHeigh)
        {
            this.borders.Add(rayHeigh);
            UpdateCurrentMedia();
        }

        private void LeaveMedium(IIntersectableRayDrawingObject rayHeigh)
        {
            //Manchmal findet er nicht den zweiten Schnittpunkt von ein Wolkenwürfel. Hiermit sorge ich beim Verlassen der Atmosphärenkugel, dass er dann im Vacuum rauskommt
            //Wenn diese Zeile aber drin ist, dann springt bei der Glas-Wasser-Szene, wo das Wasser das Glas überlappt (Wasser hat niedrigere Prio) der Strahl von rechten Rand/linke Glaswand zum Wasser dann nicht ins Wasser sondern ins Vacuum
            //while (rayHeigh == borders[0] && borders.Last() != rayHeigh) borders.RemoveAt(borders.Count - 1); 
            this.borders.Remove(rayHeigh);
            UpdateCurrentMedia();
        }

        private void UpdateCurrentMedia()
        {
            if (this.borders.Any() == false)
            {
                this.CurrentMedium = this.lowestPriorityMedia;
                return;
            }

            int maxPrio = -1;
            foreach (var b in this.borders)
            {
                if (b.Media.Priority > maxPrio)
                {
                    maxPrio = b.Media.Priority;
                    this.CurrentMedium = b.Media;
                }
            }
        }
    }
}
