using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using TriangleObjectGeneration;
using System.Drawing;
using GraphicGlobal;
using BitmapHelper;
using FullPathGenerator.AnalyseHelper;
using GraphicPanels.Helper;

namespace GraphicPanels
{
    //Bietet Zeichenfunktionen für den Rasterizer als auch Raytracer an.
    //Je nach ausgewählten Ausgabemodus bekomme ich aber eine Exception, wenn ich eine Funktion nutzen will, die der jeweilige Modus 
    //nicht unterstützt.
    public class GraphicPanel3D : GraphicPanel
    {
        public GlobalObjectPropertys GlobalSettings {get; private set;}

        public GraphicPanel3D()
        {
            this.GlobalSettings = new GlobalObjectPropertys();
        }

        private readonly DrawingObjectContainer drawingObjectContainer = new DrawingObjectContainer();
        private readonly GeometryCommandContainer commandContainer = new GeometryCommandContainer();

        private Mode3D mode = Mode3D.CPU;
        public Mode3D Mode
        {
            get
            {
                return this.mode;
            }
            set
            {
                this.mode = value;
                SwitchMode(value);
            }
        }

        public static bool IsRasterizerMode(Mode3D mode)
        {
            return 
                mode == Mode3D.Direct3D_11 || 
                mode == Mode3D.OpenGL_Version_1_0 || 
                mode == Mode3D.OpenGL_Version_1_0_OldShaders ||
                mode == Mode3D.OpenGL_Version_3_0 ||
                mode == Mode3D.CPU ||
                mode == Mode3D.DepthOfField;
        }

        private int AddObjektAndMoveToCenter(DrawingObject obj)
        {
            obj.MoveTriangleDataToCenterPoint();
            return this.drawingObjectContainer.AddObject(obj);
        }

        public int AddHelix(float smallRadius = 0.05f, float bigRadius = 0.5f, float height = 3, float numberOfTurns = 6, bool cap = false, int resolution1 = 4, int resolution2 = 20,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddHelixCommand(smallRadius, bigRadius, height, numberOfTurns, cap, resolution1, resolution2, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateHelix(smallRadius, bigRadius, height, numberOfTurns, cap, resolution1, resolution2), objectPropertys));
        }

        public int AddSphere(float radius = 1, int resolution1 = 10, int resolution2 = 10,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddSphereCommand(radius, resolution1, resolution2, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateSphere(radius, resolution1, resolution2), objectPropertys));
        }

        public int AddRing(float smallRadius = 0.3f, float bigRadius = 2, int resolution1 = 5, int resolution2 = 20,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddRingCommand(smallRadius, bigRadius, resolution1, resolution2, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateRing(smallRadius, bigRadius, resolution1, resolution2), objectPropertys));
        }

        public int[] AddCornellBox(ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddCornellBoxCommand(objectPropertys));

            var subObjects = TriangleObjectGenerator.CreateCornellBox(); //grayWalls, ground, leftWall, rightWall, light, rightCube, leftCube

            if (objectPropertys == null) objectPropertys = new ObjectPropertys();

            objectPropertys.BrdfModel = BrdfModel.Diffus;
            objectPropertys.NormalInterpolation = InterpolationMode.Flat;
            objectPropertys.ShowFromTwoSides = true;

            string white = "#B2B2B2";
            string green = "#33B233"; 
            string red = "#B23333"; 
            string lightColor = "#FFFFFF"; 
            

            //http://news.povray.org/povray.unofficial.patches/message/%3C39380E82.5F30%40wanadoo.fr%3E/#%3C39380E82.5F30%40wanadoo.fr%3E
            //string white = "#BDBDBB"; 
            //string green = "#296815"; 
            //string red = "#5D090B"; 
            //string lightColor = "#C7C7C6"; 

            int grayWalls = AddObjektAndMoveToCenter(new DrawingObject(subObjects[0], new ObjectPropertys(objectPropertys) { TextureFile = white }));
            int ground = AddObjektAndMoveToCenter(new DrawingObject(subObjects[1], new ObjectPropertys(objectPropertys) { TextureFile = white }));
            int leftWall = AddObjektAndMoveToCenter(new DrawingObject(subObjects[2], new ObjectPropertys(objectPropertys) { TextureFile = red }));
            int rightWall = AddObjektAndMoveToCenter(new DrawingObject(subObjects[3], new ObjectPropertys(objectPropertys) { TextureFile = green }));
            int rightCube = AddObjektAndMoveToCenter(new DrawingObject(subObjects[5], new ObjectPropertys(objectPropertys) { TextureFile = white }));
            int leftCube = AddObjektAndMoveToCenter(new DrawingObject(subObjects[6], new ObjectPropertys(objectPropertys) { TextureFile = white }));

            int light = AddObjektAndMoveToCenter(new DrawingObject(subObjects[4], new ObjectPropertys(objectPropertys)
            {
                TextureFile = lightColor,
                RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 1 },
                RasterizerLightSource = new RasterizerLightSourceDescription()
                {
                    ConstantAttenuation = 2
                }
            }));


            int[] ids = new int[] { grayWalls, ground, leftWall, rightWall, light, rightCube, leftCube };

            return ids;
        }

        public int AddCube(float xSize = 1, float ySize = 1, float zSize = 1,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddCubeCommand(xSize, ySize, zSize, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateCube(xSize, ySize, zSize), objectPropertys));
        }

        public int AddCube(BoundingBox box, ObjectPropertys objectPropertys)
        {
            objectPropertys.Position = box.Center;
            objectPropertys.Orientation = new Vector3D(0, 0, 0);
            return AddCube(box.XSize / 2, box.YSize / 2, box.ZSize / 2, objectPropertys);
        }

        public int AddSquareXY(float xSize = 1, float ySize = 1, int separations = 1,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddSquareXYCommand(xSize, ySize, separations, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateSquareXY(xSize, ySize, separations), objectPropertys));
        }

        public int AddSquareXZ(float xSize = 1, float zSize = 1, int separations = 1,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddSquareXZCommand(xSize, zSize, separations, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateSquareXZ(xSize, zSize, separations), objectPropertys));
        }

        public int AddBlob(Vector3D[] centerList, float sphereRadius,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddBlobCommand(centerList, sphereRadius, objectPropertys));

            if (objectPropertys == null) objectPropertys = new ObjectPropertys();
            var blobPropertys = new BlobPropertys(){ CenterList = centerList, SphereRadius = sphereRadius };
            return this.drawingObjectContainer.AddObject(new DrawingObject(new TriangleObject(TriangleObjectGenerator.CreateSphere(sphereRadius, 5,5).Triangles, blobPropertys.ToString()) , new ObjectPropertys(objectPropertys)
            {
                BlobPropertys = blobPropertys
            }));
        }

        public int AddTorch(float size = 0.2f, int resolution = 5,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddTorchCommand(size, resolution, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateTorch(size, resolution), objectPropertys));
        }

        public int AddPillar(float height = 5, float bigRadius = 1, float smallRadius = 0.08f, bool capBottom = false, bool capTop = true, int resolution = 50,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddPillarCommand(height, bigRadius, smallRadius, capBottom, capTop, resolution, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreatePillar(height, bigRadius, smallRadius, capBottom, capTop, resolution), objectPropertys));
        }

        public int AddLatice(float height = 5, float width = 3, float radius = 0.1f, int numberOfBarsX = 4, int numberOfBarsY = 5, int resolution = 5,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddLaticeCommand(height, width, radius, numberOfBarsX, numberOfBarsY, resolution, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateLatice(height, width, radius, numberOfBarsX, numberOfBarsY, resolution), objectPropertys));
        }

        public int AddCylinder(float height = 5, float radiusBottom = 1, float radiusTop = 1, bool cap = false, int resolution = 4,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddCylinderCommand(height, radiusBottom, radiusTop, cap, resolution, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateCylinder(height, radiusBottom, radiusTop, cap, resolution), objectPropertys));
        }

        public int AddBottle(float radius = 1, float length = 2, int resolution = 6,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddBottleCommand(radius, length, resolution, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateBottle(radius, length, resolution), objectPropertys));
        }

        public int AddSword(float length = 4, int resolution = 5,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddSwordCommand(length, resolution, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateSword(length, resolution), objectPropertys));
        }

        public int AddSkewer(float length = 4, int resolution = 3,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddSkewerCommand(length, resolution, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateSkewer(length, resolution), objectPropertys));
        }

        public int AddSaw(float width = 3, float height = 3, float depth = 1, int numberOfSpikes = 5, float displacementFactor = 1,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddSawCommand(width, height, depth, numberOfSpikes, displacementFactor, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateSaw(width, height, depth, numberOfSpikes, displacementFactor), objectPropertys));
        }

        public int AddMirrorFrame(float width = 5, float height = 6, float radius = 0.5f, int resolution = 5,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddMirrorFrameCommand(width, height, radius, resolution, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateMirrorFrame(width, height, radius, resolution), objectPropertys));
        }

        public int AddPerlinNoiseHeightmap(int width = 20, int height = 20, float bumpFactor = 2,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddPerlinNoiseHeightmapCommand(width, height, bumpFactor, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreatePerlinNoiseHeightmap(width, height, bumpFactor), objectPropertys));
        }

        public int AddSimpleHeightmapFromImage(string imagePath, float size = 1, int resolution = 1,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddSimpleHeightmapFromImageCommand(imagePath, size, resolution, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateSimpleHeightmapFromImage(imagePath, size, resolution), objectPropertys));
        }

        public int AddHeightmapFromImage(string imagePath, int numberOfHeightValues, int maximumNumberOfRectangles, float bumpFactor,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddHeightmapFromImageCommand(imagePath, numberOfHeightValues, maximumNumberOfRectangles, bumpFactor, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateHeightmapFromImage(imagePath, numberOfHeightValues, maximumNumberOfRectangles, bumpFactor), objectPropertys));
        }

        public int Add3DBitmap(string imagePath, int depth = 1,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new Add3DBitmapCommand(imagePath, depth, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.Create3DBitmap(imagePath, depth), objectPropertys));
        }

        public int Add3DText(string text, float fontSize = 10, int depth = 2,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new Add3DTextCommand(text, fontSize, depth, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.Create3DText(text, fontSize, depth), objectPropertys));
        }

        public int Add3DLatice(float rodThickness = 0.1f, float rodLength = 1, int countX = 3, int countY = 3, int countZ = 4,
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new Add3DLaticeCommand(rodThickness, rodLength, countX, countY, countZ, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.Create3DLatice(rodThickness, rodLength, countX, countY, countZ), objectPropertys));
        }

        public int AddTetrisstone1(
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddTetrisstone1Command(objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateTetrisstone1(), objectPropertys));
        }

        public int AddTetrisstone2(
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddTetrisstone2Command(objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateTetrisstone2(), objectPropertys));
        }

        public int AddTetrisstone3(
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddTetrisstone3Command(objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateTetrisstone3(), objectPropertys));
        }

        public int AddTetrisstone4(
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddTetrisstone4Command(objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateTetrisstone4(), objectPropertys));
        }

        public int AddTetrisstone5(
            ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddTetrisstone5Command(objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.CreateTetrisstone5(), objectPropertys));
        }

        public int CreateLegoObject(IEnumerable<int> objectIds, int separations = 60)
        {
            this.commandContainer.AddCommand(new CreateLegoObjectCommand(objectIds, separations));

            var triangleData = TriangleObjectGenerator.CreateLegoObject(GetLegoGrid(objectIds, separations));
            return this.drawingObjectContainer.AddObject(new DrawingObject(triangleData, new ObjectPropertys()));
        }

        //separations = So oft wird die längste BoundingBox-Kante unterteilt
        public LegoGrid GetLegoGrid(IEnumerable<int> objectIds, int separations)
        {
            return LegoGridCreator.Create(objectIds.SelectMany(x => this.drawingObjectContainer.GetObjectById(x).GetTrianglesInWorldSpace()).ToList(), separations);
        }

        public int AddWaveFrontFile(string file, bool takeNormalsFromFile, ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddWaveFrontFileCommand(file, takeNormalsFromFile, objectPropertys));

            return this.drawingObjectContainer.AddObject(new DrawingObject(TriangleObjectGenerator.LoadWaveFrontFile(file, false, takeNormalsFromFile)[0], objectPropertys));
        }

        public int[] AddWaveFrontFileAndSplit(string file, bool takeNormalsFromFile, ObjectPropertys objectPropertys = null)
        {
            this.commandContainer.AddCommand(new AddWaveFrontFileAndSplitCommand(file, takeNormalsFromFile, objectPropertys));

            var subObjects = WaveFrontLoader.LoadObjectsFromFile(file, takeNormalsFromFile, objectPropertys);

            int[] ids = new int[subObjects.Count];
            for (int i = 0; i < subObjects.Count; i++)
            {
                ids[i] = AddObjektAndMoveToCenter(subObjects[i]);

                if (subObjects[i].DrawingProps.Name.Contains("True:"))
                {
                    string name = subObjects[i].DrawingProps.Name.Split(new string[] { "True:" }, StringSplitOptions.None).Last();
                    if (name.Contains(":")) name = name.Split(':')[0];
                    if (name.Contains("_")) name = name.Substring(0, name.LastIndexOf('_'));
                    subObjects[i].DrawingProps.Name = name;
                }                
            }

            return ids;
        }
        
        public static Camera ReadCameraFromObjAuxFile(string auxiliaryFileName)
        {
            return WaveFrontAuxiliaryLoader.ReadFile(auxiliaryFileName).Camera;
        }

        public static string GetMaterialOverviewFromObjFile(string objFile)
        {
            return WaveFrontLoader.GetMaterialOverview(objFile);
        }

        public int TransformToWireObject(int sourceObjId, float wireWidth)
        {
            this.commandContainer.AddCommand(new TransformToWireObjectCommand(sourceObjId, wireWidth));

            return this.drawingObjectContainer.TransformToWireObject(sourceObjId, wireWidth);
        }        

        public int TransformToSphere(int objId)
        {
            //Wenn innerhalb von ein Command weitere Commands aufgerufen werden, dann sorgt das beim Abspielen
            //der Commands nach dem Json-Laden, dass es viel zu viele Commands gibt, die dann erzeugt werden
            //this.commandContainer.AddCommand(new TransformToSphereCommand(objId)); -> Darf ich so nicht machen!

            Vector3D spherePosition = GetObjectById(objId).Position;
            float radius = GetBoundingBoxFromObject(objId).RadiusInTheBox;
            int sphereId = AddSphere(radius, 10, 10, GetObjectById(objId));
            GetObjectById(sphereId).Position = spherePosition;
            GetObjectById(sphereId).Size = 1;
            RemoveObjekt(objId);

            return sphereId;
        }

        public int FlipNormals(int objId)
        {
            this.commandContainer.AddCommand(new FlipNormalsCommand(objId));

            return this.drawingObjectContainer.FlipNormals(objId);
        }

        public int MergeTwoObjects(int objId1, int objId2) //Gibt die Id von den neu erstellen Objekt zurück
        {
            this.commandContainer.AddCommand(new MergeTwoObjectsCommand(objId1, objId2));

            return this.drawingObjectContainer.MergeTwoObjects(objId1, objId2);
        }

        public int CreateCopy(int objId)
        {
            this.commandContainer.AddCommand(new CreateCopyCommand(objId));

            return this.drawingObjectContainer.AddObject(new DrawingObject(this.drawingObjectContainer.GetObjectById(objId)));
        }

        //Setzt den Koordinatenursprung von ein Objekt auf worldPosition
        public void SetCenterOfObjectOrigin(int objID, Vector3D worldPosition)
        {
            this.commandContainer.AddCommand(new SetCenterOfObjectOriginCommand(objID, worldPosition));

            this.drawingObjectContainer.SetCenterOfObjectOrigin(objID, worldPosition);
        }

        //Setzt den Koordinatenursprung von eine Menge von Objekten auf dessen gemeinsame Boundingbox-Center
        public void SetCenterOfObjectOrigin(IEnumerable<int> objectIds)
        {
            var box = GetBoundingBoxFromObjects(objectIds);
            objectIds.ToList().ForEach(x => SetCenterOfObjectOrigin(x, box.Center));
        }

        //Setzt den Koordinatenursprung von eine Menge von Objekten auf dessen gemeinsame Boundingbox-Center
        public void SetCenterOfObjectOrigin(IEnumerable<int> objectIds, Vector3D center)
        {
            objectIds.ToList().ForEach(x => SetCenterOfObjectOrigin(x, center));
        }

        public BoundingBox GetBoundingBoxFromObject(int objID)
        {
            return this.drawingObjectContainer.GetBoundingBoxFromObject(objID);
        }

        public BoundingBox GetBoundingBoxFromObjects(IEnumerable<int> objectIds)
        {
            return new BoundingBox(objectIds.Select(x => this.drawingObjectContainer.GetBoundingBoxFromObject(x)));
        }

        public BoundingBox GetBoundingBoxFromAllObjects()
        {
            var objectIds = this.drawingObjectContainer.GetAllObjectIds();
            return new BoundingBox(objectIds.Select(x => this.drawingObjectContainer.GetBoundingBoxFromObject(x)));
        }

        public void TransformToCubemappedObject(int objId)
        {
            this.commandContainer.AddCommand(new TransformToCubemappedObjectCommand(objId));

            var box = GetBoundingBoxFromObject(objId);
            this.drawingObjectContainer.TransformToCubemappedObject(objId, box.Center);
        }

        public void CreateCubemappedObject(IEnumerable<int> objectIds)
        {
            this.commandContainer.AddCommand(new CreateCubemappedObjectCommand(objectIds));

            var box = GetBoundingBoxFromObjects(objectIds);
            objectIds.ToList().ForEach(x => this.drawingObjectContainer.TransformToCubemappedObject(x, box.Center));
        }

        public void RemoveObjekt(int objId)
        {
            this.commandContainer.AddCommand(new RemoveObjektCommand(objId));

            this.drawingObjectContainer.RemoveObject(objId);
        }

        public void RemoveAllObjekts()
        {
            this.commandContainer.RemoveAllCommands();
            this.drawingObjectContainer.RemoveAllObjects();
            this.GlobalSettings = new GlobalObjectPropertys();
        }

        public void RemoveObjectByName(string startsWithText)
        {
            this.commandContainer.AddCommand(new RemoveObjectByNameCommand(startsWithText));

            this.drawingObjectContainer.RemoveObject(GetObjectByName(startsWithText).Id);
        }

        public void RemoveObjectStartsWith(string startsWithText)
        {
            this.commandContainer.AddCommand(new RemoveObjectStartsWithCommand(startsWithText));

            this.drawingObjectContainer.RemoveObject(ResolveObjectIdByNameStartsWith(startsWithText));
        }

        public void RemoveObjectContains(string containsSearch)
        {
            this.commandContainer.AddCommand(new RemoveObjectContainsCommand(containsSearch));

            this.drawingObjectContainer.RemoveObject(GetObjectsByNameContainsSearch(containsSearch).First().Id);
        }

        public ObjectPropertys GetObjectById(int id)
        {
            return this.drawingObjectContainer.GetObjectById(id).DrawingProps;
        }

        public List<ObjectPropertys> GetAllObjects()
        {
            return this.drawingObjectContainer.GetAllObjects().Select(x => x.DrawingProps).ToList();
        }

        public ObjectPropertys GetObjectByName(string name)
        {
            return this.drawingObjectContainer.GetAllObjects().Select(x => x.DrawingProps).FirstOrDefault(x => x.Name == name);
        }

        public ObjectPropertys GetObjectByNameStartsWith(string startsWithText)
        {
            return this.drawingObjectContainer.GetAllObjects().Select(x => x.DrawingProps).FirstOrDefault(x => x.Name.StartsWith(startsWithText));
        }

        public IEnumerable<ObjectPropertys> GetObjectsByNameContainsSearch(string searchString)
        {
            return this.drawingObjectContainer.GetAllObjects().Select(x => x.DrawingProps).Where(x => x.Name.Contains(searchString));
        }

        private int ResolveObjectIdByNameStartsWith(string startsWithText)
        {
            var obj = this.drawingObjectContainer.GetAllObjects().FirstOrDefault(x => x.DrawingProps.Name.StartsWith(startsWithText));
            return this.drawingObjectContainer.GetObjectId(obj);
        }

        //Zu Kontrollzwecken um die GetExportDataAsJson- und LoadExportDataFromJson zu kontrollieren
        public string GetBigExportData()
        {
            var data = new BigExportData()
            {
                Commands = this.commandContainer.GetAllCommands(),
                //AllObjects = this.drawingObjectContainer.GetAllObjekts().ToArray(),
                AllObjects = GetAllObjects().ToArray(),
                GlobalSettings = this.GlobalSettings,
                Modus = this.Mode
            };

            return JsonHelper.ToJson(data);
        }

        public string GetExportDataAsJson()
        {
            var data = new SmallExportData() 
            { 
                Commands = this.commandContainer.GetAllCommands(),
                AllObjectPropertys = GetAllObjects().ToArray(), 
                GlobalSettings = this.GlobalSettings,
                Modus = this.Mode
            };

            return ExportDataJsonConverter.ToJson(data);            
        }

        public void LoadExportDataFromJson(string json)
        {
            var data = ExportDataJsonConverter.CreateFromJson(json);

            //1. Alle Objekte aus json-Export einfügen
            this.RemoveAllObjekts();
            foreach (var command in data.Commands)
            {
                command.Execute(this);
            }

            //2. All die Propertys aktualisieren, die vom Defaultwert abweichen
            foreach (var obj in data.AllObjectPropertys)
            {
                this.drawingObjectContainer.GetObjectById(obj.Id).DrawingProps = obj;
            }
            this.GlobalSettings = data.GlobalSettings;
            this.GlobalSettings.ThreadCount = new GlobalObjectPropertys().ThreadCount;
            this.Mode = data.Modus;
        }

        protected override T GetPanel<T>()
        {
            var panel = this.controls.GetPanel(this.mode);
            if ((panel is T) == false) throw new InterfaceFromDrawingPanelNotSupportedException(typeof(T), this.mode.ToString(), "The mode " + this.mode + " does not support the Interface " + typeof(T).Name);
            return (T)this.controls.GetPanel(this.mode);
        }

        private Frame3DData GetCurrentFrame()
        {
            return new Frame3DData() { GlobalObjektPropertys = this.GlobalSettings, DrawingObjects = this.drawingObjectContainer.GetAllObjects() };
        }

        public void DrawAndFlip()
        {
            var panel = GetPanel<IDrawing3D>();
            //panel.ClearScreen(this.GlobalSettings.BackgroundImage); Geht nicht da ich explizit die TextureId oder Color angeben muss
            this.ClearScreen(this.GlobalSettings.BackgroundImage); //So wird der String automatisch als Bild oder Farbe interpretiert
            panel.Draw3DObjects(GetCurrentFrame());
            panel.FlipBuffer();
        }

        public void DrawWithoutFlip()
        {
            var panel = GetPanel<IDrawing3D>();
            //panel.ClearScreen(this.GlobalSettings.BackgroundImage);
            this.ClearScreen(this.GlobalSettings.BackgroundImage);
            panel.Draw3DObjects(GetCurrentFrame());
            panel.Enable2DModus();
        }

        public int MouseHitTest(Point mousePosition)
        {
            var panel = GetPanel<IDrawing3D>();
            var obj = panel.MouseHitTest(GetCurrentFrame(), mousePosition);
            if (obj == null) return -1;
            return this.drawingObjectContainer.GetObjectId(obj);
        }

        public Bitmap GetScreenShoot()
        {
            if (this.controls.GetPanel(this.mode) is IDrawingAsynchron)
                return this.GetPanel<IDrawingPanel>().DrawingControl.BackgroundImage as Bitmap;
            else
                return this.GetPanel<IDrawingSynchron>().GetDataFromFrontbuffer();

        }

        public string ExportToWavefront()
        {
            return this.drawingObjectContainer.ExportToWavefront();
        }

        public void StartImageAnalyser(string outputFolder, int imageWidth, int imageHeight, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            var panel = GetPanel<IDrawingAsynchron>();
            panel.StartImageAnalyser(GetCurrentFrame(), imageWidth, imageHeight, new ImagePixelRange(0, 0, imageWidth, imageHeight), outputFolder, renderingFinish, exceptionOccured);
        }

        public void StartImageAnalyser(string outputFolder, int imageWidth, int imageHeight, ImagePixelRange pixelRange, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            var panel = GetPanel<IDrawingAsynchron>();
            panel.StartImageAnalyser(GetCurrentFrame(), imageWidth, imageHeight, pixelRange, outputFolder, renderingFinish, exceptionOccured);
        }

        public void StartRaytracingFromSubImage(Bitmap imageWithRectangle, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            var panel = GetPanel<IDrawingAsynchron>();
            panel.StartRaytracing(GetCurrentFrame(), imageWithRectangle.Width, imageWithRectangle.Height, new ImagePixelRange(imageWithRectangle), renderingFinish, exceptionOccured);
        }

        public void StartRaytracing()
        {
            StartRaytracing(this.Width, this.Height, (result) => { }, (error) => { System.Windows.Forms.MessageBox.Show(error.ToString()); });
        }

        public void StartRaytracing(int imageWidth, int imageHeight, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            var panel = GetPanel<IDrawingAsynchron>();
            panel.StartRaytracing(GetCurrentFrame(), imageWidth, imageHeight, new ImagePixelRange(0, 0, imageWidth, imageHeight), renderingFinish, exceptionOccured);
        }

        public void StartRaytracing(int imageWidth, int imageHeight, ImagePixelRange pixelRange, Action<RaytracerResultImage> renderingFinish, Action<Exception> exceptionOccured)
        {
            var panel = GetPanel<IDrawingAsynchron>();
            panel.StartRaytracing(GetCurrentFrame(), imageWidth, imageHeight, pixelRange, renderingFinish, exceptionOccured);
        }

        public RaytracerResultImage GetRaytracingImageSynchron(int imageWidth, int imageHeight, ImagePixelRange pixelRange = null)
        {
            var panel = GetPanel<IDrawingAsynchron>();
            if (pixelRange == null) pixelRange = new ImagePixelRange(0, 0, imageWidth, imageHeight);
            return panel.GetRaytracingImageSynchron(GetCurrentFrame(), imageWidth, imageHeight, pixelRange);
        }
        public Vector3D GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData debuggingData, ImagePixelRange pixelRange = null)
        {
            var panel = GetPanel<IRaytracingHelper>();
            if (pixelRange == null) pixelRange = new ImagePixelRange(0, 0, debuggingData.ScreenSize.Width, debuggingData.ScreenSize.Height);
            
            var globalMedia = this.GlobalSettings.GlobalParticipatingMedia;
            this.GlobalSettings = debuggingData.GlobalSettings;
            this.GlobalSettings.GlobalParticipatingMedia = globalMedia; //Achtung: GlobalParticipatingMedia kann nicht XML-Serialisiert werden!

            return panel.GetColorFromSinglePixelForDebuggingPurpose(GetCurrentFrame(), debuggingData.ScreenSize.Width, debuggingData.ScreenSize.Height, pixelRange, debuggingData);
        }

        //Synchrone Ausgabe von ein Bild
        public Bitmap GetSingleImage(int imageWidth, int imageHeight, ImagePixelRange pixelRange = null)
        {
            if (this.controls.GetPanel(this.mode) is IDrawingAsynchron)
            {
                return GetRaytracingImageSynchron(imageWidth, imageHeight, pixelRange).Bitmap;
            }else
            {
                this.Width = imageWidth;
                this.Height = imageHeight;

                DrawAndFlip();
                Bitmap image = BitmapHelp.SetAlpha(this.GetPanel<IDrawingSynchron>().GetDataFromFrontbuffer(),255);
                if (pixelRange != null) image = BitmapHelp.GetImageFromPixelRange(image, pixelRange);
                return image;
            }
        }

        public static string CompareTwoPathSpaceFiles(string fileName1, string fileName2)
        {
            PathContributionForEachPathSpace space1 = new PathContributionForEachPathSpace(fileName1);
            PathContributionForEachPathSpace space2 = new PathContributionForEachPathSpace(fileName2);

            return space1.CompareWithOther(space2);
        }

        public static string CompareManyPathSpaceFiles(string[] files, bool withFactor)
        {
            if (withFactor)
                return PathContributionForEachPathSpace.CompareAllWithFactor(files.Select(x => new PathContributionForEachPathSpace(x)).ToArray());
            else
                return PathContributionForEachPathSpace.CompareAll(files.Select(x => new PathContributionForEachPathSpace(x)).ToArray());
        }

        public static string GetSumOverallPathSpaces(string pathspaceFile)
        {
            PathContributionForEachPathSpace space = new PathContributionForEachPathSpace(pathspaceFile);
            Vector3D sum = space.SumOverAllPathSpaces();
            Vector3D radianceWithGammaAndClamping = sum.Pow(1 / 2.2).Clamp(0, 1);
            return "Sum=" + sum.ToShortString() + Environment.NewLine + 
                   "PixelColor with Gamma and Clampling=" + radianceWithGammaAndClamping.ToShortString() + "\t" + "RGB=" + (radianceWithGammaAndClamping * 255).ToInt().ToShortString() + System.Environment.NewLine;
        }

        public Vector3D GetColorFromSinglePixel(int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            var panel = GetPanel<IRaytracingHelper>();
            if (pixelRange == null) pixelRange = new ImagePixelRange(0, 0, imageWidth, imageHeight);
            Vector3D color = panel.GetColorFromSinglePixel(GetCurrentFrame(), imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount);
            if (this.GlobalSettings.Tonemapping == TonemappingMethod.GammaOnly) color = color.Pow(1 / 2.2).Clamp(0,1);
            return (color * 255).ToInt();
        }

        public List<Vector3D> GetNPixelSamples(int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            var panel = GetPanel<IRaytracingHelper>();
            if (pixelRange == null) pixelRange = new ImagePixelRange(0, 0, imageWidth, imageHeight);
            return panel.GetNPixelSamples(GetCurrentFrame(), imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount);
        }

        public string GetFullPathsFromSinglePixel(int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            var panel = GetPanel<IRaytracingHelper>();
            if (pixelRange == null) pixelRange = new ImagePixelRange(0, 0, imageWidth, imageHeight);
            return panel.GetFullPathsFromSinglePixel(GetCurrentFrame(), imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount);
        }

        public string GetPathContributionsForSinglePixel(int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            var panel = GetPanel<IRaytracingHelper>();
            if (pixelRange == null) pixelRange = new ImagePixelRange(0, 0, imageWidth, imageHeight);
            return panel.GetPathContributionsForSinglePixel(GetCurrentFrame(), imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount);
        }

        public float GetBrightnessFactor(int imageWidth, int imageHeight)
        {
            var panel = GetPanel<IRaytracingHelper>();
            return panel.GetBrightnessFactor(GetCurrentFrame(), imageWidth, imageHeight);
        }

        public void StopRaytracing()
        {
            try
            {
                var panel = GetPanel<IDrawingAsynchron>();
                panel.StopRaytracing();
            }
            catch (InterfaceFromDrawingPanelNotSupportedException) { } //Damit ich beim Fenster-Schließen kein Fehler bekomme, wenn ich im Rasterizermode bin
        }

        public void SaveCurrentRaytracingDataToFolder()  //Damit man wärend des Raytracens zusätzlich zum AutoSave eine Sicherung der Daten anlegen kann
        {
            var panel = GetPanel<IDrawingAsynchron>();
            panel.SaveCurrentRaytracingDataToFolder();
        }

        //Hinweis: Ich kann als Input leider keine WaveFront-Datei direkt nehmen, da ja KameraStart+KameraRichtung-Objekte den Flipalgorithmus stören und 
        //da man bei WaveFronts keine Kamera mit angeben kann. Außerdem nutze ich für Lichtquellen nicht die Emission-Property, da ich mehr als Diffuse Lichtquellen habe.
        //Somit muss man also so vorgehen, als ob man eine Scene rendern will
        public string GetFlippedWavefrontFileFromCurrentSceneData(int imageWidth, int imageHeight)
        {
            var panel = GetPanel<IRaytracingHelper>();
            return panel.GetFlippedWavefrontFile(GetCurrentFrame(), imageWidth, imageHeight);
        }

        public string ProgressText 
        {
            get
            {
                if (this.controls.GetPanel(this.mode) is IDrawingAsynchron)
                    return GetPanel<IDrawingAsynchron>().ProgressText;
                else
                    return "";
            }
        }

        // Hier kann abgefragt werden, wie lange das rendern noch dauert
        public float ProgressPercent
        {
            get
            {
                if (this.controls.GetPanel(this.mode) is IDrawingAsynchron)
                    return GetPanel<IDrawingAsynchron>().ProgressPercent;
                else
                    return 0;
            }
        }

        public void UpdateProgressImage()
        {
            var panel = GetPanel<IDrawingAsynchron>();
            panel.UpdateProgressImage(this.GlobalSettings.BrightnessFactor, this.GlobalSettings.Tonemapping);
        }

        public bool IsRaytracingNow
        {
            get
            {
                if (this.controls.GetPanel(this.mode) is IDrawingAsynchron)
                    return GetPanel<IDrawingAsynchron>().IsRaytracingNow;
                else
                    return false;
            }
        }

        public static Bitmap CreateBumpmapFromObjFile(string objFile, Size size, float border)
        {
            return new ObjToHeightMapConverter().CreateBumpmap(objFile, size, border);
        }
    }
}
