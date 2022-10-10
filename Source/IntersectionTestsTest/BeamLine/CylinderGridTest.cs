using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using IntersectionTests.BeamLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntersectionTestsTest.BeamLine
{
    [TestClass]
    public class CylinderGridTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        //Durch diesen Test konnte ich rausfinden, dass die Zeile 284 aus PhotonBeam.hxx falsch ist -> float oT2 = (oT1 + d1O1 - d1O2) / d1d2;
        [TestMethod]
        public void GetIntersectionPointBetweenSingleLineAndBeam_DistanceIsNotGreaterThanBeamRadius()
        {
            QueryLine line = new QueryLine(new Ray(new Vector3D(0f, -1.89999998f, 0f), new Vector3D(-0.133565679f, 0.952100635f, 0.275072038f)), 3.04589653f);
            CylinderMock beam = new CylinderMock(new Ray(new Vector3D(-0.197390556f, 0.120746851f, 1f), new Vector3D(-0.226602837f, 0.240866244f, -0.943734407f)), 2.11924028f, 0.030f);
            var point = LineBeamIntersectionHelper.GetLineBeamIntersectionPoint(line, beam);

            Vector3D p1 = line.Ray.Start + line.Ray.Direction * point.LineIntersectionPosition;
            Vector3D p2 = beam.Ray.Start + beam.Ray.Direction * point.BeamIntersectionPosition;
            float distance = (p1 - p2).Length();
            float maxAllowedDistance = beam.Radius;
            float distanceFromPoint = point.Distance;
            
            Assert.IsTrue(distance < maxAllowedDistance);
            Assert.IsTrue(Math.Abs(distance - distanceFromPoint) < 0.0001f);
        }

        [TestMethod] //Prüfe, dass jede Voxelcelle beim traversieren auch wirklich besucht wird
        public void GetAllIntersectionPoints_CalledFor2DGrid_EachExpectedVoxelIsEntered()
        {
            int gridSize = 32;
            int sampleCount = 10000;
            Voxel2DGrid grid = new Voxel2DGrid(gridSize);

            Random rand = new Random(0);

            for (int i=0;i< sampleCount;i++)
            {                
                float x1 = (float)rand.NextDouble() * 3 - 1;
                float y1 = (float)rand.NextDouble() * 3 - 1;
                float x2 = (float)rand.NextDouble() * 3 - 1;
                float y2 = (float)rand.NextDouble() * 3 - 1;
                var line = new QueryLine(grid.VoxelSize / 2, x1, y1, x2, y2);
                //var line = new QueryLine(grid.VoxelSize / 2, -0.01f, 0.25f, 0.8f, 1.1f);

                string expected = grid.GetExpectedString(line);
                string actual = grid.GetActualString(line);

                if (expected != actual)
                {
                    Bitmap resultImage = BitmapHelper.BitmapHelp.TransformBitmapListToRow(new List<Bitmap>() { grid.GetActualImage(line), grid.GetExpectedImage(line) });
                    resultImage.Save(WorkingDirectory + "2DBeamGrid.bmp");
                }

                Assert.AreEqual(expected, actual, $"i={i}");
            }
        }

        [TestMethod] //Prüfe, dass jede Voxelcelle beim malen eines Fill-Rechtangles besucht wird
        public void AddRectangleToGrid_CalledFor2DGrid_EachExpectedVoxelIsEntered()
        {
            int gridSize = 32;
            Voxel2DGrid grid = new Voxel2DGrid(gridSize);

            GridRectangle rec = new GridRectangle(new Vector3D(0.3f, 0.3f, grid.VoxelSize / 2), new Vector3D(0.002f * 2, 0.005f * 2, 0), new Vector3D(0.5f, 0.2f, 0));
            grid.CylinderGrid.AddRectangleToGrid(rec.Pos, rec.V1, rec.V2);

            grid.Create2DGridImageWithRectangle(rec).Save(WorkingDirectory + "2DBeamGrid.bmp");
        }

        
        [TestMethod]//Prüfe Schnittpunktabfrage gegen Menge von Cylindern
        public void GetAllIntersectionPoints_CalledFor3DGrid_EachExpectedIntersectionPointIsFound()
        {
            int gridSize = 64;
            int sampleCount = 9000;

            Random rand = new Random(0);
            var cylinderList = GetRandomCylinderList(rand, 100).Cast<IIntersectableCylinder>().ToList();
            CylinderGrid grid = new CylinderGrid(cylinderList, gridSize);

            //System.Diagnostics.Debugger.Launch();

            DateTime startTime = DateTime.Now;
            for (int i=0;i<sampleCount;i++)
            {
                Vector3D start = new Vector3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()) * 3 - new Vector3D(1, 1, 1);
                Vector3D direction = Vector3D.Normalize(new Vector3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()));
                QueryLine line = new QueryLine(new Ray(start, direction));
                var actual = grid.GetAllIntersectionPoints(line);               //Grid-Suche
                var expected = GetAllIntersectionPoints(cylinderList, line);    //Lineare Suche

                Assert.AreEqual(GetIndexListString(cylinderList, expected), GetIndexListString(cylinderList, actual));
            }
            double time = (DateTime.Now - startTime).TotalMilliseconds;
        }
        //Vergleich der Abfragezeiten zwischen Grid und linearer Suche
//100 Beams, gridSize = 256
//time = 555.0189			Ohne Grid
//time = 989.0331000000001	Mit Grid

//1000 Beams, gridSize = 256
//time = 9711.3115			Ohne Grid
//time = 2394.07651			Mit Grid

//100 Beams, gridSize = 64
//time = 471.0054			Ohne Grid
//time = 466.0493			Mit Grid

//1000 Beams, gridSize = 64
//time = 5341.171			Ohne Grid
//time = 602.0189			Mit Grid

//1000 Beams, gridSize = 128
//time = 16969.6459			Ohne Grid
//time = 803.0298			Mit Grid

        private string GetIndexListString(List<IIntersectableCylinder> cylinders, List<LineBeamIntersectionPoint> points)
        {
            List<int> indizes = new List<int>();
            foreach (var p in points)
            {
                indizes.Add(cylinders.IndexOf(p.IntersectedBeam));
            }
            indizes.Sort();
            return string.Join(",", indizes);
        }

        private List<LineBeamIntersectionPoint> GetAllIntersectionPoints(List<IIntersectableCylinder> cylinders, IQueryLine line)
        {
            List<LineBeamIntersectionPoint> points = new List<LineBeamIntersectionPoint>();
            foreach (var c in cylinders)
            {
                var p = c.GetIntersectionPoint(line);
                if (p != null) points.Add(p);
            }
            return points;
        }

        private List<CylinderMock> GetRandomCylinderList(Random rand, int count)
        {
            List<CylinderMock> cylinders = new List<CylinderMock>();

            for (int i=0;i<count;i++)
            {
                Vector3D start = new Vector3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                Vector3D direction = Vector3D.Normalize(new Vector3D((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()));
                float length = (float)rand.NextDouble();
                float radius = (float)rand.NextDouble() * 0.1f;
                cylinders.Add(new CylinderMock(new Ray(start, direction), length, radius));
            }

            return cylinders;
        }
    }

    class GridRectangle
    {
        public Vector3D Pos { get; private set; }
        public Vector3D V1 { get; private set; }
        public Vector3D V2 { get; private set; }
        public GridRectangle(Vector3D pos, Vector3D v1, Vector3D v2)
        {
            this.Pos = pos;
            this.V1 = v1;
            this.V2 = v2;
        }
    }


    class CylinderMock : IIntersectableCylinder
    {
        public Ray Ray { get; private set; } //Startpunkt + Richtung des Cylinders
        public float Length { get; private set; }
        public float Radius { get; private set; }
        public float RadiusSqrt { get; private set; }

        public BoundingBox GetAxialAlignedBoundingBox()
        {
            return GetNonAlignedBoundingBox().GetAxialAlignedBoundingBox();
        }
        public NonAlignedBoundingBox GetNonAlignedBoundingBox()
        {
            Vector3D w = this.Ray.Direction,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

            float r = this.Radius;
                 
            return new NonAlignedBoundingBox(this.Ray.Start - u * r - v * r, u * r * 2, v * r * 2, this.Ray.Direction * this.Length);
        }
        public LineBeamIntersectionPoint GetIntersectionPoint(IQueryLine line)
        {
            return LineBeamIntersectionHelper.GetLineBeamIntersectionPoint(line, this);
        }

        public CylinderMock(Ray ray, float length, float radius)
        {
            this.Ray = ray;
            this.Length = length;
            this.Radius = radius;
            this.RadiusSqrt = radius * radius;
        }
    }
}
