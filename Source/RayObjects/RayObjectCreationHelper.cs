using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicGlobal;
using ObjectDivider;
using RayObjects.RayObjects;
using static ObjectDivider.Divider;
using ParticipatingMedia.Media;

namespace RayObjects
{
    //Erzeugt eine Liste von IRayObjects
    public class RayObjectCreationHelper
    {
        private readonly ParticipatingMediaBuilder mediaBuilder = null;
        private readonly GlobalObjectPropertys globalObjektPropertys = null;

        public RayObjectCreationHelper(GlobalObjectPropertys globalObjektPropertys)
        {
            this.globalObjektPropertys = globalObjektPropertys;
            this.mediaBuilder = new ParticipatingMediaBuilder(new StandardParticipatingMediaFactory());
        }

        public RayObjectCreationHelper(GlobalObjectPropertys globalObjektPropertys, IParticipatingMediaFactory mediaFactory)
        {
            this.globalObjektPropertys = globalObjektPropertys;
            this.mediaBuilder = new ParticipatingMediaBuilder(mediaFactory);
        }

        //Liste von RayObjects (Enthält RayTriangle, RayBlob, RaySphere, MotionBlureObject)
        public List<IRayObject> CreateRayObjects(List<DrawingObject> drawingObjects)
        {
            return drawingObjects.SelectMany(x => CreateTriangleSphereOrBlob(x, ParticipatingMediaBuilder.GetMediaPriorityFromDrawingObject(x, drawingObjects))).ToList();
        }

        public List<IRayObject> CreatePlanarObjects(List<DrawingObject> drawingObjects, bool createQuads, NoMoreDividePlease noMoreDividePlease, Action<string, float> progressChanged = null)
        {
            var rayObjects = CreatePlanarObjects(drawingObjects, createQuads);
            return DividePlanarObjects(rayObjects.Cast<IDivideable>().ToList(), noMoreDividePlease, progressChanged);
        }

        public List<IRayObject> DividePlanarObjects(List<IDivideable> rayObjects, NoMoreDividePlease noMoreDividePlease, Action<string, float> progressChanged = null)
        {
            List<IRayObject> allRayObjects = new List<IRayObject>();
            for (int i = 0; i < rayObjects.Count; i++)
            {
                progressChanged?.Invoke("Unterteile Patche", i * 100 / rayObjects.Count);
                var divideables = Divider.Subdivide(rayObjects[i], noMoreDividePlease);
                allRayObjects.AddRange(divideables.Cast<IRayObject>());
            }
            return allRayObjects;
        }

        public List<IRayObject> CreatePlanarObjects(List<DrawingObject> drawingObjects, bool createQuads)
        {
            return drawingObjects.SelectMany(x => CreatePlanarObject(x, ParticipatingMediaBuilder.GetMediaPriorityFromDrawingObject(x, drawingObjects), createQuads)).ToList();
        }

        private List<IRayObject> CreatePlanarObject(DrawingObject drawingObject, int mediaPriority, bool createQuads)
        {
            var rayHeigh = new RayDrawingObject(drawingObject.DrawingProps, drawingObject.GetBoundingBoxFromObject(), this.mediaBuilder.CreateMediaForDrawingObject(drawingObject, mediaPriority));
            var divideables = drawingObject.GetTrianglesInWorldSpace().Cast<IDivideable>().ToList();
            if (createQuads) divideables = QuadCreator.GetQuadList(divideables);

            List<IRayObject> rayObjects = new List<IRayObject>(); //Alle Rayobjekte von ein DrawingObjekt
            foreach (var div in divideables)
            {
                if (div is Triangle) rayObjects.Add(new RayTriangle(div as Triangle, rayHeigh));
                if (div is Quad) rayObjects.Add(new RayQuad(div as Quad, rayHeigh));
            }

            return rayObjects;
        }

        private List<IRayObject> CreateTriangleSphereOrBlob(DrawingObject drawingObject, int mediaPriority)
        {
            if (drawingObject.DrawingProps.Name.Split(':')[0].Equals("CreateSphere") &&
                Convert.ToInt32(drawingObject.DrawingProps.Name.Split(':')[2]) > 5 &&
                Convert.ToInt32(drawingObject.DrawingProps.Name.Split(':')[3]) > 5 &&
                drawingObject.DrawingProps.HasExplosionEffect == false &&
                (drawingObject.DrawingProps.RaytracingLightSource == null || (drawingObject.DrawingProps.RaytracingLightSource is DiffuseSurfaceLightDescription)==false) &&
                drawingObject.DrawingProps.MotionBlurMovment == null)
            {
                var box = drawingObject.GetBoundingBoxFromObject();
                var rayHeigh = new RayDrawingObject(drawingObject.DrawingProps, box, this.mediaBuilder.CreateMediaForDrawingObject(drawingObject, mediaPriority));

                if (drawingObject.DrawingProps.NormalSource.Type == NormalSource.Parallax)
                {
                    rayHeigh.ParallaxMap.VisibleMap.MarkTrianglesWhichAreVisibleFromTheCamera(drawingObject.GetTrianglesInWorldSpace());
                }
                
                return new List<IRayObject>() { new RaySphere(box.Center, (float)Convert.ToDouble(drawingObject.DrawingProps.Name.Split(':')[1]) * drawingObject.DrawingProps.Size, rayHeigh) };
            }

            if (drawingObject.DrawingProps.BlobPropertys != null)
            {
                Matrix4x4 objToWorld = Matrix4x4.Model(drawingObject.DrawingProps.Position, drawingObject.DrawingProps.Orientation, drawingObject.DrawingProps.Size);
                float radius = drawingObject.DrawingProps.BlobPropertys.SphereRadius * drawingObject.DrawingProps.Size;
                var position = Matrix4x4.MultPosition(objToWorld, new Vector3D(0, 0, 0));
                var centerList = drawingObject.DrawingProps.BlobPropertys.CenterList.Select(x => position + x * drawingObject.DrawingProps.Size).ToArray();
                var boxFromAll = new BoundingBox(centerList.Select(x => new BoundingBox(x - new Vector3D(1, 1, 1) * radius, x + new Vector3D(1, 1, 1) * radius)));
                return new List<IRayObject>() { new RayBlob(centerList, radius, new RayDrawingObject(drawingObject.DrawingProps, boxFromAll, this.mediaBuilder.CreateMediaForDrawingObject(drawingObject, mediaPriority) )) };
            }

            if (drawingObject.DrawingProps.MotionBlurMovment != null)
            {
                var box = drawingObject.GetBoundingBoxFromObject();
                return new List<IRayObject>() { new RayMotionObject(drawingObject, new RayDrawingObject(drawingObject.DrawingProps, box, this.mediaBuilder.CreateMediaForDrawingObject(drawingObject, mediaPriority)), (text, zahl) => { }, MotionBlureMovementBuilder.Build(drawingObject.DrawingProps.MotionBlurMovment, drawingObject.DrawingProps)) };
            }

            if (drawingObject.DrawingProps.CreateQuads)
            {
                return CreatePlanarObject(drawingObject, mediaPriority, true);
            }

            return CreateTriangleList(drawingObject, mediaPriority);
        }

        private List<IRayObject> CreateTriangleList(DrawingObject drawingObject, int mediaPriority)
        {
            var rayHeigh = new RayDrawingObject(drawingObject.DrawingProps, drawingObject.GetBoundingBoxFromObject(), this.mediaBuilder.CreateMediaForDrawingObject(drawingObject, mediaPriority));
            var rayTriangles = drawingObject.GetTrianglesInWorldSpace().Select(x => ConvertTriangleToRayTriangle(x, rayHeigh)).ToList();
            if (drawingObject.DrawingProps.NormalSource.Type == NormalSource.Parallax)
            {
                rayHeigh.ParallaxMap.VisibleMap.MarkTrianglesWhichAreVisibleFromTheCamera(rayTriangles.Cast<Triangle>().ToList());
                //rayHeigh.ParallaxMap.VisibleMap.GetAsBitmap(this.globalObjektPropertys.Camera.Position).Save("ParallaxVisibleMap.bmp");
            }
            return rayTriangles.Cast<IRayObject>().ToList();
        }

        private RayTriangle ConvertTriangleToRayTriangle(Triangle t, RayDrawingObject rayHeigh)
        {
            Triangle triangle = t;
            if (rayHeigh.Propertys.HasExplosionEffect)
            {
                Vector3D translate = t.V[0].Normal * (float)Math.Abs(Math.Sin(this.globalObjektPropertys.Time / 100.0f)) * this.globalObjektPropertys.ExplosionRadius;
                triangle = new Triangle(
                    new Vertex(t.V[0].Position + translate, t.V[0].Normal, t.V[0].Tangent, t.V[0].TexcoordU, t.V[0].TexcoordV),
                    new Vertex(t.V[1].Position + translate, t.V[1].Normal, t.V[1].Tangent, t.V[1].TexcoordU, t.V[1].TexcoordV),
                    new Vertex(t.V[2].Position + translate, t.V[2].Normal, t.V[2].Tangent, t.V[2].TexcoordU, t.V[2].TexcoordV)
                    );
            }
            return new RayTriangle(triangle, rayHeigh);
        }
    }
}
