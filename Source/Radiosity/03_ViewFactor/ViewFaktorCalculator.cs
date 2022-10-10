using System;
using System.Collections.Generic;
using System.Linq;
using IntersectionTests;
using System.Threading;
using System.Threading.Tasks;
using GraphicMinimal;
using GraphicGlobal;
using System.IO;
using RayTracerGlobal;
using RayObjects;

namespace Radiosity._03_ViewFactor
{
    //Berechnet für jeden Patch-Center-Punkt: Wie viel Prozent der ausgesendeten Photonen fliegt in Richtung X (X ist der ViewFaktor, der auf das Empfänger-Patch zeigt)
    class ViewFaktorCalculator
    {
        private readonly List<IPatch> patches;
        private readonly IntersectionFinder intersectionFinder;
        private readonly int hemicubeResolution;
        private readonly bool useShadowRayTest;

        private readonly VisibleMatrix visibleMatrix;
        private readonly string visibleMatrixFileName;

        public ViewFaktorCalculator(List<IPatch> patches, IntersectionFinder intersectionFinder, int hemicubeResolution, bool useShadowRayTest, string visibleMatrixFileName)
        {
            this.patches = patches;
            this.intersectionFinder = intersectionFinder;
            this.hemicubeResolution = hemicubeResolution;
            this.useShadowRayTest = useShadowRayTest;
            this.visibleMatrixFileName = visibleMatrixFileName;

            if (string.IsNullOrEmpty(visibleMatrixFileName) == false && File.Exists(visibleMatrixFileName))
            {
                var matrixFromFile = new VisibleMatrix(visibleMatrixFileName);
                if (matrixFromFile.Size == patches.Count)
                    this.visibleMatrix = matrixFromFile;
                else
                    this.visibleMatrix = new VisibleMatrix(patches.Count);
            }
            else
                this.visibleMatrix = new VisibleMatrix(patches.Count);
        }

        public void CalculateViewFaktorsWithHemicubeMethod(int threadCount, Action<string, float> progressChanged, CancellationTokenSource stopTrigger)
        {
            CalculateViewFaktors(threadCount, progressChanged, stopTrigger, AddAllViewFaktorsWithHemicube);
        }

        public void CalculateViewFaktorsWithSolidAngle(int threadCount, Action<string, float> progressChanged, CancellationTokenSource stopTrigger)
        {
            CalculateViewFaktors(threadCount, progressChanged, stopTrigger, AddAllViewFaktorsWithSolidAngle);
        }

        private void AddAllViewFaktorsWithHemicube(IPatch patch, IRandom rand)
        {
            foreach (var hemi in new Hemicube(hemicubeResolution, (float)rand.NextDouble() * 360, patch.Normal).GetDirectionsWithFaktor())
            {
                Ray ray = new Ray(patch.CenterOfGravity, hemi.Direction);
                var point = this.intersectionFinder.GetIntersectionPoint(ray, 0, patch);

                if (point != null && patch != (IPatch)point.IntersectedObject)
                {
                    var otherPatch = (IPatch)point.IntersectedObject;
                    if (otherPatch.IsLightSource) continue; //Lichtquellen dürfen nicht  beleuchtet werden
                    if (patch.IsLightSource && patch.IsInSpotDirection(otherPatch.CenterOfGravity) == false) continue; //Der andere liegt außerhalb meines Spotcutoff

                    patch.AddViewFaktor(new ViewFactor() { Patch = otherPatch, FormFactor = hemi.DeltaFormFactor * patch.SurfaceArea });
                }
            }
        }

        private void AddAllViewFaktorsWithSolidAngle(IPatch patch, IRandom rand)
        {
            foreach (var otherPatch in this.patches)
            {
                if (patch == otherPatch) continue; //Keine Beleuchtung mit sich selbst
                if (otherPatch.IsLightSource) continue; //Lichtquellen dürfen nicht  beleuchtet werden

                Ray ray = new Ray(patch.CenterOfGravity, Vector3D.Normalize(otherPatch.CenterOfGravity - patch.CenterOfGravity));

                float lambda1 = patch.Normal * ray.Direction;
                if (lambda1 <= 0.01f) continue;
                float lambda2 = otherPatch.Normal * (-ray.Direction);
                if (lambda2 <= 0.01f) continue;

                if (patch.IsLightSource && patch.IsInSpotDirection(otherPatch.CenterOfGravity) == false) continue; //Der andere liegt außerhalb meiens Spotcutoff

                if (this.useShadowRayTest) //Bei der einfachen Radiostiy-Box-Testscene kann man sich die Sichtbarkeitsprüfung sparen
                {
                    int index1 = this.patches.IndexOf(patch);
                    int index2 = this.patches.IndexOf(otherPatch);

                    if (this.visibleMatrix[index1, index2] == VisibleMatrix.VisibleValue.NotVisible) continue;
                    if (this.visibleMatrix[index1, index2] == VisibleMatrix.VisibleValue.NotSet)
                    {
                        var point = this.intersectionFinder.GetIntersectionPoint(ray, 0, patch);
                        //bool isVisible = point != null && point.IntersectedObject == otherPatch; //Wenn ich das so mache, dann trifft er nicht immer aus der Gegenrichtung. Grund: Wenn zwei Objekte eine gemeinsame Schnittkante haben und dort befindet sich ein IntersectionPoint, dann ist die Reihenfolge der Schnittpunktabfrage entscheidend. Siehe IntersectionFinderTest.GetIntersectionPoint_ShadowRayTestInBothDirections
                        bool isVisible = point != null && (point.Position - otherPatch.CenterOfGravity).Length() < MagicNumbers.DistanceForPoint2PointVisibleCheck;//Sichtbarkeitstest
                        VisibleMatrix.VisibleValue visibleTest = isVisible ? VisibleMatrix.VisibleValue.Visible : VisibleMatrix.VisibleValue.NotVisible;

                        this.visibleMatrix[index1, index2] = visibleTest;
                        //this.visibleMatrix[index2, index1] = visibleTest; //Die Matrix sortiert beim Zugriff immer index1 und index2 aufsteigend. Somit brauche ich keine zwei Einträge erstellen

                        if (visibleTest == VisibleMatrix.VisibleValue.NotVisible) continue;
                    }
                }

                float distanceSqrt = (patch.CenterOfGravity - otherPatch.CenterOfGravity).SquareLength();
                float formFactorErrorValue = (patch.SurfaceArea + otherPatch.SurfaceArea) / distanceSqrt;

                float formFactor;
                if (formFactorErrorValue > 2) //Die 2 hängt von der Anzahl der Beleuchtungsschritte und der MaxSurfaceAreaPerPatch ab. Um so mehr Beleuchtungsschritte oder um so größer die MaxSurfaceAreaPerPatch ist, um so kleiner muss diese Zahl hier sein
                {
                    formFactor = GetExactFormfactor(patch, otherPatch, rand); //Berechnet den Formfaktor genau, wenn eine Näherungslösung zu ungenau wird
                }
                else
                {
                    float geometryTerm = lambda1 * lambda2 / distanceSqrt;
                    float viewFactor = 1; //Zu wie viel Prozent ist otherPatch von patch.CenterOfGravity aus sichtbar. Diese Zahl soll später mal mit ein Rasterizer berechnet werden
                    formFactor = geometryTerm * viewFactor * patch.SurfaceArea * otherPatch.SurfaceArea; //Das ist nur eine Näherungslösung
                }

                patch.AddViewFaktor(new ViewFactor() { Patch = otherPatch, FormFactor = formFactor / (float)Math.PI });
            }
        }

        //Berechnet numerisch das Doppelintegral, wo über alle alle Punkte aus Patch1 und Patch2 gegangen wird, und jeweils der GeometryTerm für dA1 udn dA2 berechnet wird
        //Der FormFaktor, welcher hier beschrieben ist: https://de.wikipedia.org/wiki/Radiosity_(Computergrafik) enthält noch zusätzlich den VisibleTerm und / PI (Brdf)
        //Brdf gehört für mich nicht zu diesen Term, da er sich nicht direkt auf die Strecke zwischen zwei Patches bezieht sondern auf ein Patch (Pfadumknickpunkt)
        //VisibleTerm lasse ich aus Performancegründen weg
        private float GetExactFormfactor(IPatch patch1, IPatch patch2, IRandom rand)
        {
            int sampleCount = 100;
            float geometryTermSum = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                var point1 = patch1.GetRandomPointOnSurface(rand);
                var point2 = patch2.GetRandomPointOnSurface(rand);

                Vector3D direction = point2.Position - point1.Position;
                float sqrtLength = direction.SquareLength();
                direction /= (float)Math.Sqrt(sqrtLength);
                float lambda1 = patch1.Normal * direction;
                float lambda2 = patch2.Normal * (-direction);

                float geometryTermEstimation = lambda1 * lambda2 / sqrtLength * patch1.SurfaceArea * patch2.SurfaceArea;
                geometryTermSum += geometryTermEstimation;
            }

            return geometryTermSum / sampleCount;
        }

        private void CalculateViewFaktors(int threadCount, Action<string, float> progressChanged, CancellationTokenSource stopTrigger, Action<IPatch, IRandom> addAllViewFaktors)
        {
            Task[] tasks = new Task[threadCount];

            int[] progressCounter = new int[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                progressCounter[i] = 0;

                tasks[i] = Task.Factory.StartNew((object obj) =>
                {
                    TaskInputData input = obj as TaskInputData;
                    IRandom rand = new Rand(input.Index);

                    for (int j = input.Index; j < this.patches.Count; j += threadCount) //auf das j-Patch wird schreibend (Auf die ViewVaktor-Liste) zugegriffen
                    {
                        if (stopTrigger.IsCancellationRequested) break;
                        addAllViewFaktors(this.patches[j], rand);
                        float summe = this.patches[j].ViewFaktors.Sum(x => x.FormFactor);
                        progressCounter[input.Index]++;
                    }
                }, new TaskInputData()
                {
                    Index = i,
                });
            }

            do
            {
                try
                {
                    if (Task.WaitAll(tasks, 1000)) break;
                    progressChanged("Berechne Viewfaktors", progressCounter.Sum() * 100.0f / this.patches.Count);
                }
                catch (OperationCanceledException) //Stopptrigger wurde benutzt
                {
                    break;
                }
            } while (true);

            if (string.IsNullOrEmpty(this.visibleMatrixFileName) == false)
                this.visibleMatrix.WriteToFile(this.visibleMatrixFileName);
        }

        class TaskInputData
        {
            public int Index;
        }
    }
}
