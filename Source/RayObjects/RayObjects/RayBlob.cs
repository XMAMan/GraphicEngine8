using System;
using IntersectionTests;
using GraphicMinimal;
using GraphicGlobal;
using IntersectionTests.Ray_3D_Object.IntersectableObjects;

namespace RayObjects.RayObjects
{
    class RayBlob : IntersectableBlob, IIntersecableObject, IRayObject
    {
        public RayBlob(Vector3D[] centerList, float sphereRadius, IIntersectableRayDrawingObject rayHeigh)
            : base(centerList, sphereRadius, rayHeigh)
        {
            this.SurfaceArea = centerList.Length * 4.0f * sphereRadius * sphereRadius * (float)Math.PI; // Es wird die Summe aller Flächeninhalte aller Kugeln zurück gegeben
        }

        

        public SurfacePoint GetRandomPointOnSurface(IRandom rand)
        {
            Vector3D center = this.centerList[rand.Next(this.centerList.Length)];

            float phi = 2 * (float)(Math.PI * rand.NextDouble());
            float theta = (float)(Math.Acos(1 - 2 * rand.NextDouble()));
            Vector3D position = center + new Vector3D((float)(Math.Cos(phi) * Math.Sin(theta)), (float)(Math.Sin(phi) * Math.Sin(theta)), (float)(Math.Cos(theta))) * this.size;

            CalculateTextueCoordinates(position, out Vector3D gradient, out Vector3D texCoord);
            Vector3D normal = Vector3D.Normalize(gradient);
            Vector3D color = this.RayHeigh.GetColor(texCoord.X, texCoord.Y, position);
            return new SurfacePoint(position, normal, color, this, 1.0f / this.SurfaceArea);
        }

        public float SurfaceArea { get; private set; }
    }
}
