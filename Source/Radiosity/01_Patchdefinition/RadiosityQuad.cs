using System;
using System.Collections.Generic;
using GraphicMinimal;
using IntersectionTests;
using RayObjects.RayObjects;
using IntersectionTests.Ray_3D_Object.IntersectableObjects;
using RaytracingBrdf;
using GraphicGlobal;

namespace Radiosity
{
    class RadiosityQuad : RayQuad, IPatch
    {
        public List<ViewFactor> ViewFaktors { get; private set; }
        private Vector3D centerRayHeigh = null;
        private float lightingArea = 1;
        private readonly Vector3D[] cornerColors;

        public Vector3D[] CornerPoints { get; private set; }

        public RadiosityQuad(RayQuad quad)
            : base(quad, quad.RayHeigh)
        {
            this.CornerPoints = new Vector3D[] { v1.Position, v2.Position, v3.Position, v4.Position };

            this.ViewFaktors = new List<ViewFactor>();

            Vector2D texcoord = InterpolateVector2DOverQuad(this.v1.TextcoordVector, this.v2.TextcoordVector, this.v3.TextcoordVector, this.v4.TextcoordVector, 0.5f, 0.5f);
            this.ColorOnCenterPoint = this.RayHeigh.GetColor(texcoord.X, texcoord.Y, this.CenterOfGravity);
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

            var simplePoint1 = (QuadIntersectionPoint)simplePoint;

            Vector3D color = InterpolateVectorOverQuad(this.cornerColors[0], this.cornerColors[1], this.cornerColors[2], this.cornerColors[3], simplePoint1.F1, simplePoint1.F2);
            
            if (this.RayHeigh.Propertys.Color.Type != ColorSource.ColorString) color = point.Color * color.Length();

            return new IntersectionPoint(point.VertexPoint, color, point.BumpmapColor, point.FlatNormal, point.OrientedFlatNormal, point.ParallaxPoint, point.IntersectedObject, point.IntersectedRayHeigh);
        }

        

        public override string ToString()
        {
            return this.RayHeigh.Propertys.Name + " [" + this.v1.Position.ToString() + "|" + this.v2.Position.ToString() + "|" + this.v3.Position.ToString() + "|" + this.v4.Position.ToString() + "]";
        }
    }
}
