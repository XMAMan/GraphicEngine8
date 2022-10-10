using System;
using System.Collections.Generic;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using IntersectionTests.Ray_3D_Object.IntersectableObjects;
using RayObjects.RayObjects;
using RaytracingBrdf;

namespace Radiosity
{
    class RadiosityTriangle : RayTriangle, IPatch
    {
        public Vector3D[] CornerPoints { get; private set; }
        public List<ViewFactor> ViewFaktors { get; private set; }
        private Vector3D centerRayHeigh = null;
        private float lightingArea = 1;
        private readonly Vector3D[] cornerColors;

        public RadiosityTriangle(RayTriangle triangle)
            : base(triangle)
        {
            this.CornerPoints = new Vector3D[] { this.V[0].Position, this.V[1].Position, this.V[2].Position };
            this.ViewFaktors = new List<ViewFactor>();
            
            float beta = 0.5f, gamma = 0.5f;
            float f = 1 - beta - gamma;
            float textcoordU = f * this.V[0].TexcoordU + beta * this.V[1].TexcoordU + gamma * this.V[2].TexcoordU;
            float textcoordV = f * this.V[0].TexcoordV + beta * this.V[1].TexcoordV + gamma * this.V[2].TexcoordV;
            this.ColorOnCenterPoint = this.RayHeigh.GetColor(textcoordU, textcoordV, this.CenterOfGravity);

            float diffuseFactor = BrdfFactory.CreateBrdf(new IntersectionPoint(new Vertex(new Vector3D(0, 0, 0), new Vector3D(1, 0, 0)), this.ColorOnCenterPoint, null, null, new Vector3D(1, 0, 0), null, this, this.RayHeigh), new Vector3D(-1, 0, 0), 1, 1).DiffuseFactor;
            this.ColorOnCenterPoint *= diffuseFactor;

            this.cornerColors = new Vector3D[this.CornerPoints.Length];
            for (int i = 0; i < this.cornerColors.Length; i++) this.cornerColors[i] = this.ColorOnCenterPoint;
        }

        public void AddViewFaktor(ViewFactor faktor)
        {
            this.ViewFaktors.Add(faktor);
        }

        public bool IsLightSource { get { return this.RayHeigh.Propertys.RaytracingLightSource != null; } }

        public void SetCenterPointFromRayHeigh(Vector3D point)
        {
            this.centerRayHeigh = point;
        }

        public void SetLightingArea(float area)
        {
            this.lightingArea = area;
        }

        public bool IsInSpotDirection(Vector3D point)
        {
            if (this.IsLightSource == false) return true;
            if ((this.RayHeigh.Propertys.RaytracingLightSource is IRaytracingSphereWithSpotLight) == false) return true;
            return IsInSpotCuttoff((float)Math.Cos((this.RayHeigh.Propertys.RaytracingLightSource as IRaytracingSphereWithSpotLight).SpotCutoff * Math.PI / 180), (this.RayHeigh.Propertys.RaytracingLightSource as IRaytracingSphereWithSpotLight).SpotDirection, Vector3D.Normalize(point - this.centerRayHeigh));
        }

        private static bool IsInSpotCuttoff(float spotCutoffCosinus, Vector3D spotDirection, Vector3D direction)
        {
            float spot = Math.Max(spotDirection * direction, 0.0f);
            return spot >= spotCutoffCosinus;
        }

        public float EmissionPerPatch 
        {
            get
            {
                return (this.RayHeigh.Propertys.RaytracingLightSource as IRaytracingLightSource).Emission * (this.SurfaceArea / this.lightingArea);
            }
        }

        public Vector3D InputRadiosity { get; set; }
        public Vector3D OutputRadiosity { get; set; }

        public Vector3D ColorOnCenterPoint { get; private set; }

        public void SetCornerColor(int cornerIndex, Vector3D color)
        {
            this.cornerColors[cornerIndex] = color;
        }

        public new IntersectionPoint TransformSimplePointToIntersectionPoint(IIntersectionPointSimple simplePoint)
        {
            var point = base.TransformSimplePointToIntersectionPoint(simplePoint);

            var simplePoint1 = (TriangleIntersectionPoint)simplePoint;
            float beta = simplePoint1.Beta, gamma = simplePoint1.Gamma;
            float f = 1 - beta - gamma;
            Vector3D color = f * this.cornerColors[0] + beta * this.cornerColors[1] + gamma * this.cornerColors[2];
            if (this.RayHeigh.Propertys.Color.Type != ColorSource.ColorString) color = point.Color * color.Length();

            //float textcoordU = f * this.V[0].TexcoordU + beta * this.V[1].TexcoordU + gamma * this.V[2].TexcoordU;
            //float textcoordV = f * this.V[0].TexcoordV + beta * this.V[1].TexcoordV + gamma * this.V[2].TexcoordV;

            return new IntersectionPoint(point.VertexPoint, color, point.BumpmapColor, point.FlatNormal, point.OrientedFlatNormal, point.ParallaxPoint, point.IntersectedObject, point.IntersectedRayHeigh);
        }

        public override string ToString()
        {
            return this.RayHeigh.Propertys.Name + " " + base.ToString();
        }
    }
}
