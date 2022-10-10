using GraphicGlobal;
using GraphicMinimal;
using RaytracingColorEstimator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TriangleObjectGeneration;

namespace RadiosityTest
{
    class BoxTestScene
    {
        public static RaytracingFrame3DData CreateScene()
        {
            int screenWidth = 100;
            int screenHeight = 100;

            float surfaceAlbedo = 0.3f;
            float sizeFactor = 10;
            double groundSize = 0.9;       //Die Platten dürfen sich an den Kanten/Ecken nicht direkt berühren, da sonst der GeometryTerm gegen unendlich geht (Division durch Quadrad-Abstand)     
            float groundSizeF = (float)groundSize; //groundSize muss double sein, da nur so die Vorgabe-LightSourceArea mit der Ist-LightSourceArea übereinstimmt
            float emission = 50 * sizeFactor * sizeFactor;


            List<DrawingObject> drawingObjects = new List<DrawingObject>
            {
                new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(0, 0, -1) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(0, 0, 0), TextureFile = "#B2B2B2", NormalInterpolation = InterpolationMode.Flat, Albedo = surfaceAlbedo }),//Fußboden
                new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(0, 1, 0) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(90, 0, 0), TextureFile = "#B2B2B2", NormalInterpolation = InterpolationMode.Flat, Albedo = surfaceAlbedo }),//Rückwand
                new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(-1, 0, 0) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(0, 90, 0), TextureFile = "#B23333", NormalInterpolation = InterpolationMode.Flat, Albedo = surfaceAlbedo }),//Linke Wand
                new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(+1, 0, 0) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(0, -90, 0), TextureFile = "#33B233", NormalInterpolation = InterpolationMode.Flat, Albedo = surfaceAlbedo }),//Rechte Wand
                new DrawingObject(TriangleObjectGenerator.CreateSquareXY(groundSizeF, groundSizeF, 1), new ObjectPropertys() { Position = new Vector3D(0, 0, +1) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(180, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = surfaceAlbedo, RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = emission } })//Decke
            };

            double imagePlaneSize = groundSize * 2;
            float imagePlaneDistance = 1.0f;
            float foV = (float)(Math.Atan(imagePlaneSize / 2 / imagePlaneDistance) / (2 * Math.PI) * 360) * 2;
            var camera = new Camera(new Vector3D(0, -(groundSizeF + imagePlaneDistance), 0) * sizeFactor, new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), foV);

            return new RaytracingFrame3DData()
            {
                GlobalObjektPropertys = new GlobalObjectPropertys()
                {
                    Camera = camera
                },
                DrawingObjects = drawingObjects,
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight,
                ProgressChanged = (s, f) => { },
                StopTrigger = new CancellationTokenSource(),
                PixelRange = new ImagePixelRange(screenWidth / 2, screenHeight / 2, 1, 1)
            };
        }
    }
}
