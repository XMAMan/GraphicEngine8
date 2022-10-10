using System;
using System.Collections.Generic;
using System.Text;
using GraphicMinimal;
using System.Drawing;
using BitmapHelper;
using GraphicGlobal;

namespace TriangleObjectGeneration
{
    public static class TriangleObjectGenerator
    {
        //smallRadius, resolution1=für kleinen Kreis, bigRadius, resolution2=für großen Kreis
        //Beispiel: CreateHelix(0.05f, 0.5f, 3, 6, 4, 20)
        public static TriangleObject CreateHelix(float smallRadius, float bigRadius, float height, float numberOfTurns, bool cap, int resolution1, int resolution2)
        {
            string callingParameters = "CreateHelix" + ":" + smallRadius + ":" + bigRadius + ":" + height + ":" + numberOfTurns + ":" + cap + ":" + resolution1 + ":" + resolution2;

            List<Vector3D> path = new List<Vector3D>();
            List<Vector2D> shape = new List<Vector2D>();
            for (int i = 0; i < resolution1; i++)
                shape.Add(new Vector2D((float)(Math.Cos((float)(i) / resolution1 * 2 * Math.PI)) * smallRadius, (float)(Math.Sin((float)(i) / resolution1 * 2 * Math.PI)) * smallRadius));
            for (float f = 0; f <= 2 * Math.PI * numberOfTurns; f += 2 * (float)Math.PI / resolution2)
                path.Add(new Vector3D((float)(Math.Cos(f)) * bigRadius, f / (2 * (float)Math.PI * numberOfTurns) * height - height / 2, (float)(Math.Sin(f)) * bigRadius));
            TriangleList retObj = ExtrudeAndRotate.CreateExtrusionObject(path, shape, cap);
            retObj.Name = callingParameters;

            return retObj.GetTriangleObject();
        }

        //Beispiel: CreateSphere(1, 10, 10); 

        public static TriangleObject CreateSphere(float radius, int resolution1, int resolution2)
        {
            if (radius == 0 || resolution1 < 2 || resolution2 < 2) return null;
            string callingParameters = "CreateSphere:" + radius + ":" + resolution1 + ":" + resolution2;

            List<Vector2D> shape = new List<Vector2D>();
            for (int i = 0; i <= resolution1; i++)
                shape.Add(new Vector2D((float)(Math.Sin((float)(i) / resolution1 * (float)Math.PI)) * radius, (float)(Math.Cos((float)(i) / resolution1 * (float)Math.PI)) * radius));
            TriangleList retObj = ExtrudeAndRotate.CreateRotationObject(shape, new Vector3D(0, 0, 0), new Vector3D(0, -1, 0), 0, 360, resolution2);

            retObj.Name = callingParameters;

            return retObj.GetTriangleObject();
        }

        //smallRadius=kleiner Ring, bigRadius=großer Ring
        //Beispiel: CreateRing(0.3f, 2, 5, 20);
        public static TriangleObject CreateRing(float smallRadius, float bigRadius, int resolution1, int resolution2)
        {
            string callingParameters = "CreateRing:" + smallRadius + ":" + bigRadius + ":" + resolution1 + ":" + resolution2;

            List<Vector2D> shape = new List<Vector2D>();
            for (int i = 0; i <= resolution1; i++)
                shape.Add(new Vector2D((float)(Math.Cos((float)(i) / resolution1 * 2 * (float)Math.PI)) * smallRadius - bigRadius + smallRadius / 2, (float)(Math.Sin((float)(i) / resolution1 * 2 * (float)Math.PI)) * smallRadius - bigRadius + smallRadius / 2));
            TriangleList retObj = ExtrudeAndRotate.CreateRotationObject(shape, new Vector3D(0, 0, 0), new Vector3D(0, -1, 0), 0, 360, resolution2);
            retObj.Name = callingParameters;

            return retObj.GetTriangleObject();
        }

        public static List<TriangleObject> CreateCornellBox()
        {
            TriangleList grayWalls = new TriangleList();
            grayWalls.Name = "CornellBox:GrayWalls";

            grayWalls.AddTriangle(new Vertex(0.556f, 0.549f, -0.559f, 1, 1), new Vertex(0.000f, 0.549f, -0.559f, 0, 1), new Vertex(0.556f, 0.000f, -0.559f, 1, 0));//Rückwand
            grayWalls.AddTriangle(new Vertex(0.006f, 0.000f, -0.559f, 0, 0), new Vertex(0.556f, 0.000f, -0.559f, 1, 0), new Vertex(0.000f, 0.549f, -0.559f, 0, 1));

            grayWalls.AddTriangle(new Vertex(0.556f, 0.549f, -0.000f, 0, 0), new Vertex(0.000f, 0.549f, -0.000f, 1, 0), new Vertex(0.556f, 0.549f, -0.559f, 0, 1));//Decke
            grayWalls.AddTriangle(new Vertex(0.000f, 0.549f, -0.559f, 1, 1), new Vertex(0.556f, 0.549f, -0.559f, 0, 1), new Vertex(0.000f, 0.549f, -0.000f, 1, 0));

            TriangleList ground = new TriangleList();
            ground.Name = "CornellBox:Ground";

            ground.AddTriangle(new Vertex(0.556f, 0.000f, -0.559f, 1, 1), new Vertex(0.006f, 0.000f, -0.559f, 0, 1), new Vertex(0.556f, 0.000f, -0.000f, 1, 0));//Fußboden
            ground.AddTriangle(new Vertex(0.003f, 0.000f, -0.000f, 0, 0), new Vertex(0.556f, 0.000f, -0.000f, 1, 0), new Vertex(0.006f, 0.000f, -0.559f, 0, 1));

            TriangleList leftWall = new TriangleList();
            leftWall.Name = "CornellBox:LeftWall";

            leftWall.AddTriangle(new Vertex(0.000f, 0.549f, -0.559f, 0, 1), new Vertex(0.000f, 0.549f, -0.000f, 0, 0), new Vertex(0.006f, 0.000f, -0.559f, 1, 1));//Linke Wand
            leftWall.AddTriangle(new Vertex(0.003f, 0.000f, -0.000f, 1, 0), new Vertex(0.006f, 0.000f, -0.559f, 1, 1), new Vertex(0.000f, 0.549f, -0.000f, 0, 0));

            TriangleList rightWall = new TriangleList();
            rightWall.Name = "CornellBox:RightWall";

            rightWall.AddTriangle(new Vertex(0.556f, 0.549f, -0.000f, 1, 0), new Vertex(0.556f, 0.549f, -0.559f, 1, 1), new Vertex(0.556f, 0.000f, -0.000f, 0, 0));//Rechte Wand
            rightWall.AddTriangle(new Vertex(0.556f, 0.000f, -0.559f, 0, 1), new Vertex(0.556f, 0.000f, -0.000f, 0, 0), new Vertex(0.556f, 0.549f, -0.559f, 1, 1));

            TriangleList light = new TriangleList();
            light.Name = "CornellBox:Light";

            light.AddTriangle(new Vertex(0.343f, 0.545f, -0.227f), new Vertex(0.213f, 0.545f, -0.227f), new Vertex(0.343f, 0.545f, -0.332f));//Lampe
            light.AddTriangle(new Vertex(0.213f, 0.545f, -0.332f), new Vertex(0.343f, 0.545f, -0.332f), new Vertex(0.213f, 0.545f, -0.227f));

            TriangleList rightCube = new TriangleList();
            rightCube.Name = "CornellBox:RightCube";

            rightCube.AddTriangle(new Vertex(0.316f, 0.165f, -0.272f), new Vertex(0.426f, 0.165f, -0.065f), new Vertex(0.474f, 0.165f, -0.225f));//Rechter Würfel Oben
            rightCube.AddTriangle(new Vertex(0.426f, 0.165f, -0.065f), new Vertex(0.316f, 0.165f, -0.272f), new Vertex(0.266f, 0.165f, -0.114f));

            rightCube.AddTriangle(new Vertex(0.266f, 0.000f, -0.114f), new Vertex(0.266f, 0.165f, -0.114f), new Vertex(0.316f, 0.165f, -0.272f));//Rechter Würfel Links
            rightCube.AddTriangle(new Vertex(0.316f, 0.000f, -0.272f), new Vertex(0.266f, 0.000f, -0.114f), new Vertex(0.316f, 0.165f, -0.272f));

            rightCube.AddTriangle(new Vertex(0.474f, 0.165f, -0.225f), new Vertex(0.316f, 0.165f, -0.272f), new Vertex(0.316f, 0.000f, -0.272f));//Rechter Würfel Hinten
            rightCube.AddTriangle(new Vertex(0.474f, 0.165f, -0.225f), new Vertex(0.316f, 0.000f, -0.272f), new Vertex(0.474f, 0.000f, -0.225f));

            rightCube.AddTriangle(new Vertex(0.474f, 0.000f, -0.225f), new Vertex(0.474f, 0.165f, -0.225f), new Vertex(0.426f, 0.165f, -0.065f));//Rechter Würfel rechts
            rightCube.AddTriangle(new Vertex(0.426f, 0.165f, -0.065f), new Vertex(0.426f, 0.000f, -0.065f), new Vertex(0.474f, 0.000f, -0.225f));

            rightCube.AddTriangle(new Vertex(0.426f, 0.000f, -0.065f), new Vertex(0.426f, 0.165f, -0.065f), new Vertex(0.266f, 0.165f, -0.114f));//Rechter Würfel vorne
            rightCube.AddTriangle(new Vertex(0.266f, 0.165f, -0.114f), new Vertex(0.266f, 0.000f, -0.114f), new Vertex(0.426f, 0.000f, -0.065f));

            rightCube.AddTriangle(new Vertex(0.474f, 0.000f, -0.225f), new Vertex(0.316f, 0.000f, -0.272f), new Vertex(0.426f, 0.000f, -0.065f));//Rechter Würfel Unten
            rightCube.AddTriangle(new Vertex(0.266f, 0.000f, -0.114f), new Vertex(0.426f, 0.000f, -0.065f), new Vertex(0.316f, 0.000f, -0.272f));

            TriangleList leftCube = new TriangleList();
            leftCube.Name = "CornellBox:LuftCube";

            leftCube.AddTriangle(new Vertex(0.133f, 0.330f, -0.247f), new Vertex(0.291f, 0.330f, -0.296f), new Vertex(0.242f, 0.330f, -0.456f));//Linker Würfel Oben
            leftCube.AddTriangle(new Vertex(0.242f, 0.330f, -0.456f), new Vertex(0.084f, 0.330f, -0.406f), new Vertex(0.133f, 0.330f, -0.247f));

            leftCube.AddTriangle(new Vertex(0.133f, 0.000f, -0.247f), new Vertex(0.133f, 0.330f, -0.247f), new Vertex(0.084f, 0.330f, -0.406f));//Linker Würfel Links
            leftCube.AddTriangle(new Vertex(0.084f, 0.330f, -0.406f), new Vertex(0.084f, 0.000f, -0.406f), new Vertex(0.133f, 0.000f, -0.247f));

            leftCube.AddTriangle(new Vertex(0.084f, 0.000f, -0.406f), new Vertex(0.084f, 0.330f, -0.406f), new Vertex(0.242f, 0.330f, -0.456f));//Linker Würfel Hinten
            leftCube.AddTriangle(new Vertex(0.242f, 0.330f, -0.456f), new Vertex(0.242f, 0.000f, -0.456f), new Vertex(0.084f, 0.000f, -0.406f));

            leftCube.AddTriangle(new Vertex(0.291f, 0.330f, -0.296f), new Vertex(0.242f, 0.330f, -0.456f), new Vertex(0.242f, 0.000f, -0.456f));//Linker Würfel Rechts
            leftCube.AddTriangle(new Vertex(0.242f, 0.000f, -0.456f), new Vertex(0.291f, 0.000f, -0.296f), new Vertex(0.291f, 0.330f, -0.296f));

            leftCube.AddTriangle(new Vertex(0.291f, 0.000f, -0.296f), new Vertex(0.291f, 0.330f, -0.296f), new Vertex(0.133f, 0.330f, -0.247f));//Linker Würfel vorne
            leftCube.AddTriangle(new Vertex(0.133f, 0.330f, -0.247f), new Vertex(0.133f, 0.000f, -0.247f), new Vertex(0.291f, 0.000f, -0.296f));
            
            grayWalls.SetNormals();
            ground.SetNormals();
            leftWall.SetNormals();
            rightWall.SetNormals();
            light.SetNormals();
            rightCube.SetNormals();
            leftCube.SetNormals();

            var box = new List<TriangleObject>() { grayWalls.GetTriangleObject(), ground.GetTriangleObject(), leftWall.GetTriangleObject(), rightWall.GetTriangleObject(), light.GetTriangleObject(), rightCube.GetTriangleObject(), leftCube.GetTriangleObject() };
            return box;
        }

        public static TriangleObject CreateCube(float xSize, float ySize, float zSize)
        {
            string callingParameters = "CreateCube:" + xSize + ":" + ySize + ":" + zSize;

            TriangleList newObject = new TriangleList();
            newObject.AddQuad(new Vertex(-xSize, ySize, zSize, 0, 1), new Vertex(xSize, ySize, zSize, 1, 1), new Vertex(xSize, ySize, -zSize, 1, 0), new Vertex(-xSize, ySize, -zSize, 0, 0));//Oberseite
            newObject.AddQuad(new Vertex(-xSize, -ySize, -zSize, 0, 1), new Vertex(xSize, -ySize, -zSize, 1, 1), new Vertex(xSize, -ySize, zSize, 1, 0), new Vertex(-xSize, -ySize, zSize, 0, 0));//Unterseite
            newObject.AddQuad(new Vertex(-xSize, -ySize, zSize, 0, 1), new Vertex(xSize, -ySize, zSize, 1, 1), new Vertex(xSize, ySize, zSize, 1, 0), new Vertex(-xSize, ySize, zSize, 0, 0)); //Vorderseite
            newObject.AddQuad(new Vertex(-xSize, ySize, -zSize, 1, 0), new Vertex(xSize, ySize, -zSize, 0, 0), new Vertex(xSize, -ySize, -zSize, 0, 1), new Vertex(-xSize, -ySize, -zSize, 1, 1)); //Rückseite
            newObject.AddQuad(new Vertex(xSize, -ySize, zSize, 0, 1), new Vertex(xSize, -ySize, -zSize, 1, 1), new Vertex(xSize, ySize, -zSize, 1, 0), new Vertex(xSize, ySize, zSize, 0, 0));//Rechts
            newObject.AddQuad(new Vertex(-xSize, -ySize, -zSize, 0, 1), new Vertex(-xSize, -ySize, zSize, 1, 1), new Vertex(-xSize, ySize, zSize, 1, 0), new Vertex(-xSize, ySize, -zSize, 0, 0));//Links

            newObject.SetNormals();
            newObject.Name = callingParameters;

            return newObject.GetTriangleObject();
        }

        public static TriangleObject CreateSquareXY(float xSize, float ySize, int separations)
        {
            string callingParameters = "CreateSquareXY:" + xSize + ":" + ySize + ":" + separations;

            TriangleList newObject = new TriangleList();
            float dx = xSize * 2 / separations, dy = ySize * 2 / separations;
            for (float x = -xSize; x < xSize; x += dx)
                for (float y = -ySize; y < ySize; y += dy)
                    newObject.AddQuad(new Vertex(x, y, 0, (x + xSize) / (xSize * 2), 1 - (y + ySize) / (ySize * 2)), new Vertex(x + dx, y, 0, (x + dx + xSize) / (xSize * 2), 1 - (y + ySize) / (ySize * 2)), new Vertex(x + dx, y + dy, 0, (x + dx + xSize) / (xSize * 2), 1 - (y + dy + ySize) / (ySize * 2)), new Vertex(x, y + dy, 0, (x + xSize) / (xSize * 2), 1 - (y + dy + ySize) / (ySize * 2)));//Oberseite
    
            newObject.SetNormals();
            newObject.Name = callingParameters;

            return newObject.GetTriangleObject();
        }

        public static TriangleObject CreateSquareXZ(float xSize, float zSize, int separations)
        {
            string callingParameters = "CreateSquareXZ:" + xSize + ":" + zSize + ":" + separations;

            TriangleList newObject = new TriangleList();
            float dx = xSize * 2 / separations, dz = zSize * 2 / separations;
            for (float x = -xSize; x < xSize; x += dx)
                for (float z = -zSize; z < zSize; z += dz)
                    newObject.AddQuad(new Vertex(x, 0, z, (x + xSize) / (xSize * 2), 1 - (z + zSize) / (zSize * 2)), new Vertex(x + dx, 0, z, (x + dx + xSize) / (xSize * 2), 1 - (z + zSize) / (zSize * 2)), new Vertex(x + dx, 0, z + dz, (x + dx + xSize) / (xSize * 2), 1 - (z + dz + zSize) / (zSize * 2)), new Vertex(x, 0, z + dz, (x + xSize) / (xSize * 2), 1 - (z + dz + zSize) / (zSize * 2)));//Oberseite Normale =(0,-1,0)

            newObject.SetNormals();
            newObject.Name = callingParameters;

            return newObject.GetTriangleObject();
        }

        //Beispiel: CreateTorch(0.2f, 5);
        public static TriangleObject CreateTorch(float size, int resolution)
        {
            string callingParameters = "CreateTorch:" + size + ":" + resolution;

            List<Vector2D> shape = new List<Vector2D>();
            shape.Add(new Vector2D(0, 0));
            shape.Add(new Vector2D(size, size));
            shape.Add(new Vector2D(size, size * 20));
            shape.Add(new Vector2D(size * 3, size * 23));
            TriangleList retObj = ExtrudeAndRotate.CreateRotationObject(shape, new Vector3D(0, 0, 0), new Vector3D(0, 1, 0), 0, 360, resolution);
            retObj.Name = callingParameters;

            return retObj.GetTriangleObject();
        }

        //Erzeugt eine gerillte Säule mit der Höhe "height" und dem Außenradius "bigRadius" und der Rillengröße "smallRadius"
        //Beispiel: CreatePillar(5, 1, 0.08f, false, true, 50);
        public static TriangleObject CreatePillar(float height, float bigRadius, float smallRadius, bool capBottom, bool capTop, int resolution)
        {
            string callingParameters = "CreatePillar:" + height + ":" + bigRadius + ":" + smallRadius + ":" + capBottom + ":" + capTop + ":" + resolution;

            if (resolution <= 0) resolution = 1;
            if (smallRadius > bigRadius) smallRadius = bigRadius;
            List<Vector3D> path = new List<Vector3D>();
            List<Vector2D> shape = new List<Vector2D>();

            path.Add(new Vector3D(0, 0, 0));
            path.Add(new Vector3D(0, height, 0));

            float r, angle;
            for (int i = 0; i < resolution; i++)
            {
                if (smallRadius > 0) 
                    r = bigRadius + (float)Math.Sin(i * 2.0 * Math.PI * bigRadius / smallRadius / resolution) * smallRadius;
                else 
                    r = bigRadius;
                angle = (float)(i * 2.0 * Math.PI / resolution);
                shape.Add(new Vector2D((float)Math.Cos(angle) * r, (float)Math.Sin(angle) * r));
            }

            TriangleList middleSection = ExtrudeAndRotate.CreateExtrusionObject(path, shape, false);

            if (capBottom)
            {
                shape.Clear();
                shape.Add(new Vector2D(0, 0));
                shape.Add(new Vector2D(bigRadius * 1.4f, 0));
                shape.Add(new Vector2D(bigRadius * 1.3f, height / 20.0f));
                shape.Add(new Vector2D(bigRadius * 0.8f, height / 10.0f));
                TriangleList bottom = ExtrudeAndRotate.CreateRotationObject(shape, new Vector3D(0, 0, 0), new Vector3D(0, 1, 0), 0, 360, resolution);
                middleSection = ExtrudeAndRotate.MergeObjects(middleSection, bottom, new Vector3D(0, -height / 2.0f + height / 20.0f, 0));
            }
            if (capTop)
            {
                shape.Clear();
                shape.Add(new Vector2D(0, 0));
                shape.Add(new Vector2D(bigRadius * 1.4f, 0));
                shape.Add(new Vector2D(bigRadius * 1.3f, -height / 20.0f));
                shape.Add(new Vector2D(bigRadius * 0.8f, -height / 10.0f));
                TriangleList top = ExtrudeAndRotate.CreateRotationObject(shape, new Vector3D(0, 0, 0), new Vector3D(0, 1, 0), 0, 360, resolution);
                middleSection = ExtrudeAndRotate.MergeObjects(middleSection, top, new Vector3D(0, height / 2.0f - height / 20.0f, 0));
            }

            middleSection.Name = callingParameters;

            return middleSection.GetTriangleObject();
        }

        //Erzeugt ein Gitter, wo die Gitterstäbe den Radius "Radius" haben. 
        //Beispiel: CreateLatice(5, 3, 0.1f, 4, 5, 5);
        public static TriangleObject CreateLatice(float height, float width, float radius, int numberOfBarsX, int numberOfBarsY, int resolution)
        {
            string callingParameters = "CreateLatice:" + height + ":" + width + ":" + radius + ":" + numberOfBarsX + ":" + numberOfBarsY + ":" + resolution;

            List<Vector3D> pathY = new List<Vector3D>();
            List<Vector3D> pathX = new List<Vector3D>();
            List<Vector2D> shapeY = new List<Vector2D>();
            for (int i = 0; i < resolution; i++) shapeY.Add(new Vector2D((float)Math.Cos(i * 2.0f * Math.PI / resolution) * radius, (float)Math.Sin(i * 2.0f * Math.PI / resolution) * radius));
            List<Vector2D> shapeX = new List<Vector2D>();
            for (int i = 0; i < 4; i++) shapeX.Add(new Vector2D((float)Math.Cos(i * 2.0f * Math.PI / resolution) * radius, (float)Math.Sin(i * 2.0f * Math.PI / resolution) * radius));
            pathY.Add(new Vector3D(0, 0, 0));
            pathY.Add(new Vector3D(0, height, 0));
            pathX.Add(new Vector3D(0, 0, 0));
            pathX.Add(new Vector3D(width, 0, 0));
            TriangleList yBar = ExtrudeAndRotate.CreateExtrusionObject(pathY, shapeY, false);
            TriangleList xBar = ExtrudeAndRotate.CreateExtrusionObject(pathX, shapeX, false);
            TriangleList latice = new TriangleList(yBar);
            for (int i = 1; i < numberOfBarsX - 1; i++) latice = ExtrudeAndRotate.MergeObjects(latice, yBar, new Vector3D(i * width / numberOfBarsX, 0, 0));
            for (int i = 1; i < numberOfBarsY; i++) latice = ExtrudeAndRotate.MergeObjects(latice, xBar, new Vector3D(+width / 2.0f - width / numberOfBarsX, i * height / numberOfBarsY - height / 2.0f, 0));
            latice.TransformToCoordinateOrigin();

            latice.Name = callingParameters;

            return latice.GetTriangleObject();
        }

        //Beispiel: CreateCylinder(5, 1, 1, 4);
        public static TriangleObject CreateCylinder(float height, float radiusBottom, float radiusTop, bool cap, int resolution)
        {
            string callingParameters = "CreateCylinder:" + height + ":" + radiusBottom + ":" + radiusTop + ":" + cap + ":" + resolution;

            List<Vector2D> shape = new List<Vector2D>();
            if (cap) shape.Add(new Vector2D(0, height));
            shape.Add(new Vector2D(radiusTop, height));
            shape.Add(new Vector2D(radiusBottom, 0));
            if (cap) shape.Add(new Vector2D(0, 0));
            TriangleList retObj = ExtrudeAndRotate.CreateRotationObject(shape, new Vector3D(0, 0, 0), new Vector3D(0, -1, 0), 0, 360, resolution);
            retObj.Name = callingParameters;

            return retObj.GetTriangleObject();
        }

        //Beispiel: CreateBottle(1, 2, 6);
        public static TriangleObject CreateBottle(float radius, float length, int resolution)
        {
            string callingParameters = "CreateBottle:" + radius + ":" + length + ":" + resolution;

            List<Vector2D> shape = new List<Vector2D>();
            for (int i = 0; i < resolution; i++) shape.Add(new Vector2D(radius / 2.0f + (float)Math.Cos(i * Math.PI / resolution - Math.PI / 4.0f) * radius, (float)Math.Sin(i * Math.PI / resolution - Math.PI / 4.0f) * radius));
            shape.Add(new Vector2D(radius / 2.0f, radius * length));
            TriangleList retObj = ExtrudeAndRotate.CreateRotationObject(shape, new Vector3D(0, 0, 0), new Vector3D(0, 1, 0), 0, 360, resolution);
            retObj.Name = callingParameters;

            return retObj.GetTriangleObject();
        }

        //Beispiel: CreateSword(4, 5);
        public static TriangleObject CreateSword(float length, int resolution)
        {
            string callingParameters = "CreateSword:" + length + ":" + resolution;

            List<Vector2D> shape = new List<Vector2D>();
            shape.Add(new Vector2D(0, -length / 10.0f));
            shape.Add(new Vector2D(length / 100.0f, -length / 10.0f + length / 100.0f));
            shape.Add(new Vector2D(length / 100.0f, -length / 100.0f));
            shape.Add(new Vector2D(length / 12.0f + length / 100.0f, -length / 100.0f));
            shape.Add(new Vector2D(length / 12.0f + length / 100.0f, length / 100.0f));
            shape.Add(new Vector2D(length / 100.0f, length / 100.0f));
            shape.Add(new Vector2D(length / 100.0f, length - length / 100.0f));
            shape.Add(new Vector2D(0, length));
            for (int i = 0; i < shape.Count; i++) shape[i].Y = -shape[i].Y;
            TriangleList retObj = ExtrudeAndRotate.CreateRotationObject(shape, new Vector3D(0, 0, 0), new Vector3D(0, -1, 0), 0, 360, resolution);
            retObj.Move(new Vector3D(0, -length / 2.0f + length / 20.0f, 0));
            retObj.Name = callingParameters;

            return retObj.GetTriangleObject();
        }

        //Beispiel: CreateSkewer(4, 3);
        public static TriangleObject CreateSkewer(float length, int resolution)
        {
            string callingParameters = "CreateSkewer:" + length + ":" + resolution;

            List<Vector2D> shape = new List<Vector2D>();
            shape.Add(new Vector2D(length / 100, 0));
            shape.Add(new Vector2D(0, length));
            TriangleList retObj = ExtrudeAndRotate.CreateRotationObject(shape, new Vector3D(0, 0, 0), new Vector3D(0, 1, 0), 0, 360, resolution);
            retObj.Name = callingParameters;

            return retObj.GetTriangleObject();
        }

        //versetzungsfaktor muss zwischen -1 und 1 liegen 
        //Beispiel: CreateSaw(3, 3, 1, 5, 1);
        public static TriangleObject CreateSaw(float width, float height, float depth, int numberOfSpikes, float displacementFactor)
        {
            string callingParameters = "CreateSaw:" + width + ":" + height + ":" + depth + ":" + numberOfSpikes + ":" + displacementFactor;

            TriangleList newObject = new TriangleList();

            if (displacementFactor < -1) displacementFactor = -1;
            if (displacementFactor > 1) displacementFactor = 1;
            float l = width / numberOfSpikes; //Breite einer Zacke
            float h = l * l * 0.75f;           //Höhe einer Zacke
            float d = l * displacementFactor; //um diesen Wert wird jede Zacke nach rechts/links verschoben

            newObject.AddQuad(new Vertex(0, 0, 0, 0, 0),
                                  new Vertex(width, 0, 0, 1, 0),
                                  new Vertex(width, height - h, 0, 1, (height - h) / height),
                                  new Vertex(0, height - h, 0, 0, (height - h) / height));

            int I1 = 0, I2 = 0;
            if (displacementFactor < 0) { I1 = +1; I2 = -1; }
            if (displacementFactor > 0) { I1 = -1; I2 = +1; }
            for (int i = I1; i < numberOfSpikes + I2; i++)
            {
                float x1 = l * i + d, y1 = height - h;
                float x2 = l * (i + 1) + d, y2 = height - h;
                float x3 = l * i + l / 2.0f + d, y3 = height;
                if (x1 < 0) x1 = 0; if (x1 > width) x1 = width;
                if (x2 < 0) x2 = 0; if (x2 > width) x2 = width;
                if (x3 < 0) x3 = 0; if (x3 > width) x3 = width;
                newObject.AddTriangle(new Vertex(x1, y1, 0, x1 / width, y1 / height),
                                      new Vertex(x2, y2, 0, x2 / width, y2 / height),
                                      new Vertex(x3, y3, 0, x3 / width, y3 / height));
            }
            newObject.AddQuad(new Vertex(0, 0, depth / 2.0f, 0, 0),
                              new Vertex(0, 0, -depth / 2.0f, 1, 0),
                              new Vertex(0, height - h, -depth / 2.0f, 1, 1),
                              new Vertex(0, height - h, depth / 2.0f, 0, 1));
            newObject.AddQuad(new Vertex(width, 0, depth / 2.0f, 0, 0),
                              new Vertex(width, 0, -depth / 2.0f, 1, 0),
                              new Vertex(width, height - h, -depth / 2.0f, 1, 1),
                              new Vertex(width, height - h, depth / 2.0f, 0, 1));

            newObject.SetNormals();
            newObject.Name = callingParameters;

            return newObject.GetTriangleObject();
        }

        //Beispiel: CreateMirrorFrame(5, 6, 0.5f, 5); 
        public static TriangleObject CreateMirrorFrame(float width, float height, float radius, int resolution)
        {
            string callingParameters = "CreateMirrorFrame:" + width + ":" + height + ":" + radius + ":" + resolution;

            List<Vector3D> path1 = new List<Vector3D>();
            List<Vector3D> path2 = new List<Vector3D>();
            List<Vector3D> path3 = new List<Vector3D>();

            List<Vector2D> shape = new List<Vector2D>();
            float r, angle, groovesRadius = radius / 15.0f;
            for (int i = 0; i < resolution; i++)
            {
                if (groovesRadius > 0) 
                    r = radius + (float)Math.Sin(i * 2.0 * Math.PI * radius / groovesRadius / resolution) * groovesRadius;
                else 
                    r = radius;
                angle = (float)(i * 2.0 * Math.PI / resolution);
                shape.Add(new Vector2D((float)Math.Cos(angle) * r, (float)Math.Sin(angle) * r));
            }
            path1.Add(new Vector3D(0, 0, 0));
            path1.Add(new Vector3D(0, height - radius, 0));

            path2.Add(new Vector3D(0, height - radius, 0));
            path2.Add(new Vector3D(width - 2 * radius, height - radius, 0));

            path3.Add(new Vector3D(width - 2 * radius, height - radius, 0));
            path3.Add(new Vector3D(width - 2 * radius, 0, 0));

            TriangleList newObject = new TriangleList();//Spiegelviereck
            newObject.AddQuad(new Vertex(radius - width / 2.0f, -height / 2.0f, 0, 0, 0), new Vertex(width - radius * 2 - width / 2.0f, -height / 2.0f, 0, 1, 0), new Vertex(width - radius * 2 - width / 2.0f, height - radius * 2 - height / 2.0f, 0, 1, 1), new Vertex(radius - width / 2.0f, height - radius * 2 - height / 2.0f, 0, 0, 1));
            newObject.SetNormals();

            TriangleList Obj1 = ExtrudeAndRotate.MergeObjects(ExtrudeAndRotate.CreateExtrusionObject(path1, shape, false), ExtrudeAndRotate.CreateExtrusionObject(path3, shape, false), new Vector3D(width - 2 * radius, 0, 0));
            TriangleList Obj2 = ExtrudeAndRotate.MergeObjects(Obj1, ExtrudeAndRotate.CreateExtrusionObject(path2, shape, false), new Vector3D((width - 2 * radius) / 2.0f, (height - radius) / 2.0f, 0));
            Obj2.TransformToCoordinateOrigin();

            Obj2.Name = callingParameters;

            return Obj2.GetTriangleObject();
        }

        public static TriangleObject CreatePerlinNoiseHeightmap(int width, int height, float bumpFactor)
        {
            string callingParameters = "CreatePerlinNoiseHeightmap:" + width + ":" + height + ":" + bumpFactor;

            var obj = PerlinNoiseHeightMap.CreatePerlinNoiseHeightMap(width, height, bumpFactor);
            obj.Name = callingParameters;
            return obj.GetTriangleObject();
        }

        public static TriangleObject CreateSimpleHeightmapFromImage(string imagePath, float size, int resolution)
        {
            string callingParameters = "CreateSimpleHeightmapFromImage:" + imagePath + ":" + size + ":" + resolution;

            var obj = BitmapHeightMap.CreateSimpleHeightMapFromBitmap(new Bitmap(imagePath), size, resolution);
            obj.Name = callingParameters;
            return obj.GetTriangleObject();
        }
        
        public static TriangleObject CreateHeightmapFromImage(string imagePath, int numberOfHeightValues, int maximumNumberOfRectangles, float bumpFactor)
        {
            string callingParameters = "CreateHeightmapFromImage:" + imagePath + ":" + numberOfHeightValues + ":" + maximumNumberOfRectangles + ":" + bumpFactor;

            var obj = BitmapHeightMap.CreateHeightMapFromBitmap(new Bitmap(imagePath), numberOfHeightValues, maximumNumberOfRectangles, bumpFactor);
            obj.Name = callingParameters;
            return obj.GetTriangleObject();
        }

        public static TriangleObject Create3DBitmap(string imagePath, int depth)
        {
            string callingParameters = "Create3DBitmap:" + imagePath + ":" + depth;

            var obj = Bitmap3D.Create3DBitmap(new Bitmap(imagePath), depth);
            obj.Name = callingParameters;
            return obj.GetTriangleObject();
        }

        public static TriangleObject Create3DText(string text, float fontSize, int depth)
        {
            string callingParameters = "Create3DText:" + text + ":" + fontSize + ":" + depth;

            var obj = Bitmap3D.Create3DBitmap(BitmapHelp.GetBitmapText(text, fontSize, Color.Black, Color.White), depth);
            obj.Name = callingParameters;
            return obj.GetTriangleObject();
        }

        //Beispiel: Create3DLatice(0.1f, 1, 3, 3, 4); 
        public static TriangleObject Create3DLatice(float rodThickness, float rodLength, int countX, int countY, int countZ)
        {
            string callingParameters = "Create3DLatice:" + rodThickness + ":" + rodLength + ":" + countX + ":" + countY + ":" + countZ;

            TriangleList newObject = new TriangleList();
            for (int x = 0; x < countX; x++)
                for (int y = 0; y < countY; y++)
                    for (int z = 0; z < countZ; z++)
                    {
                        if (x < countX - 1)
                        {
                            //X-Achsen-Würfel
                            newObject.AddCube(
                                new Vertex(x - rodThickness, y + rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodLength - rodThickness, y + rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodLength - rodThickness, y + rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y + rodThickness, z - rodThickness, 0, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodLength - rodThickness, y - rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodLength - rodThickness, y - rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z - rodThickness, 0, 1));

                        }
                        else
                        {
                            //X-Achsen-Würfel
                            newObject.AddCube(
                                new Vertex(x - rodThickness, y + rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y + rodThickness, z - rodThickness, 0, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y - rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y - rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z - rodThickness, 0, 1));
                        }
                        if (y < countY - 1)
                        {
                            //Y-Achsen-Würfel
                            newObject.AddCube(
                                new Vertex(x - rodThickness, y + rodLength - rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y + rodLength - rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y + rodLength - rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y + rodLength - rodThickness, z - rodThickness, 0, 1),
                                new Vertex(x - rodThickness, y + rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y + rodThickness, z - rodThickness, 0, 1));
                        }
                        else
                        {
                            //Y-Achsen-Würfel
                            newObject.AddCube(
                                new Vertex(x - rodThickness, y + rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y + rodThickness, z - rodThickness, 0, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y - rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y - rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z - rodThickness, 0, 1));
                        }
                        if (z < countZ - 1)
                        {
                            //Z-Achsen-Würfel
                            newObject.AddCube(
                                new Vertex(x - rodThickness, y + rodThickness, z + rodLength - rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z + rodLength - rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z + rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y + rodThickness, z + rodThickness, 0, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z + rodLength - rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y - rodThickness, z + rodLength - rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y - rodThickness, z + rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z + rodThickness, 0, 1));
                        }
                        else
                        {
                            //Z-Achsen-Würfel
                            newObject.AddCube(
                                new Vertex(x - rodThickness, y + rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y + rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y + rodThickness, z - rodThickness, 0, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z + rodThickness, 0, 0),
                                new Vertex(x + rodThickness, y - rodThickness, z + rodThickness, 1, 0),
                                new Vertex(x + rodThickness, y - rodThickness, z - rodThickness, 1, 1),
                                new Vertex(x - rodThickness, y - rodThickness, z - rodThickness, 0, 1));
                        }
                    }

            newObject.TransformToCoordinateOrigin();
            newObject.SetNormals();
            newObject.Name = callingParameters;
            return newObject.GetTriangleObject();
        }

        private static TriangleList CreateTetrisstone(bool[][][] field)
        {
            StringBuilder parameters = new StringBuilder();
            for (int x = 0; x < field.Length; x++)
                for (int y = 0; y < field[x].Length; y++)
                    for (int z = 0; z < field[x][y].Length; z++)
                        parameters.Append(field[x][y][z] + ":");
            string callingParameters = "CreateTetrisstone:" + parameters.ToString();

            TriangleList stone = new TriangleList();

            for (int x = 0; x < field.Length; x++)
                for (int y = 0; y < field[x].Length; y++)
                    for (int z = 0; z < field[x][y].Length; z++)
                        if (field[x][y][z])
                        {
                            stone.AddRoundedCube(new Vector3D(x * 2, y * 2, z * 2), 1, 1.1f);
                        }

            stone.TransformToCoordinateOrigin();
            stone.SetNormals();
            stone.Name = callingParameters;
            return stone;
        }

        public static TriangleObject CreateTetrisstone1()
        {
            TriangleList newObj = CreateTetrisstone(
                new bool[][][] { new bool[][] { new bool[] { true, true, true, true } } });
            newObj.Name = "CreateTetrisstone1";
            return newObj.GetTriangleObject();
        }
        public static TriangleObject CreateTetrisstone2()
        {
            TriangleList newObj = CreateTetrisstone(
                new bool[][][] { new bool[][] {new bool[] { true, true, false, false }},
                               new bool[][] {new bool[] { false, true, true, false }}});
            newObj.Name = "CreateTetrisstone2";
            return newObj.GetTriangleObject();
        }
        public static TriangleObject CreateTetrisstone3()
        {
            TriangleList newObj = CreateTetrisstone(
                new bool[][][] { new bool[][] {new bool[] { true, true, true, true }},
                                 new bool[][] {new bool[] { false, false, false, true }}});
            newObj.Name = "CreateTetrisstone3";
            return newObj.GetTriangleObject();
        }
        public static TriangleObject CreateTetrisstone4()
        {
            TriangleList newObj = CreateTetrisstone(
                new bool[][][] { new bool[][] {new bool[] { false, true, false, false }},
                                 new bool[][] {new bool[] { true, true, true, false }}});
            newObj.Name = "CreateTetrisstone4";
            return newObj.GetTriangleObject();
        }
        public static TriangleObject CreateTetrisstone5()
        {
            TriangleList newObj = CreateTetrisstone(
                new bool[][][] { new bool[][] {new bool[] { true, true, false, false }, new bool[] { true, true, false, false }},
                                 new bool[][] {new bool[] { true, true, false, false }, new bool[] { true, true, false, false }}});
            newObj.Name = "CreateTetrisstone5";
            return newObj.GetTriangleObject();
        }

        public static TriangleObject CreateLegoObject(LegoGrid data)
        {
            return new LegoCreator().CreateLegoObject(data);
        }

        public static List<TriangleObject> LoadWaveFrontFile(string file, bool inMehrereObjekteAufteilen, bool takeNormalsFromFile)
        {
            return WaveFrontObjLoader.LoadWaveFrontFile(file, inMehrereObjekteAufteilen, takeNormalsFromFile, out string materialFile);
        }

        //wandelt jedes einzelne Dreieck in Draht um: Drahtobjekt hat 6 mal so viel Dreiecke wie original
        public static TriangleObject GetWireObject(TriangleObject obj, float wireWidth)
        {
            TriangleList newObject = new TriangleList();
            Vertex[] P = new Vertex[] { new Vertex(0, 0, 0), new Vertex(0, 0, 0), new Vertex(0, 0, 0) };

            for (int i = 0; i < obj.Triangles.Length; i++)
            {
                P[0].Position = obj.Triangles[i].V[0].Position + Vector3D.Normalize((obj.Triangles[i].V[2].Position - obj.Triangles[i].V[1].Position) / 2 + obj.Triangles[i].V[1].Position - obj.Triangles[i].V[0].Position) * wireWidth;
                P[1].Position = obj.Triangles[i].V[1].Position + Vector3D.Normalize((obj.Triangles[i].V[0].Position - obj.Triangles[i].V[2].Position) / 2 + obj.Triangles[i].V[2].Position - obj.Triangles[i].V[1].Position) * wireWidth;
                P[2].Position = obj.Triangles[i].V[2].Position + Vector3D.Normalize((obj.Triangles[i].V[1].Position - obj.Triangles[i].V[0].Position) / 2 + obj.Triangles[i].V[0].Position - obj.Triangles[i].V[2].Position) * wireWidth;

                P[0].TexcoordU = obj.Triangles[i].V[0].TexcoordU + ((obj.Triangles[i].V[2].TexcoordU - obj.Triangles[i].V[1].TexcoordU) / 2 + obj.Triangles[i].V[1].TexcoordU - obj.Triangles[i].V[0].TexcoordU) * wireWidth;
                P[1].TexcoordU = obj.Triangles[i].V[1].TexcoordU + ((obj.Triangles[i].V[0].TexcoordU - obj.Triangles[i].V[2].TexcoordU) / 2 + obj.Triangles[i].V[2].TexcoordU - obj.Triangles[i].V[1].TexcoordU) * wireWidth;
                P[2].TexcoordU = obj.Triangles[i].V[2].TexcoordU + ((obj.Triangles[i].V[1].TexcoordU - obj.Triangles[i].V[0].TexcoordU) / 2 + obj.Triangles[i].V[0].TexcoordU - obj.Triangles[i].V[2].TexcoordU) * wireWidth;

                P[0].TexcoordV = obj.Triangles[i].V[0].TexcoordV + ((obj.Triangles[i].V[2].TexcoordV - obj.Triangles[i].V[1].TexcoordV) / 2 + obj.Triangles[i].V[1].TexcoordV - obj.Triangles[i].V[0].TexcoordV) * wireWidth;
                P[1].TexcoordV = obj.Triangles[i].V[1].TexcoordV + ((obj.Triangles[i].V[0].TexcoordV - obj.Triangles[i].V[2].TexcoordV) / 2 + obj.Triangles[i].V[2].TexcoordV - obj.Triangles[i].V[1].TexcoordV) * wireWidth;
                P[2].TexcoordV = obj.Triangles[i].V[2].TexcoordV + ((obj.Triangles[i].V[1].TexcoordV - obj.Triangles[i].V[0].TexcoordV) / 2 + obj.Triangles[i].V[0].TexcoordV - obj.Triangles[i].V[2].TexcoordV) * wireWidth;

                newObject.AddQuad(new Vertex(obj.Triangles[i].V[0]), new Vertex(obj.Triangles[i].V[1]), new Vertex(P[1]), new Vertex(P[0]));
                newObject.AddQuad(new Vertex(obj.Triangles[i].V[1]), new Vertex(obj.Triangles[i].V[2]), new Vertex(P[2]), new Vertex(P[1]));
                newObject.AddQuad(new Vertex(obj.Triangles[i].V[2]), new Vertex(obj.Triangles[i].V[0]), new Vertex(P[0]), new Vertex(P[2]));
            }

            newObject.Name = "GetWireObject(" + obj.Name + "," + wireWidth + ")";
            newObject.SetNormals();

            return newObject.GetTriangleObject();
        }

        public static TriangleObject GetFlippedNormalsObjectFromOtherObject(TriangleObject obj)
        {
            TriangleList newObj = new TriangleList();

            foreach (var oldTriangle in obj.Triangles)
            {
                newObj.AddTriangle(new Vertex(oldTriangle.V[2]), new Vertex(oldTriangle.V[1]), new Vertex(oldTriangle.V[0]));
            }

            newObj.Name = "GetFlippedNormalsObjectFromOtherObject(" + obj.Name + ")";
            newObj.SetNormals();

            return newObj.GetTriangleObject();
        }

        public static TriangleObject MergeTwoObjects(List<Triangle> triangles1, List<Triangle> triangles2, out Vector3D positionFromMergedObject, string nameFromMergedObject)
        {
            TriangleList newObject = new TriangleList();

            foreach (var T in triangles1)
            {
                newObject.AddTriangle(T.V[0], T.V[1], T.V[2]);

            }

            foreach (var T in triangles2)
            {
                newObject.AddTriangle(T.V[0], T.V[1], T.V[2]);

            }

            var box = newObject.Triangles.GetBoundingBox();
            positionFromMergedObject = box.Center;

            newObject.TransformToCoordinateOrigin();
            newObject.SetNormals();
            newObject.Name = nameFromMergedObject;
            return newObject.GetTriangleObject();
        }        
    }
}
