using System.Collections.Generic;
using GraphicMinimal;

namespace GraphicPanels
{
    //All die Befehle, welche Objekte hinzufügen, entfernen oder modizfizieren (Dreiecksdaten; Keine Materialeigenschaften)
    internal interface IGeometryCommand
    {
        void Execute(GraphicPanel3D graphic); //führt die Änderung an graphic durch 
    }

    class GeometryCommandContainer
    {
        private List<IGeometryCommand> commands = new List<IGeometryCommand>();

        public void AddCommand(IGeometryCommand command)
        {
            this.commands.Add(command);
        }

        public void RemoveAllCommands()
        {
            this.commands.Clear();
        }

        public IGeometryCommand[] GetAllCommands()
        {
            return this.commands.ToArray();
        }
    }

    internal class AddHelixCommand : IGeometryCommand
    {
        public float SmallRadius { get; set; }
        public float BigRadius { get; set; }
        public float Height { get; set; }
        public float NumberOfTurns { get; set; }
        public bool Cap { get; set; }
        public int Resolution1 { get; set; }
        public int Resolution2 { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddHelixCommand(float smallRadius, float bigRadius, float height, float numberOfTurns, bool cap, int resolution1, int resolution2,
            ObjectPropertys objectPropertys)
        {
            this.SmallRadius = smallRadius;
            this.BigRadius = bigRadius;
            this.Height = height;
            this.NumberOfTurns = numberOfTurns;
            this.Cap = cap;
            this.Resolution1 = resolution1;
            this.Resolution2 = resolution2;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddHelix(this.SmallRadius, this.BigRadius, this.Height, this.NumberOfTurns, this.Cap, this.Resolution1, this.Resolution2, this.ObjectPropertys);
        }
    }

    internal class AddSphereCommand : IGeometryCommand
    {
        public float Radius { get; set; }
        public int Resolution1 { get; set; }
        public int Resolution2 { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddSphereCommand(float radius, int resolution1, int resolution2,
            ObjectPropertys objectPropertys)
        {
            this.Radius = radius;            
            this.Resolution1 = resolution1;
            this.Resolution2 = resolution2;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddSphere(this.Radius, this.Resolution1, this.Resolution2, this.ObjectPropertys);
        }
    }

    internal class AddRingCommand : IGeometryCommand
    {
        public float SmallRadius { get; set; }
        public float BigRadius { get; set; }
        public int Resolution1 { get; set; }
        public int Resolution2 { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddRingCommand(float smallRadius, float bigRadius, int resolution1, int resolution2,
            ObjectPropertys objectPropertys)
        {
            this.SmallRadius = smallRadius;
            this.BigRadius = bigRadius;
            this.Resolution1 = resolution1;
            this.Resolution2 = resolution2;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddRing(this.SmallRadius, this.BigRadius, this.Resolution1, this.Resolution2, this.ObjectPropertys);
        }
    }

    internal class AddCornellBoxCommand : IGeometryCommand
    {
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddCornellBoxCommand(ObjectPropertys objectPropertys)
        {
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddCornellBox(this.ObjectPropertys);
        }
    }

    internal class AddCubeCommand : IGeometryCommand
    {
        public float XSize { get; set; }
        public float YSize { get; set; }
        public float ZSize { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddCubeCommand(float xSize, float ySize, float zSize,
            ObjectPropertys objectPropertys)
        {
            this.XSize = xSize;
            this.YSize = ySize;
            this.ZSize = zSize;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddCube(this.XSize, this.YSize, this.ZSize, this.ObjectPropertys);
        }
    }

    internal class AddSquareXYCommand : IGeometryCommand
    {
        public float XSize { get; set; }
        public float YSize { get; set; }
        public int Separations { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddSquareXYCommand(float xSize, float ySize, int separations,
            ObjectPropertys objectPropertys)
        {
            this.XSize = xSize;
            this.YSize = ySize;
            this.Separations = separations;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddSquareXY(this.XSize, this.YSize, this.Separations, this.ObjectPropertys);
        }
    }

    internal class AddSquareXZCommand : IGeometryCommand
    {
        public float XSize { get; set; }
        public float ZSize { get; set; }
        public int Separations { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddSquareXZCommand(float xSize, float zSize, int separations,
            ObjectPropertys objectPropertys)
        {
            this.XSize = xSize;
            this.ZSize = zSize;
            this.Separations = separations;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddSquareXZ(this.XSize, this.ZSize, this.Separations, this.ObjectPropertys);
        }
    }

    internal class AddBlobCommand : IGeometryCommand
    {
        public Vector3D[] CenterList { get; set; }
        public float SphereRadius { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddBlobCommand(Vector3D[] centerList, float sphereRadius,
            ObjectPropertys objectPropertys)
        {
            this.CenterList = centerList;
            this.SphereRadius = sphereRadius;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddBlob(this.CenterList, this.SphereRadius, this.ObjectPropertys);
        }
    }

    internal class AddTorchCommand : IGeometryCommand
    {
        public float Size { get; set; }
        public int Resolution { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddTorchCommand(float size, int resolution,
            ObjectPropertys objectPropertys)
        {
            this.Size = size;
            this.Resolution = resolution;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddTorch(this.Size, this.Resolution, this.ObjectPropertys);
        }
    }

    internal class AddPillarCommand : IGeometryCommand
    {
        public float Height { get; set; }
        public float BigRadius { get; set; }
        public float SmallRadius { get; set; }
        public bool CapBottom { get; set; }
        public bool CapTop { get; set; }
        public int Resolution { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddPillarCommand(float height, float bigRadius, float smallRadius, bool capBottom, bool capTop, int resolution,
            ObjectPropertys objectPropertys)
        {
            this.Height = height;
            this.BigRadius = bigRadius;
            this.SmallRadius = smallRadius;
            this.CapBottom = capBottom;
            this.CapTop = capTop;
            this.Resolution = resolution;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddPillar(this.Height, this.BigRadius, this.SmallRadius, this.CapBottom, this.CapTop, this.Resolution, this.ObjectPropertys);
        }
    }

    internal class AddLaticeCommand : IGeometryCommand
    {
        public float Height { get; set; }
        public float Width { get; set; }
        public float Radius { get; set; }
        public int NumberOfBarsX { get; set; }
        public int NumberOfBarsY { get; set; }
        public int Resolution { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddLaticeCommand(float height, float width, float radius, int numberOfBarsX, int numberOfBarsY, int resolution,
            ObjectPropertys objectPropertys)
        {
            this.Height = height;
            this.Width = width;
            this.Radius = radius;
            this.NumberOfBarsX = numberOfBarsX;
            this.NumberOfBarsY = numberOfBarsY;
            this.Resolution = resolution;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddLatice(this.Height, this.Width, this.Radius, this.NumberOfBarsX, this.NumberOfBarsY, this.Resolution, this.ObjectPropertys);
        }
    }

    internal class AddCylinderCommand : IGeometryCommand
    {
        public float Height { get; set; }
        public float RadiusBottom { get; set; }
        public float RadiusTop { get; set; }
        public bool Cap { get; set; }
        public int Resolution { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddCylinderCommand(float height, float radiusBottom, float radiusTop, bool cap, int resolution,
            ObjectPropertys objectPropertys)
        {
            this.Height = height;
            this.RadiusBottom = radiusBottom;
            this.RadiusTop = radiusTop;
            this.Cap = cap;
            this.Resolution = resolution;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddCylinder(this.Height, this.RadiusBottom, this.RadiusTop, this.Cap, this.Resolution, this.ObjectPropertys);
        }
    }

    internal class AddBottleCommand : IGeometryCommand
    {
        public float Radius { get; set; }
        public float Length { get; set; }
        public int Resolution { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddBottleCommand(float radius, float length, int resolution,
            ObjectPropertys objectPropertys)
        {
            this.Radius = radius;
            this.Length = length;
            this.Resolution = resolution;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddBottle(this.Radius, this.Length, this.Resolution, this.ObjectPropertys);
        }
    }

    internal class AddSwordCommand : IGeometryCommand
    {
        public float Length { get; set; }
        public int Resolution { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddSwordCommand(float length, int resolution,
            ObjectPropertys objectPropertys)
        {
            this.Length = length;
            this.Resolution = resolution;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddSword(this.Length, this.Resolution, this.ObjectPropertys);
        }
    }

    internal class AddSkewerCommand : IGeometryCommand
    {
        public float Length { get; set; }
        public int Resolution { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddSkewerCommand(float length, int resolution,
            ObjectPropertys objectPropertys)
        {
            this.Length = length;
            this.Resolution = resolution;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddSkewer(this.Length, this.Resolution, this.ObjectPropertys);
        }
    }

    internal class AddSawCommand : IGeometryCommand
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }
        public int NumberOfSpikes { get; set; }
        public float DisplacementFactor { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddSawCommand(float width, float height, float depth, int numberOfSpikes, float displacementFactor,
            ObjectPropertys objectPropertys)
        {
            this.Width = width;
            this.Height = height;
            this.Depth = depth;
            this.NumberOfSpikes = numberOfSpikes;
            this.DisplacementFactor = displacementFactor;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddSaw(this.Width, this.Height, this.Depth, this.NumberOfSpikes, this.DisplacementFactor, this.ObjectPropertys);
        }
    }

    internal class AddMirrorFrameCommand : IGeometryCommand
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Radius { get; set; }
        public int Resolution { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddMirrorFrameCommand(float width, float height, float radius, int resolution,
            ObjectPropertys objectPropertys)
        {
            this.Width = width;
            this.Height = height;
            this.Radius = radius;
            this.Resolution = resolution;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddMirrorFrame(this.Width, this.Height, this.Radius, this.Resolution, this.ObjectPropertys);
        }
    }

    internal class AddPerlinNoiseHeightmapCommand : IGeometryCommand
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public float BumpFactor { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddPerlinNoiseHeightmapCommand(int width, int height, float bumpFactor,
            ObjectPropertys objectPropertys)
        {
            this.Width = width;
            this.Height = height;
            this.BumpFactor = bumpFactor;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddPerlinNoiseHeightmap(this.Width, this.Height, this.BumpFactor, this.ObjectPropertys);
        }
    }

    internal class AddSimpleHeightmapFromImageCommand : IGeometryCommand
    {
        public string ImagePath { get; set; }
        public float Size { get; set; }
        public int Resolution { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddSimpleHeightmapFromImageCommand(string imagePath, float size, int resolution,
            ObjectPropertys objectPropertys)
        {
            this.ImagePath = imagePath;
            this.Size = size;
            this.Resolution = resolution;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddSimpleHeightmapFromImage(this.ImagePath, this.Size, this.Resolution, this.ObjectPropertys);
        }
    }

    internal class AddHeightmapFromImageCommand : IGeometryCommand
    {
        public string ImagePath { get; set; }
        public int NumberOfHeightValues { get; set; }
        public int MaximumNumberOfRectangles { get; set; }
        public float BumpFactor { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddHeightmapFromImageCommand(string imagePath, int numberOfHeightValues, int maximumNumberOfRectangles, float bumpFactor,
            ObjectPropertys objectPropertys)
        {
            this.ImagePath = imagePath;
            this.NumberOfHeightValues = numberOfHeightValues;
            this.MaximumNumberOfRectangles = maximumNumberOfRectangles;
            this.BumpFactor = bumpFactor;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddHeightmapFromImage(this.ImagePath, this.NumberOfHeightValues, this.MaximumNumberOfRectangles, this.BumpFactor, this.ObjectPropertys);
        }
    }

    internal class Add3DBitmapCommand : IGeometryCommand
    {
        public string ImagePath { get; set; }
        public int Depth { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public Add3DBitmapCommand(string imagePath, int depth,
            ObjectPropertys objectPropertys)
        {
            this.ImagePath = imagePath;
            this.Depth = depth;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.Add3DBitmap(this.ImagePath, this.Depth, this.ObjectPropertys);
        }
    }

    internal class Add3DTextCommand : IGeometryCommand
    {
        public string Text { get; set; }
        public float FontSize { get; set; }
        public int Depth { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public Add3DTextCommand(string text, float fontSize, int depth,
            ObjectPropertys objectPropertys)
        {
            this.Text = text;
            this.FontSize = fontSize;
            this.Depth = depth;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.Add3DText(this.Text, this.FontSize , this.Depth, this.ObjectPropertys);
        }
    }

    internal class Add3DLaticeCommand : IGeometryCommand
    {
        public float RodThickness { get; set; }
        public float RodLength { get; set; }
        public int CountX { get; set; }
        public int CountY { get; set; }
        public int CountZ { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public Add3DLaticeCommand(float rodThickness, float rodLength, int countX, int countY, int countZ,
            ObjectPropertys objectPropertys)
        {
            this.RodThickness = rodThickness;
            this.RodLength = rodLength;
            this.CountX = countX;
            this.CountY = countY;
            this.CountZ = countZ;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.Add3DLatice(this.RodThickness, this.RodLength, this.CountX, this.CountY, this.CountZ, this.ObjectPropertys);
        }
    }

    internal class AddTetrisstone1Command : IGeometryCommand
    {
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddTetrisstone1Command(ObjectPropertys objectPropertys)
        {
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddTetrisstone1(this.ObjectPropertys);
        }
    }

    internal class AddTetrisstone2Command : IGeometryCommand
    {
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddTetrisstone2Command(ObjectPropertys objectPropertys)
        {
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddTetrisstone2(this.ObjectPropertys);
        }
    }

    internal class AddTetrisstone3Command : IGeometryCommand
    {
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddTetrisstone3Command(ObjectPropertys objectPropertys)
        {
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddTetrisstone3(this.ObjectPropertys);
        }
    }

    internal class AddTetrisstone4Command : IGeometryCommand
    {
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddTetrisstone4Command(ObjectPropertys objectPropertys)
        {
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddTetrisstone4(this.ObjectPropertys);
        }
    }

    internal class AddTetrisstone5Command : IGeometryCommand
    {
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddTetrisstone5Command(ObjectPropertys objectPropertys)
        {
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddTetrisstone5(this.ObjectPropertys);
        }
    }

    internal class CreateLegoObjectCommand : IGeometryCommand
    {
        public IEnumerable<int> ObjectIds { get; set; }
        public int Separations { get; set; }

        public CreateLegoObjectCommand(IEnumerable<int> objectIds, int separations)
        {
            this.ObjectIds = objectIds;
            this.Separations = separations;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.CreateLegoObject(this.ObjectIds, this.Separations);
        }
    }

    internal class AddWaveFrontFileCommand : IGeometryCommand
    {
        public string File { get; set; }
        public bool TakeNormalsFromFile { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddWaveFrontFileCommand(string file, bool takeNormalsFromFile, ObjectPropertys objectPropertys)
        {
            this.File = file;
            this.TakeNormalsFromFile = takeNormalsFromFile;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddWaveFrontFile(this.File, this.TakeNormalsFromFile, this.ObjectPropertys);
        }
    }

    internal class AddWaveFrontFileAndSplitCommand : IGeometryCommand
    {
        public string File { get; set; }
        public bool TakeNormalsFromFile { get; set; }
        public ObjectPropertys ObjectPropertys { get; set; }

        public AddWaveFrontFileAndSplitCommand(string file, bool takeNormalsFromFile, ObjectPropertys objectPropertys)
        {
            this.File = file;
            this.TakeNormalsFromFile = takeNormalsFromFile;
            this.ObjectPropertys = objectPropertys;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.AddWaveFrontFileAndSplit(this.File, this.TakeNormalsFromFile, this.ObjectPropertys);
        }
    }

    internal class TransformToWireObjectCommand : IGeometryCommand
    {
        public int SourceObjId { get; set; }
        public float WireWidth { get; set; }

        public TransformToWireObjectCommand(int sourceObjId, float wireWidth)
        {
            this.SourceObjId = sourceObjId;
            this.WireWidth = wireWidth;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.TransformToWireObject(this.SourceObjId, this.WireWidth);
        }
    }

    internal class FlipNormalsCommand : IGeometryCommand
    {
        public int ObjId { get; set; }

        public FlipNormalsCommand(int objId)
        {
            this.ObjId = objId;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.FlipNormals(this.ObjId);
        }
    }

    internal class MergeTwoObjectsCommand : IGeometryCommand
    {
        public int ObjId1 { get; set; }
        public int ObjId2 { get; set; }

        public MergeTwoObjectsCommand(int objId1, int objId2)
        {
            this.ObjId1 = objId1;
            this.ObjId2 = objId2;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.MergeTwoObjects(this.ObjId1, this.ObjId2);
        }
    }

    internal class CreateCopyCommand : IGeometryCommand
    {
        public int ObjId { get; set; }

        public CreateCopyCommand(int objId)
        {
            this.ObjId = objId;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.CreateCopy(this.ObjId);
        }
    }

    internal class SetCenterOfObjectOriginCommand : IGeometryCommand
    {
        public int ObjID { get; set; }
        public Vector3D WorldPosition { get; set; }

        public SetCenterOfObjectOriginCommand(int objID, Vector3D worldPosition)
        {
            this.ObjID = objID;
            this.WorldPosition = worldPosition;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.SetCenterOfObjectOrigin(this.ObjID, this.WorldPosition);
        }
    }

    internal class TransformToCubemappedObjectCommand : IGeometryCommand
    {
        public int ObjId { get; set; }

        public TransformToCubemappedObjectCommand(int objId)
        {
            this.ObjId = objId;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.TransformToCubemappedObject(this.ObjId);
        }
    }

    internal class CreateCubemappedObjectCommand : IGeometryCommand
    {
        public IEnumerable<int> ObjectIds { get; set; }

        public CreateCubemappedObjectCommand(IEnumerable<int> objectIds)
        {
            this.ObjectIds = objectIds;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.CreateCubemappedObject(this.ObjectIds);
        }
    }

    internal class RemoveObjektCommand : IGeometryCommand
    {
        public int ObjId { get; set; }

        public RemoveObjektCommand(int objId)
        {
            this.ObjId = objId;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.RemoveObjekt(this.ObjId);
        }
    }

    internal class RemoveObjectByNameCommand : IGeometryCommand
    {
        public string StartsWithText { get; set; }

        public RemoveObjectByNameCommand(string startsWithText)
        {
            this.StartsWithText = startsWithText;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.RemoveObjectByName(this.StartsWithText);
        }
    }

    internal class RemoveObjectStartsWithCommand : IGeometryCommand
    {
        public string StartsWithText { get; set; }

        public RemoveObjectStartsWithCommand(string startsWithText)
        {
            this.StartsWithText = startsWithText;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.RemoveObjectStartsWith(this.StartsWithText);
        }
    }

    internal class RemoveObjectContainsCommand : IGeometryCommand
    {
        public string ContainsSearch { get; set; }

        public RemoveObjectContainsCommand(string containsSearch)
        {
            this.ContainsSearch = containsSearch;
        }

        public void Execute(GraphicPanel3D graphic)
        {
            graphic.RemoveObjectContains(this.ContainsSearch);
        }
    }
}
