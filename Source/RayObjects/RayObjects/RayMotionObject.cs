using System;
using System.Collections.Generic;
using System.Linq;
using IntersectionTests;
using GraphicMinimal;
using GraphicGlobal;

namespace RayObjects.RayObjects
{
    //Bei der DirectLighting-PdfA-Berechung in der SurfaceWithMotion-Lichtquelle benötige ich die Information darüber, welches 
    //Dreieck getroffen wurde. Die Schattenstrahlberechung geht aber nur, wenn das InterersectedObjekt das RayMotionObject ist.
    //Deswegen erweitere ich den IntersectionPoint hier um das getroffene 'interne' Dreieck was nur von den Klassen verwendet wird,
    //die explizit mit den Dreiecken des RayMotionObjects arbeiten
    public class IntersectionPointWithRayMotionObject : IntersectionPoint
    {
        public IIntersecableObject IntersectedTriangle { get; private set; }

        public IntersectionPointWithRayMotionObject(Vertex vertexPoint, Vector3D color, Vector3D bumpmapColor, Vector3D flatNormal, Vector3D orientedFlatNormal, ParallaxPoint parallaxPoint, IIntersecableObject intersectedObject, IIntersectableRayDrawingObject rayDrawingObject, IIntersecableObject intersectedTriangle)
            :base(vertexPoint, color, bumpmapColor, flatNormal, orientedFlatNormal, parallaxPoint, intersectedObject, rayDrawingObject)
        {
            this.IntersectedTriangle = intersectedTriangle;
        }
    }

    public class RayMotionObject : IIntersecableObject, IRayObject
    {
        public IIntersectableRayDrawingObject RayHeigh { get; protected set; } //Die Lichtquelle muss beim Schattenstrahltest wissen, ob Lichtquelle-RayHeigh == IntersectionPoint.RayHeigh
        public Vector3D AABBCenterPoint { get; private set; } //Das ist der Mittelpunkt von der Axis Aligned Bounding Box
        public Vector3D MinPoint { get; private set; }
        public Vector3D MaxPoint { get; private set; }

        public IMotionBlurMovement Movement { get; private set; }
        private readonly IntersectionFinder rayObjectIntersectionInLocalSpace; //Enthält die RayLow-List im Lokal-Objekt-Space
        private readonly DrawingObject drawingObject;

        public List<RayTriangle> LocalSpaceTriangles { get; private set; }

        public RayMotionObject(DrawingObject drawingObject, IIntersectableRayDrawingObject rayHigh, Action<string, float> progressChangeHandler, IMotionBlurMovement movement)
        {
            this.RayHeigh = rayHigh;
            this.drawingObject = drawingObject;
            this.Movement = movement;

            var box = GetBoundingBox(10);
            this.MinPoint = box.Min;
            this.MaxPoint = box.Max;
            this.AABBCenterPoint = this.MinPoint + (this.MaxPoint - this.MinPoint) / 2;

            this.LocalSpaceTriangles = drawingObject.TriangleData.Triangles.Select(x => new RayTriangle(x, rayHigh)).ToList();
            this.rayObjectIntersectionInLocalSpace = new IntersectionFinder(this.LocalSpaceTriangles.Cast<IIntersecableObject>().ToList(), progressChangeHandler);

            this.SurfaceArea = drawingObject.TriangleData.Triangles.Sum(x => GetSurvaceAreaFromTriangle(x)) * rayHigh.Propertys.Size * rayHigh.Propertys.Size;
        }

        private float GetSurvaceAreaFromTriangle(Triangle triangle)
        {
            Vector3D pa2 = Vector3D.Cross(triangle.V[2].Position - triangle.V[1].Position, triangle.V[1].Position - triangle.V[0].Position);
            return (float)Math.Sqrt(pa2 * pa2) * 0.5f;
        }

        private BoundingBox GetBoundingBox(int timeSteps)
        {
            BoundingBox masterBox = null;
            for (float time = 0; time <= 1; time += 1.0f / timeSteps)
            {
                var box = GetBoundingBoxFromTime(time);
                masterBox = masterBox == null ? box : new BoundingBox(masterBox, box);
            }
            return masterBox;
        }

        public BoundingBox GetBoundingBoxFromTime(float time)
        {
            var matrizes = this.Movement.GetMotionMatrizes(time);
            var trianglesInWorldSpace = TriangleHelper.TransformTrianglesWithMatrix(matrizes.ObjectToWorldMatrix, this.drawingObject.TriangleData.Triangles);
            return trianglesInWorldSpace.GetBoundingBox();            
        }

        public IIntersectionPointSimple GetSimpleIntersectionPoint(Ray ray, float time)
        {
            var m = this.Movement.GetMotionMatrizes(time);

            Vector3D p1 = Matrix4x4.MultPosition(m.WorldToObjectMatrix, ray.Start);
            Vector3D p2 = Matrix4x4.MultPosition(m.WorldToObjectMatrix, ray.Start + ray.Direction);

            Ray objectSpaceRay = new Ray(p1, Vector3D.Normalize(p2 - p1));
            var intersectionPoint = this.rayObjectIntersectionInLocalSpace.GetIntersectionPoint(objectSpaceRay, time); //Schnittpunkt in Objektkoordinaten

            if (intersectionPoint == null) return null;

            //Umrechnung des Schnittpunktes von Objekt- in World-Koordinaten
            Vector3D position = Matrix4x4.MultPosition(m.ObjectToWorldMatrix, intersectionPoint.Position);
            Vector3D orientedFlatNormal = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, intersectionPoint.OrientedFlatNormal));
            Vector3D shadedNormal = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, intersectionPoint.ShadedNormal));
            Vector3D flatNormal = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, intersectionPoint.FlatNormal));
            Vector3D tangent = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, intersectionPoint.Tangent));
            //tangent = intersectionPoint.Tangent;
            float distanceToRayStart = (position - ray.Start).Length(); //intersectionPoint.DistanceToRayStart

            ParallaxPoint movedParallaxPoint = null;
            if (intersectionPoint.ParallaxPoint != null)
            {
                movedParallaxPoint = new ParallaxPoint(intersectionPoint.ParallaxPoint);
                movedParallaxPoint.EntryWorldPoint = new Vertex(
                    Matrix4x4.MultPosition(m.ObjectToWorldMatrix, movedParallaxPoint.EntryWorldPoint.Position),
                    Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, movedParallaxPoint.EntryWorldPoint.Normal)),
                    Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, movedParallaxPoint.EntryWorldPoint.Tangent)),
                    movedParallaxPoint.EntryWorldPoint.TexcoordU,
                    movedParallaxPoint.EntryWorldPoint.TexcoordV
                    );
                movedParallaxPoint.WorldSpacePoint = Matrix4x4.MultPosition(m.ObjectToWorldMatrix, movedParallaxPoint.WorldSpacePoint);
                movedParallaxPoint.Normal = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, movedParallaxPoint.Normal));
            }

            var nestedPoint = new IntersectionPointWithRayMotionObject(new Vertex(position, shadedNormal, tangent, intersectionPoint.VertexPoint.TexcoordU, intersectionPoint.VertexPoint.TexcoordV), intersectionPoint.Color, intersectionPoint.BumpmapColor, flatNormal, orientedFlatNormal, movedParallaxPoint, this, this.RayHeigh, intersectionPoint.IntersectedObject);
            
            return new RayMotionIntersectionPoint(this, position, distanceToRayStart, nestedPoint);
        }

        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float time)
        {
            var m = this.Movement.GetMotionMatrizes(time);

            Vector3D p1 = Matrix4x4.MultPosition(m.WorldToObjectMatrix, ray.Start);
            Vector3D p2 = Matrix4x4.MultPosition(m.WorldToObjectMatrix, ray.Start + ray.Direction);

            Ray objectSpaceRay = new Ray(p1, Vector3D.Normalize(p2 - p1));
            var intersectionPoints = this.rayObjectIntersectionInLocalSpace.GetAllIntersectionPoints(objectSpaceRay, float.MaxValue, time); //Schnittpunkt in Objektkoordinaten

            if (intersectionPoints.Any() == false) return new List<IIntersectionPointSimple>();

            List<IIntersectionPointSimple> returnList = new List<IIntersectionPointSimple>();
            foreach (var intersectionPoint in intersectionPoints)
            {
                //Umrechnung des Schnittpunktes von Objekt- in Eye-Koordinaten
                Vector3D position = Matrix4x4.MultPosition(m.ObjectToWorldMatrix, intersectionPoint.Position);
                Vector3D orientedFlatNormal = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, intersectionPoint.OrientedFlatNormal));
                Vector3D shadedNormal = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, intersectionPoint.ShadedNormal));
                Vector3D flatNormal = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, intersectionPoint.FlatNormal));
                Vector3D tangent = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, intersectionPoint.Tangent));
                float distanceToRayStart = (position - ray.Start).Length(); //intersectionPoint.DistanceToRayStart

                ParallaxPoint movedParallaxPoint = null;
                if (intersectionPoint.ParallaxPoint != null)
                {
                    movedParallaxPoint = new ParallaxPoint(intersectionPoint.ParallaxPoint);
                    movedParallaxPoint.EntryWorldPoint = new Vertex(
                        Matrix4x4.MultPosition(m.ObjectToWorldMatrix, movedParallaxPoint.EntryWorldPoint.Position),
                        Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, movedParallaxPoint.EntryWorldPoint.Normal)),
                        Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, movedParallaxPoint.EntryWorldPoint.Tangent)),
                        movedParallaxPoint.EntryWorldPoint.TexcoordU,
                        movedParallaxPoint.EntryWorldPoint.TexcoordV
                        );
                    movedParallaxPoint.WorldSpacePoint = Matrix4x4.MultPosition(m.ObjectToWorldMatrix, movedParallaxPoint.WorldSpacePoint);
                    movedParallaxPoint.Normal = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, movedParallaxPoint.Normal));
                }

                //var nestedPoint = new IntersectionPoint(new Vertex(position, orientedFlatNormal, tangent, intersectionPoint.VertexPoint.TexcoordU, intersectionPoint.VertexPoint.TexcoordV), intersectionPoint.Color, intersectionPoint.BumpmapColor, flatNormal, shadedNormal, movedParallaxPoint, intersectionPoint.IntersectedObject, this.RayHeigh);
                var nestedPoint = new IntersectionPointWithRayMotionObject(new Vertex(position, shadedNormal, tangent, intersectionPoint.VertexPoint.TexcoordU, intersectionPoint.VertexPoint.TexcoordV), intersectionPoint.Color, intersectionPoint.BumpmapColor, flatNormal, orientedFlatNormal, movedParallaxPoint, this, this.RayHeigh, intersectionPoint.IntersectedObject);
                returnList.Add(new RayMotionIntersectionPoint(this, position, distanceToRayStart, nestedPoint));
            }
            return returnList;            
        }

        public IntersectionPoint TransformSimplePointToIntersectionPoint(IIntersectionPointSimple simplePoint)
        {
            return (simplePoint as RayMotionIntersectionPoint).NestedPoint;
        }

        public SurfacePoint GetRandomPointOnSurface(IRandom rand)
        {
            Triangle triangle = drawingObject.TriangleData.Triangles[rand.Next(drawingObject.TriangleData.Triangles.Length)];

            Vector3D edgePos1 = triangle.V[1].Position - triangle.V[0].Position;
            Vector3D edgePos2 = triangle.V[2].Position - triangle.V[0].Position;
            Vector2D edgeTex1 = triangle.V[1].TextcoordVector - triangle.V[0].TextcoordVector;
            Vector2D edgeTex2 = triangle.V[2].TextcoordVector - triangle.V[0].TextcoordVector;

            // get two randoms
            float sqr1 = (float)Math.Sqrt(rand.NextDouble());
            float r2 = (float)rand.NextDouble();

            // make barycentric coords
            float a = 1.0f - sqr1;
            float b = (1.0f - r2) * sqr1;

            // make position from barycentrics
            // calculate interpolation by using two edges as axes scaled by the
            // barycentrics
            Vector3D position = edgePos1 * a + edgePos2 * b + triangle.V[0].Position;
            Vector2D texcoord = edgeTex1 * a + edgeTex2 * b + triangle.V[0].TextcoordVector;
            Vector3D color = this.RayHeigh.GetColor(texcoord.X, texcoord.Y, position);

            Vector3D pa2 = Vector3D.Cross(triangle.V[2].Position - triangle.V[1].Position, triangle.V[1].Position - triangle.V[0].Position);
            float survaceArea = (float)Math.Sqrt(pa2 * pa2) * 0.5f;

            float time = (float)rand.NextDouble();
            var m = this.Movement.GetMotionMatrizes(time);
            Vector3D positionWorld = Matrix4x4.MultPosition(m.ObjectToWorldMatrix, position);
            Vector3D normalWorld = Vector3D.Normalize(Matrix4x4.MultDirection(m.NormalObjectToWorldMatrix, triangle.Normal));

            return new SurfacePoint(positionWorld, normalWorld, color, this, 1.0f / drawingObject.TriangleData.Triangles.Length * 1.0f / survaceArea);
        }
        public float SurfaceArea { get; private set; }
    }

    class RayMotionIntersectionPoint : IIntersectionPointSimple
    {
        public IIntersecableObject IntersectedObject { get; private set; }
        public Vector3D Position { get; private set; }
        public float DistanceToRayStart { get; private set; }
        public IntersectionPoint NestedPoint { get; private set; }

        public RayMotionIntersectionPoint(IIntersecableObject intersectedObject, Vector3D position, float distanceToRayStart, IntersectionPoint nestedPoint)
        {
            this.IntersectedObject = intersectedObject;
            this.Position = position;
            this.DistanceToRayStart = distanceToRayStart;
            this.NestedPoint = nestedPoint;
        }
    }
}
