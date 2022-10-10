namespace GraphicMinimal
{
    public class Camera
    {
        public Vector3D Position { get; set; }
        public Vector3D Forward { get; set; }
        public Vector3D Up { get; set; }
        public float OpeningAngleY { get; set; }
        public float zNear { get; set; } = 0.001f;
        public float zFar { get; set; } = 3000;

        public Camera() { } //Für den Serialisizer

        public Camera(Camera copy)
        {
            this.Position = copy.Position;
            this.Forward = copy.Forward;
            this.Up = copy.Up;
            this.OpeningAngleY = copy.OpeningAngleY;
            this.zNear = copy.zNear;
            this.zFar = copy.zFar;
        }

        public Camera(Vector3D position, Vector3D forward, Vector3D up, float openingAngleY)
        {
            this.Position = position;
            this.Forward = Vector3D.Normalize(forward);
            this.Up = Vector3D.Normalize(up);
            this.OpeningAngleY = openingAngleY;
        }

        public Camera(Vector3D position, Vector3D forward, float openingAngleY)
            :this(position, forward, new Vector3D(0, 1, 0), openingAngleY)
        {
        }

        //Wenn ich die Kamera so angebe: new Camera(new Vector3D(0, 0, 0), new Vector3D(0, 0, -1), 45.0f);
        //Bekomme ich die Einheitsmatrix bei dieser Funktion: 
        //float[] cameraMatrix=Matrix4x4.LookAt(camera.Position, camera.Forward, camera.Up).Values
        //Das Weltkoordinatensystem=Eyekoordinatensystem ist dann so das die X-Achse von Fenster-Links nach Fenster-Rechts geht und die Y-Achse von Fenster-unten nach Fenster-oben geht.
        //Objekte die nah sind haben die Z-Koordinate -1 und Objekte die weiter weg sind dann z.B. -10
        public Camera(float openingAngleY) //Einheitsmatrix bei der LookAtMatrix
            : this(new Vector3D(0, 0, 0), new Vector3D(0, 0, -1), openingAngleY)
        {
        }
    }
}
