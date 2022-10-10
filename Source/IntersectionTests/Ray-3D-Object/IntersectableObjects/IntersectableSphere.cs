using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests.Ray_3D_Object.IntersectableObjects
{
    public class IntersectableSphere : IIntersecableObject, IParseableString
    {
        public IIntersectableRayDrawingObject RayHeigh { get; protected set; } //Die Lichtquelle muss beim Schattenstrahltest wissen, ob Lichtquelle-RayHeigh == IntersectionPoint.RayHeigh
        public Vector3D AABBCenterPoint { get; private set; } //Das ist der Mittelpunkt von der Axis Aligned Bounding Box
        public Vector3D MinPoint { get; private set; }
        public Vector3D MaxPoint { get; private set; }

        public float Radius { get; private set; } //Die SphereLightSourceNoSpotCutoff kennt die RaySphere direkt. Deswegen sind diese beiden Propertys public
        public Vector3D Center { get; private set; }

        protected Matrix4x4 inverseNormalMatrix; //Zum Berechnen der Texturkoordinaten
        protected Matrix4x4 normalMatrix; //Zum Berechnen der Tangente

        public IntersectableSphere(Vector3D center, float radius, IIntersectableRayDrawingObject rayHeigh)
        {
            this.RayHeigh = rayHeigh;
            this.Center = center;
            this.Radius = radius;
            this.MinPoint = center - new Vector3D(1, 1, 1) * radius;
            this.MaxPoint = center + new Vector3D(1, 1, 1) * radius;
            this.AABBCenterPoint = center;// this.MinPoint + (this.MaxPoint - this.MinPoint) / 2;

            //Wenn ich mit dem Strahl im World-Space die Kugel treffe und die Normale beim Schnittpunkt
            //berechne, habe ich eine Worldspace-Normale, welche ich mit der inversen Dreh-Matrix in den
            //Objekt-Space umrechne. Mit der Objekt-Space-Normale kann ich dann die TexturKoordinaten berechnen
            this.inverseNormalMatrix = Matrix4x4.InverseNormalRotate(this.RayHeigh.Propertys.Orientation);

            this.normalMatrix = Matrix4x4.NormalRotate(this.RayHeigh.Propertys.Orientation);
        }

        public string ToCtorString()
        {
            return $"new IntersectableSphere({Center.ToCtorString()},{Radius.ToFloatString()},null)";
        }

        public IIntersectionPointSimple GetSimpleIntersectionPoint(Ray ray, float time)
        {
            float distance = IntersectionHelper.GetIntersectionPointDistanceBetweenRayAndSphere(ray, Center, Radius);
            if (float.IsNaN(distance)) return null;
            return GetSimpleIntersectionPointFromDistanceValue(ray, distance);
        }

        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float time)
        {
            List<float> distanceList = IntersectionHelper.GetAllIntersectionPointDistancesBetweenRayAndSphere(ray, Center, Radius);
            return distanceList.Select(x => GetSimpleIntersectionPointFromDistanceValue(ray, x)).Where(x => x != null).ToList(); //Durch Parallax-CutEdgeCuttoff kann GetSimpleIntersectionPointFromDistanceValue null zurück geben
        }

        private IIntersectionPointSimple GetSimpleIntersectionPointFromDistanceValue(Ray ray, float distance)
        {
            Vector3D position = ray.Start + ray.Direction * distance;
            Vector3D normal = Vector3D.Normalize(position - Center);

            ParallaxPoint parallaxPoint = null;

            //Berechne Farbe, wenn nötig
            if (this.RayHeigh.Propertys.BlackIsTransparent)
            {
                //http://www.scratchapixel.com/code.php?id=34&origin=/lessons/3d-basic-rendering/global-illumination-path-tracing
                Vector3D texNormal = Vector3D.Normalize(Matrix4x4.MultDirection(this.inverseNormalMatrix, normal));
                float textcoordU = (float)((Math.PI - Math.Atan2(texNormal.Z, texNormal.X)) / (2 * Math.PI)); //SmallUPBP-Phi-Formel
                float textcoordV = (float)(Math.Acos(texNormal.Y) / Math.PI);
                
                if (this.RayHeigh.IsBlackColor(textcoordU, textcoordV, position))
                {
                    List<float> distanceList = IntersectionHelper.GetAllIntersectionPointDistancesBetweenRayAndSphere(ray, Center, Radius);
                    if (distanceList.Count == 2 && distanceList.Last() > distance) return GetSimpleIntersectionPointFromDistanceValue(ray, distanceList.Last());
                    return null;
                }
            }

            //Berechne ParallaxPoint, wenn nötig
            if (this.RayHeigh.Propertys.NormalSource.Type == NormalSource.Parallax && this.RayHeigh.Propertys.NormalSource.As<NormalFromParallax>().IsParallaxEdgeCutoffEnabled)
            {
                //Texturkoordinaten
                Vector3D texNormal = Vector3D.Normalize(Matrix4x4.MultDirection(this.inverseNormalMatrix, normal));
                float textcoordU = (float)((Math.PI - Math.Atan2(texNormal.Z, texNormal.X)) / (2 * Math.PI)); //SmallUPBP-Phi-Formel
                float textcoordV = (float)(Math.Acos(texNormal.Y) / Math.PI);

                //Tangente
                double tangentPhi = (Math.PI - Math.Atan2(texNormal.Z, texNormal.X)) + 0.5 * Math.PI; //Ich rechne hier Plus PI-Halbe um somit statt in Richtung Normale in Richtugn tangente zu zeigen
                Vector3D tangentLocal = new Vector3D(-(float)Math.Cos(tangentPhi), 0, (float)Math.Sin(tangentPhi));
                Vector3D tangent = Vector3D.Normalize(Matrix4x4.MultDirection(this.normalMatrix, texNormal + tangentLocal) - normal);


                Vector3D shadeNormal = normal;
                if (shadeNormal * ray.Direction > 0) shadeNormal = -shadeNormal;
                Vertex interpolatedVertexWithoutParallax = new Vertex(position, shadeNormal, tangent, textcoordU, textcoordV);

                parallaxPoint = this.RayHeigh.ParallaxMap.GetParallaxIntersectionPointFromOutToIn(interpolatedVertexWithoutParallax, ray.Direction);
                if (parallaxPoint == null) return null;
            }

            return new SphereIntersectionPoint(this, position, distance, parallaxPoint, ray.Direction, normal);
        }

        public IntersectionPoint TransformSimplePointToIntersectionPoint(IIntersectionPointSimple simplePoint)
        {
            SphereIntersectionPoint point = (SphereIntersectionPoint)simplePoint;

            //Texturkoordinaten
            Vector3D texNormal = Vector3D.Normalize(Matrix4x4.MultDirection(this.inverseNormalMatrix, point.Normal)); //Drehe Normale vom World- in Lokal-Space
            //http://www.scratchapixel.com/code.php?id=34&origin=/lessons/3d-basic-rendering/global-illumination-path-tracing
            float textcoordU = (float)((Math.PI - Math.Atan2(texNormal.Z, texNormal.X)) / (2 * Math.PI)); //SmallUPBP-Phi-Formel
            float textcoordV = (float)(Math.Acos(texNormal.Y) / Math.PI);                              // acosf returns a value in the range [0, pi] and we also need to remap it to the range [0, 1]

            //Shaded-Normale
            Vector3D shadeNormal = point.Normal;
            if (shadeNormal * point.RayDirection > 0) shadeNormal = -shadeNormal;

            //Tangente
            //Wenn die TexturMatrix eine Rotation erzeugt, hat das Einfluß auf die Tangente. aktuell beachte ich nur die Objektrotation.
            //Ich müsste aus der Texturmatrix die Rotation ermitteln und damit dann die Tangente um die Normale drehen, wenn ich das hier noch einbauen wöllte.
            double tangentPhi = (Math.PI - Math.Atan2(texNormal.Z, texNormal.X)) + 0.5 * Math.PI; //Ich rechne hier Plus PI-Halbe um somit statt in Richtung Normale in Richtugn tangente zu zeigen
            Vector3D tangentLocal = new Vector3D(-(float)Math.Cos(tangentPhi), 0, (float)Math.Sin(tangentPhi));
            Vector3D tangent = Vector3D.Normalize(Matrix4x4.MultDirection(this.normalMatrix, texNormal + tangentLocal) - point.Normal) ;
            
            return this.RayHeigh.CreateIntersectionPoint(
                new Vertex(point.Position, shadeNormal, tangent, textcoordU, textcoordV),
                shadeNormal, point.Normal, point.RayDirection, point.ParallaxPoint, this);
        }
    }
}
