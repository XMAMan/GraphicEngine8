using System;
using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests
{
    //Ein Punkt auf der Oberflächen von ein Objekt liegt. Kann somit kein Media- oder Kamera-Punkt sein. Propertys: Position, Farbe, Normale + RayObjectHigh
    public class IntersectionPoint
    {
        public Vertex VertexPoint { get; private set; }//Punkt ohne Parallax/Grouroud-Shading/Normalmapping-Effekt aber mit gedrehter Flat-Normale
        public ParallaxPoint ParallaxPoint { get; private set; }
        public Vector3D Color { get; private set; }
        public Vector3D BumpmapColor { get; private set; } //Wird für die Roughness in der Heiz/Walter-Brdf benötigt
        public Vector3D FlatNormal { get; private set; }//Die Normale vom Dreieck ohne dass ich sie irgendwie zum Strahl hin drehen würde

        //Flat-Normale, welche immer in die Richtung zeigt, aus der der Strahl kam
        //Wird beim Reflektieren/Brechen/Brdf-Sampling genutzt
        public Vector3D OrientedFlatNormal { get; private set; } //Physikalisch korrekte Normale


        //Normale nach Grouroud-Shading/Normalmapping/Parallax-Mapping
        //Wird beim Geometry-Term verwendet
        public Vector3D ShadedNormal //Physikalisch gefakte Normale
        {
            get
            {
                return this.VertexPoint.Normal;
            }
        }

        public IIntersecableObject IntersectedObject { get; private set; } = null; //Damit ich nach dem Brdf-Sampling den nächsten Schnittpunkt finden kann
        public IIntersectableRayDrawingObject IntersectedRayHeigh { get; private set; } = null;
        
        public static IntersectionPoint CreatePointOnLight(Vector3D position, Vector3D color, Vector3D normal, IIntersecableObject pointSampler, IIntersectableRayDrawingObject rayDrawing)
        {
            return new IntersectionPoint(new Vertex(position, normal), color, null, normal, normal, null, pointSampler, rayDrawing);
        }

        //Konstruktor für Schnittpunkte auf Oberflächen
        public IntersectionPoint(Vertex vertexPoint, Vector3D color, Vector3D bumpmapColor, Vector3D flatNormal, Vector3D orientedFlatNormal, ParallaxPoint parallaxPoint, IIntersecableObject intersectedObject, IIntersectableRayDrawingObject rayDrawingObject)
        {
            this.VertexPoint = vertexPoint;
            this.Color = color;
            this.BumpmapColor = bumpmapColor;
            this.FlatNormal = flatNormal;
            this.OrientedFlatNormal = orientedFlatNormal;
            this.ParallaxPoint = parallaxPoint;
            this.IntersectedObject = intersectedObject;
            this.IntersectedRayHeigh = rayDrawingObject;

            if (Math.Abs(this.OrientedFlatNormal.Length() - 1) > 0.1f) throw new Exception("Normal must have length 1");
            if (ShadedNormal != null && Math.Abs(this.ShadedNormal.Length() - 1) > 0.1f) throw new Exception("Normal must have length 1");
        }

        public IRaytracerDrawingProps Propertys
        {
            get
            {
                return this.IntersectedRayHeigh.Propertys;
            }
        }

        public Vector3D Position
        {
            get
            {
                return this.VertexPoint.Position;
            }
        }

        

        public Vector3D Tangent
        {
            get
            {
                return this.VertexPoint.Tangent;
            }
        }

        public float GlossyPowExponent
        {
            get
            {
                return this.Propertys.GlossyPowExponent;
            }
        }

        public BrdfModel BrdfModel
        {
            get
            {
                return this.Propertys.BrdfModel;
            }
        }

        public float RefractionIndex               // Luft...1, Wasser.. 1.33
        {
            get
            {
                return this.IntersectedRayHeigh.Propertys.RefractionIndex;
            }
        }

        public float Albedo
        {
            get
            {
                return this.Propertys.Albedo;
            }
        }

        public float SpecularAlbedo
        {
            get
            {
                return this.Propertys.SpecularAlbedo;
            }
        }

        public float SpecularHighlightPowExponent
        {
            get
            {
                return this.Propertys.SpecularHighlightPowExponent;
            }
        }

        public float SpecularHighlightCutoff1
        {
            get
            {
                return this.Propertys.SpecularHighlightCutoff1;
            }
        }

        public float SpecularHighlightCutoff2
        {
            get
            {
                return this.Propertys.SpecularHighlightCutoff2;
            }
        }

        public bool IsLocatedOnLightSource
        {
            get
            {
                return this.IntersectedRayHeigh != null && this.Propertys.RaytracingLightSource != null;
            }
        }

        public bool IsLocatedOnInfinityAwayLightSource
        {
            get
            {
                return this.IntersectedRayHeigh != null && this.Propertys.RaytracingLightSource != null && this.Propertys.RaytracingLightSource.IsInfinityAway;
            }
        }

        public override string ToString()
        {
            return this.IntersectedRayHeigh.Propertys.Name + (this.IntersectedObject.RayHeigh.Media != null ? "Media=" + this.IntersectedObject.RayHeigh.Media.ToString() + "=" + this.IntersectedObject.RayHeigh.Media.Priority : "NoMedia");
        }
    }
}
