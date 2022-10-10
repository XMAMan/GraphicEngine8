using System;
using GraphicGlobal;
using GraphicMinimal;
using RayTracerGlobal;

namespace RayCameraNamespace
{
    //Wer verwendet alles die Kamera?
    //Lightracing, um Subpath-Point mit der Kamera zu verbinden
    //Photonmap um Pixelfootprint zu berechnen (für Photonsearchradius)
    //Radiosity um mit Pixelfootprint die Patchgröße zu bestimmen (Abbruchbedingung für die Patchunterteilungsfunktion)
    //Environmentlightsource benutzt den Kamera-Up-Vektor, um somit die Theta-0-Achse als Input zu haben
    //Importance-Light um zu sehen welche Photonen im Sichtbereich landen
    //SubpathSampler für die Erzeugung von Light-Subpahts (Für VertexConnection und Photonmaps)

    public class RayCamera : Camera, IRayCamera
    {
        private int screenWidth;
        private int screenHeight;
        private ImagePixelRange pixelRange;
        private float imagePlaneDistance;
        private float fieldOfViewX; //Öffnungswinkel in X-Richtung (Im Cosinus angegeben)
        private float fieldOfViewY; //Öffnungswinkel in Y-Richtung (Das ist die Angabe, welche man in Grad bei foY angibt)
        private Frame frame;
        
        private float distanceDephtOfFieldPlane;
        private float widthDephtOfField;
        private Vector3D leftUpperCorner; //Imageplange liegt im 1. Quadrat vom kartesischen Koordinatensystem. D.h. Pixelposition wird wie in WinForm-Fensterposition angegeben
        private const float pixelSizeOnImagePlane = 1.0f; //Die Breite==Höhe von ein einzelnen Pixel auf der Bildebene

        public int PixelCountFromScreen { get; private set; }

        public PixelSamplingMode SamplingMode { get; private set; }
        public bool DepthOfFieldIsEnabled { get; private set; }

        Vector3D IRayCamera.Position {get{ return base.Position; } }
        Vector3D IRayCamera.Forward { get { return base.Forward; } }
        Vector3D IRayCamera.Up { get { return base.Up; } }


        public bool UseCosAtCamera { get; private set; } //Sind die Kamera-Sensoren flach oder Kugelförmig angeordnet?

        public RayCamera(CameraConstructorData data)
            : base(data.Camera.Position, data.Camera.Forward, data.Camera.Up, data.Camera.OpeningAngleY)
        {
            //Der PixelPdfW-Filter liefert viel zu kleine Zahlen bei der Tent-Tiefenunschärfe, da ein Primärstrahl aufeinmal viel zu weit weg vom PixelCenter entfernt ist
            if (DepthOfFieldIsEnabled && data.SamplingMode == PixelSamplingMode.Tent) throw new Exception("Momentan darf der Tent-Filter bei Einsatz der Tiefenunschärfe nicht genutzt werden. Nutze den Equal-Filter so lange");

            this.SamplingMode = data.SamplingMode;
            this.screenWidth = data.ScreenWidth;
            this.screenHeight = data.ScreenHeight;
            this.pixelRange = data.PixelRange;
            this.distanceDephtOfFieldPlane = data.DistanceDephtOfFieldPlane;
            this.widthDephtOfField = data.WidthDephtOfField;
            this.DepthOfFieldIsEnabled = data.DepthOfFieldIsEnabled;
            this.imagePlaneDistance = this.GetImagePlaneDistance();
            this.PixelCountFromScreen = pixelRange.Width * pixelRange.Height; //So viele Light-Subpaths erzeugt das Lighttracing
            this.UseCosAtCamera = data.UseCosAtCamera;

            //Bildebene liegt in der X-Z-Ebene (Siehe Blender)
            this.leftUpperCorner = new Vector3D(-screenWidth * pixelSizeOnImagePlane / 2.0f, screenHeight * pixelSizeOnImagePlane / 2.0f, this.imagePlaneDistance);

            //Die Bezeichnungen für die 3 Vektoren entnehme ich den Frame-Konstruktorparameter
            //Ich Spanne mit meiner Linken Hand ein Koordinatensystem auf. Der Zeigefinger ist die Forward-Direction; Der Daumen der Up-Vektor; Der Mittelfinger die X-Achse
            Vector3D right = -Vector3D.Normalize(Vector3D.Cross(Up, Forward));
            Vector3D forward = Vector3D.Normalize(Forward);
            Vector3D up = Vector3D.Normalize(Vector3D.Cross(right, forward));
            this.frame = new Frame(right, up, forward);

            CalculateFieldOfViewPropertys();
        }

        //pix = Position wo im Pixel der Strahl erzeugt wird. Geht von -0.5 bis +0.5. pix=(0.0; 0.0) wäre die Pixelmitte
        public Ray CreatePrimaryRayWithPixi(int x, int y, Vector2D pix)
        {
            Vector3D worldSpaceDirection = this.frame.ToWorld(Vector3D.Normalize(this.leftUpperCorner + new Vector3D(x + pix.X + 0.5f, -y - pix.Y - 0.5f, 0) * pixelSizeOnImagePlane));
            return new Ray(this.Position, worldSpaceDirection);
        }

        //Die PixelPdfW entspricht der dem Pixel-Filter-Wert(Meine Bezeichnung) oder auch Measurement Contribution Function (Eric Veachs Bezeichnung)
        public float GetPixelPdfW(int x, int y, Vector3D primaryRayDirection)
        {
            switch (this.SamplingMode)
            {
                case PixelSamplingMode.None:
                    return 1;

                case PixelSamplingMode.Equal:
                    {
                        //PdfW für gleichmäßiges sampeln
                        //Idee: Ich berechne die PdfA des Pixels auf der Bildebene und rechne sie in eine PdfW mit *r² / cosAtCamera um.
                        float cosAtCamera = this.Forward * primaryRayDirection;
                        float distanceToPixel = this.imagePlaneDistance / cosAtCamera;
                        float pixelPdfA = 1.0f / (pixelSizeOnImagePlane * pixelSizeOnImagePlane);
                        return pixelPdfA * distanceToPixel * distanceToPixel / cosAtCamera;
                    }

                case PixelSamplingMode.Tent:
                    {
                        //PdfW für den Tent-Filter (Achtung: Es wird davon ausgegangen, das die Pixelfläche 1 groß ist)
                        Vector3D pixelCenterOnImagePlane = this.leftUpperCorner + new Vector3D(x + 0.5f, -y - 0.5f, 0) * pixelSizeOnImagePlane;

                        Vector3D localDirection = Vector3D.Normalize(this.frame.ToLocal(primaryRayDirection));
                        float cosAtCamera = new Vector3D(0, 0, 1) * localDirection;
                        //cosAtCamera = imagePlaneDistance / distanceToPixel
                        float distanceToPixel = this.imagePlaneDistance / cosAtCamera;
                        Vector3D pointOnImagePlane = localDirection * distanceToPixel;

                        float fx = pointOnImagePlane.X - pixelCenterOnImagePlane.X;
                        float fy = -(pointOnImagePlane.Y - pixelCenterOnImagePlane.Y);

                        if (Math.Abs(fx) >= 1 || Math.Abs(fy) >= 1) return MagicNumbers.MinAllowedPdfW;
                        //if (Math.Abs(fx) > 1.00000381f || Math.Abs(fy) > 1.00000381f) return 0; //Wenn beim Sampeln fx/fy==1 gesmpelt wurde, dann kommt hier aufgrund von float-Rundungsfehler eine 1.00000381 raus

                        float pdfX, pdfY;
                        if (fx <= 0) pdfX = 2 * fx + 2; else pdfX = 2 - 2 * fx;
                        if (fy <= 0) pdfY = 2 * fy + 2; else pdfY = 2 - 2 * fy;
                        float pixelPdfA = pdfX * pdfY / 4; //Die Fläche beim Tent-Sampling ist 4 mal so groß wie ein Pixel (Doppelte Kantenlänge)
                        return pixelPdfA * distanceToPixel * distanceToPixel / cosAtCamera;
                    }
            }

            throw new Exception("PixelSamplingMode " + this.SamplingMode + " nicht gefunden");
        }

        public Ray CreateRandomPrimaryRay(IRandom rand)
        {
            return CreatePrimaryRay((int)(rand.NextDouble() * this.screenWidth), (int)(rand.NextDouble() * this.screenHeight), rand);
        }

        public Ray CreatePrimaryRay(int x, int y, IRandom rand)
        {
            if (this.DepthOfFieldIsEnabled == false || rand == null)
            {
                Vector3D cameraSpaceDirection = CreatePrimaryRayDirectionInCameraSpace(x, y, rand);
                Vector3D worldSpaceDirection = Vector3D.Normalize(this.frame.ToWorld(cameraSpaceDirection));

                //GetPixelPdfW(x, y, worldSpaceDirection);

                return new Ray(this.Position, worldSpaceDirection);
            }
            else
            {
                Vector3D cameraSpaceDirection = CreatePrimaryRayDirectionInCameraSpace(x, y, rand);
                Vector3D worldSpaceDirection = Vector3D.Normalize(this.frame.ToWorld(cameraSpaceDirection));

                Vector3D punktAufDeepOfFieldEbene = this.Position + worldSpaceDirection * Math.Abs(this.distanceDephtOfFieldPlane);
                float doFFactor = 0.01f;//Um so mehr gegen 0, um so Schärfer ist das Bild, um so größer, um so mehr sieht man den Tiefenunschärfeeffekt
                float startVerschiebungslänge = Math.Abs(this.distanceDephtOfFieldPlane) * doFFactor / Math.Abs(this.widthDephtOfField);
                Vector3D startPoint = this.Position + Vector3D.Normalize(Vector3D.RotateVerticalDirectionAroundAxis(this.Up, this.Forward, (float)rand.NextDouble() * 360)) * startVerschiebungslänge;
                cameraSpaceDirection = Vector3D.Normalize(punktAufDeepOfFieldEbene - startPoint);

                return new Ray(startPoint, cameraSpaceDirection);
            }
        }

        private Vector3D CreatePrimaryRayDirectionInCameraSpace(int x, int y, IRandom rand)
        {
            Vector2D pix = rand != null ? SampleInsidePixel(rand) : new Vector2D(0, 0);

            Vector3D pointOnImagePlane = this.leftUpperCorner + new Vector3D(x + pix.X + 0.5f, -y - pix.Y - 0.5f, 0) * pixelSizeOnImagePlane;

            Vector3D cameraSpaceDirection = Vector3D.Normalize(pointOnImagePlane);
            return cameraSpaceDirection;
        }

        private Vector2D SampleInsidePixel(IRandom rand)
        {
            float fx = 0, fy = 0;

            switch (this.SamplingMode)
            {
                case PixelSamplingMode.None:
                    {
                        fx = 0;
                        fy = 0;
                        break;
                    }

                case PixelSamplingMode.Equal:
                    {
                        //Gleichmäßg sampeln
                        float r1 = (float)rand.NextDouble();
                        float r2 = (float)rand.NextDouble();
                        fx = r1 - 0.5f;
                        fy = r2 - 0.5f;
                        break;
                    }

                case PixelSamplingMode.Tent:
                    {
                        //Tent-Filter (Erzeuge mit größerer Wahrscheinlichkeit in der Mitte des Pixels ein Strahl) (Die Verteilungsfunktion ist ein Dreieck/Zelt). Deswegen Tent-Filter
                        float r1 = 2 * (float)rand.NextDouble();
                        float r2 = 2 * (float)rand.NextDouble();
                        fx = r1 < 1 ? (float)Math.Sqrt(r1) - 1 : 1 - (float)Math.Sqrt(2 - r1); //Erzeugt Zahl zwischen -1 und +1
                        fy = r2 < 1 ? (float)Math.Sqrt(r2) - 1 : 1 - (float)Math.Sqrt(2 - r2); //-1 + +1
                        break;
                    }
            }

            return new Vector2D(fx, fy);
        }

        //Es wird nur geschaut, ob Punkt in Sichtbereich liegt, wenn pixelRange==Bildschirmgröße. Wenn pixelRange nur ein Teilausschnitt sieht, dann liefert die Funktion hier trotzdem true
        public bool IsPointInVieldOfFiew(Vector3D point)
        {
            Vector3D toPointDirection = this.frame.ToLocal(Vector3D.Normalize(point - this.Position));

            Vector3D cameraCenterRay = new Vector3D(0, 0, 1); //Im lokalen Raum zeigt Forward immer per Definition in Z-Achse nach vorne
            if (toPointDirection * cameraCenterRay < 0) return false;
            float cosX = cameraCenterRay * Vector3D.Normalize(new Vector3D(toPointDirection.X, 0, toPointDirection.Z));
            float cosY = cameraCenterRay * Vector3D.Normalize(new Vector3D(0, toPointDirection.Y, toPointDirection.Z));

            return cosX >= this.fieldOfViewX && cosY >= this.fieldOfViewY;
        }
        private void CalculateFieldOfViewPropertys()
        {
            this.fieldOfViewX = (float)Math.Atan((this.screenWidth * pixelSizeOnImagePlane / 2) / GetImagePlaneDistance());
            this.fieldOfViewY = (float)((this.OpeningAngleY * Math.PI / 180.0) / 2);

            this.fieldOfViewX = (float)Math.Cos(this.fieldOfViewX);
            this.fieldOfViewY = (float)Math.Cos(this.fieldOfViewY);
        }

        private float GetImagePlaneDistance()
        {
            double foV = (this.OpeningAngleY * Math.PI / 180.0);               //Hier hat mir der Otze geholfen :-) 
            float cameraDistance = (float)((this.screenHeight * pixelSizeOnImagePlane) / (2 * Math.Tan(foV / 2)));

            return cameraDistance;
        }

        public Vector2D GetPixelPositionFromEyePoint(Vector3D point)
        {
            if (this.DepthOfFieldIsEnabled) throw new Exception("LightTracing darf bei Tiefenunschärfe momentan nicht verwendet werden");

            Vector3D toPointDirection = this.frame.ToLocal(Vector3D.Normalize(point - this.Position));

            Ray ray = new Ray(new Vector3D(0, 0, 0), toPointDirection);
            //if (ray.Richtung.Z >= 0) return null; //Punkt liegt entweder auf oder hinter der Bildebene und ist somit nicht sichtbar 
            Plane imagePlane = new Plane(new Vector3D(0, 0, 1), new Vector3D(0, 0, this.imagePlaneDistance));
            Vector3D p = imagePlane.GetIntersectionPointWithRay(ray);//x = -f*0.5 .. +f*0.5    y = -0.5 .. +0.5f            
            if (p == null) return null;

            var pixelPosition = new Vector2D((p.X / pixelSizeOnImagePlane + this.screenWidth / 2.0f), this.screenHeight - (p.Y / pixelSizeOnImagePlane + this.screenHeight / 2.0f));

            float tent = 0.5f; //Der Tentfilter geht noch um ein halben Pixel über den Rand hinaus
            if (this.SamplingMode != PixelSamplingMode.Tent) tent = 0;
            if (pixelPosition.X < this.pixelRange.XStart - tent || pixelPosition.Y < this.pixelRange.YStart - tent || pixelPosition.X > this.pixelRange.XStart + this.pixelRange.Width + tent || pixelPosition.Y > this.pixelRange.YStart + this.pixelRange.Height + tent) return null;


            return pixelPosition;
        }

        //Es wird der Pixel, über welchen man den 'point' sieht gerade auf point projetziert. Wenn an der Stelle die Fläche schräg ist, dann wird das nicht beachtet.
        public Vector2D GetPixelFootprintSize(Vector3D point)
        {
            Vector3D toPoint = point - this.Position;
            float toPointDistance = (new Plane(this.Forward, this.Position).GetOrthogonalDistanceFromPointToPlane(point));

            //distanceToPixel zu pixelSizeOnImagePlane ist das gleiche wie toPointDistance zu pixelFootprintEdge
            float distanceToPixel = this.imagePlaneDistance / (Vector3D.Normalize(toPoint) * this.Forward);
            float pixelFootprintEdge = toPointDistance * pixelSizeOnImagePlane / distanceToPixel;
            return new Vector2D(pixelFootprintEdge, pixelFootprintEdge);
        }
    }
}
