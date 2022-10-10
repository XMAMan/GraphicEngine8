using GraphicMinimal;

namespace GraphicGlobal
{
    //Schnittpunkt zwischen ein Strahl und ein Objekt, was Parallax-Mapping verwendet
    //Der Parallax-Point liegt unter dem 'echten' getroffenen Objekt
    public class ParallaxPoint
    {
        public bool PointIsOnTopHeight; //Liegt der Punkt ganz oben auf Höhe z==TexturHeightScale?
        public Vertex EntryWorldPoint;  //Über diesen Punkt wurde die Textur betreten (Wurde über den Schnittpunkttest mit den Dreieck bestimmt)
        public Vector2D TexureCoords;   //XY-Koordinaten nach Versatz um Parallax im TexObj-Raum (Ohne TextureMatrix-Multiplikation)
        public Vector3D TexturSpacePoint; //X/Y sind die Koordinaten nach Versatz um Parallax im TexObj*TexturMatrix-Raum; Z = 0..textureHighScaleFaktor
        public Vector3D WorldSpacePoint;  //Das ist der TexturSpacePoint umgerechnet in Weltkoordinaten. Dieser Punkt liegt innerhalb des Geometryobjekts in der Luft
        public Vector3D Normal; //Normale aus der Bumpmap an der Stelle TexturSpacePoint.XY ausgelesen
        public Matrix4x4 TangentToWorldMatrix;    //Die Normale von dieser Matrix wurde per Flat oder Smooth erstellt
        public Matrix4x4 WorldToTangentMatrix;    //Entspricht Transpose(TangentToWorldMatrix)

        public ParallaxPoint() { }

        public ParallaxPoint(ParallaxPoint copy)
        {
            this.PointIsOnTopHeight = copy.PointIsOnTopHeight;
            this.EntryWorldPoint = copy.EntryWorldPoint;
            this.TexturSpacePoint = copy.TexturSpacePoint;
            this.WorldSpacePoint = copy.WorldSpacePoint;
            this.Normal = copy.Normal;
            this.TangentToWorldMatrix = copy.TangentToWorldMatrix;
            this.WorldToTangentMatrix = copy.WorldToTangentMatrix;
        }
    }
}
