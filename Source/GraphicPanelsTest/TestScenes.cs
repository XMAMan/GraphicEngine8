using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using GraphicPanels;
using System.Drawing;
using TextureHelper.TexturMapping;
using System.IO;

namespace GraphicPanelsTest
{
    internal class TestScenes
    {
        public static string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;

        public static void LoadScene(GraphicPanel3D graphic, string sceneFile)
        {
            graphic.LoadExportDataFromJson(File.ReadAllText(DataDirectory + sceneFile).Replace("<DataFolder>", DataDirectory.Replace("\\", "\\\\")));
        }

        public static void AddTestscene1_RingSphere(GraphicPanel3D graphic) => LoadScene(graphic, "01_RingSphere_json.txt");
        public static void AddTestscene2_NoWindowRoom(GraphicPanel3D graphic) => LoadScene(graphic, "02_NoWindowRoom_json.txt");
        public static void AddTestscene3_SpheresWithTexture(GraphicPanel3D graphic) => LoadScene(graphic, "03_SpheresWithTexture_json.txt");
        public static void AddTestscene4_ProceduralTextures(GraphicPanel3D graphic) => LoadScene(graphic, "04_ProceduralTextures_json.txt");
        public static void AddTestscene5_Cornellbox(GraphicPanel3D graphic) => LoadScene(graphic, "05_Cornellbox_json.txt");
        public static void AddTestscene5_BlenderCornellbox(GraphicPanel3D graphic) => LoadScene(graphic, "05_BlenderCornellbox_json.txt");
        public static void AddTestscene5_WaterNoMediaCornellbox(GraphicPanel3D graphic) => LoadScene(graphic, "05_WaterNoMediaCornellbox_json.txt");
        public static void AddTestscene5_WaterCornellbox(GraphicPanel3D graphic) => LoadScene(graphic, "05_WaterCornellbox_json.txt");
        public static void AddTestscene5_MirrorCornellbox(GraphicPanel3D graphic) => LoadScene(graphic, "05_MirrorCornellbox_json.txt");
        public static void AddTestscene5_RisingSmokeCornellbox(GraphicPanel3D graphic) => LoadScene(graphic, "05_RisingSmokeCornellbox_json.txt");
        public static void AddTestscene6_ChinaRoom(GraphicPanel3D graphic) => LoadScene(graphic, "06_ChinaRoom_json.txt");
        public static void AddTestscene7_Chessboard(GraphicPanel3D graphic) => LoadScene(graphic, "07_Chessboard_json.txt");
        public static void AddTestscene8_WindowRoom(GraphicPanel3D graphic) => LoadScene(graphic, "08_WindowRoom_json.txt");
        public static void AddTestscene9_MultipleImportanceSampling(GraphicPanel3D graphic) => LoadScene(graphic, "09_MultipleImportanceSampling_json.txt"); 
        public static void AddTestscene10_MirrorGlassCaustic(GraphicPanel3D graphic) => LoadScene(graphic, "10_MirrorGlassCaustic_json.txt");
        public static void AddTestscene11_PillarsOfficeGodRay(GraphicPanel3D graphic) => LoadScene(graphic, "11_PillarsOfficeGodRay_json.txt");
        public static void AddTestscene11_PillarsOfficeMedia(GraphicPanel3D graphic) => LoadScene(graphic, "11_PillarsOfficeMedia_json.txt");
        public static void AddTestscene11_PillarsOfficeNoMedia(GraphicPanel3D graphic) => LoadScene(graphic, "11_PillarsOfficeNoMedia_json.txt");
        public static void AddTestscene11_PillarsOfficeNight(GraphicPanel3D graphic) => LoadScene(graphic, "11_PillarsOfficeNight_json.txt");
        public static void AddTestscene12_Snowman(GraphicPanel3D graphic) => LoadScene(graphic, "12_Snowman_json.txt");
        public static void AddTestscene13_MicrofacetGlas(GraphicPanel3D graphic) => LoadScene(graphic, "13_MicrofacetGlas_json.txt");
        public static void AddTestscene14_MicrofacetSundial(GraphicPanel3D graphic) => LoadScene(graphic, "14_MicrofacetSundial_json.txt");
        public static void AddTestscene15_MicrofacetSphereBox(GraphicPanel3D graphic) => LoadScene(graphic, "15_MicrofacetSphereBox_json.txt");
        public static void AddTestscene16_Graphic6Memories(GraphicPanel3D graphic) => LoadScene(graphic, "16_Graphic6Memories_json.txt");
        public static void AddTestscene17_TheFifthElement(GraphicPanel3D graphic) => LoadScene(graphic, "17_TheFifthElement_json.txt");
        public static void AddTestScene18_CloudsForTestImage(GraphicPanel3D graphic) => LoadScene(graphic, "18_CloudsForTestImage_json.txt");
        public static void AddTestscene19_StillLife(GraphicPanel3D graphic) => LoadScene(graphic, "19_Stilllife_json.txt");
        public static void AddTestscene20_Mirrorballs(GraphicPanel3D graphic) => LoadScene(graphic, "20_Mirrorballs_json.txt");
        public static void AddTestscene21_Candle(GraphicPanel3D graphic) => LoadScene(graphic, "21_Candle_json.txt");
        public static void AddTestscene22_ToyBox(GraphicPanel3D graphic) => LoadScene(graphic, "22_ToyBox_json.txt");
        public static void AddTestscene23_MirrorShadowWithSphere(GraphicPanel3D graphic) => LoadScene(graphic, "23_MirrorShadowWithSphere_json.txt");
        public static void AddTestscene23_MirrorShadowNoSphere(GraphicPanel3D graphic) => LoadScene(graphic, "23_MirrorShadowNoSphere_json.txt");
        public static void AddTestscene24_EnvironmentMaterialTest(GraphicPanel3D graphic) => LoadScene(graphic, "24_EnvironmentMaterialTest_json.txt");
        public static void AddTestscene25_SingleSphereForRapso(GraphicPanel3D graphic) => LoadScene(graphic, "25_SingleSphereForRapso_json.txt");
        public static void AddTestscene26_SkyEnvironmapCreator(GraphicPanel3D graphic) => LoadScene(graphic, "26_SkyEnvironmapCreator_json.txt");
        public static void AddTestscene27_MirrorsEdge(GraphicPanel3D graphic) => LoadScene(graphic, "27_MirrorsEdge_json.txt");

        public static void AddTestscene1_RingSphereWithParallaxGround(GraphicPanel3D graphic)
        {
            AddTestscene1_RingSphere(graphic);

            //Fußboden
            graphic.GetObjectById(1).NormalSource = new NormalFromParallax() { ParallaxMap = DataDirectory + "Decal.bmp", TextureMatrix = Matrix3x3.Scale(1, 3), ConvertNormalMapFromColor = true, IsParallaxEdgeCutoffEnabled = true, TexturHeightFactor = 0.14f };
            graphic.GetObjectById(1).BrdfModel = BrdfModel.Diffus;

            //Durch angabe von unterschiedlichen ScaleX/Y will ich testen, ob es zu einer Verzehrung kommt
            //Wenn man die StepDirection nicht mit ScaleXY multipliziert, erhält man diese Verzehrung
            graphic.GetObjectById(1).Color.As<ColorFromTexture>().TextureMatrix = Matrix3x3.Scale(1, 3);

            //Damit ich die eventuelle fehlerhafte Verzehrung besser sehe, drehe ich den Boden
            graphic.GetObjectById(1).Orientation.Z = 75;


            graphic.GetObjectById(4).Albedo = 1.0f; //Kugel

            graphic.GlobalSettings.ShadowsForRasterizer = RasterizerShadowMode.Shadowmap;
        }


        //Erde + Atmospähre + Richtungslicht für die Sonne
        public static void AddTestscene_SkyMedia(GraphicPanel3D graphic, float sunDegree)
        {
            graphic.RemoveAllObjekts();

            DescriptionForSkyMedia media = new DescriptionForSkyMedia();

            graphic.AddSphere(media.EarthRadius, 10, 10, new ObjectPropertys() { TextureFile = "#007700" }); //Erde
            graphic.AddSphere(media.AtmosphereRadius, 10, 10, new ObjectPropertys() { TextureFile = "#FFFFFF", RefractionIndex = 1, MediaDescription = media, ShowFromTwoSides = true });
            graphic.AddSquareXY(1, 1, 1, new ObjectPropertys() { Orientation = new Vector3D(90, -sunDegree, 0), RaytracingLightSource = new FarAwayDirectionLightDescription() { Emission = 20 } });

            graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, media.EarthRadius + 100, 0), Vector3D.Normalize(new Vector3D(1, 1.1f, 0)), 100.0f);
        }

        //Eine Szene, die aus lauter Vierecken besteht, welche in der XY-Ebene betrachet so aussehen: < _ - _ -
        public static void AddTestscene_SubpathSamplerTestScene(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();

            int count = 2;
            List<int> plates = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int id = graphic.AddSquareXY(0.5f, 0.5f, 1, new ObjectPropertys() { Position = new Vector3D(i * 2, i % 2, 0), Orientation = new Vector3D(90, 0, 0), TextureFile = "#FFFFFF", BrdfModel = BrdfModel.Diffus, NormalInterpolation = InterpolationMode.Flat });
                if (i % 2 == 0) graphic.FlipNormals(id);
                plates.Add(id);
            }

            graphic.GetObjectById(plates.Last()).RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 100 };
            graphic.GetObjectById(plates.Last()).Size = 0.01f;

            graphic.GlobalSettings.BackgroundImage = "#0000AA";

            graphic.GlobalSettings.Camera = new Camera(new Vector3D(-1, 0.5f, 0), new Vector3D(1, 0, 0), 45.0f);
        }

        //Wie die Cornellbox nur ohne Würfel und mit nur 5 Vierecken
        public static void AddTestscene_FullPathSamplerTestScene(GraphicPanel3D graphic, bool withMediaBox)
        {
            graphic.RemoveAllObjekts();

            float sizeFactor = 10;

            float groundSize = 0.9f;
            graphic.AddSquareXY(groundSize, groundSize, 1, new ObjectPropertys() { Position = new Vector3D(0, 0, -1) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(0, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = 0.8f }); //Fußboden
            graphic.AddSquareXY(groundSize, groundSize, 1, new ObjectPropertys() { Position = new Vector3D(0, 1, 0) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(90, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = 0.8f }); //Rückwand
            graphic.AddSquareXY(groundSize, groundSize, 1, new ObjectPropertys() { Position = new Vector3D(-1, 0, 0) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(0, 90, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = 0.8f }); //Linke Wand
            graphic.AddSquareXY(groundSize, groundSize, 1, new ObjectPropertys() { Position = new Vector3D(+1, 0, 0) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(0, -90, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, Albedo = 0.8f }); //Rechte Wand
            graphic.AddSquareXY(groundSize, groundSize, 1, new ObjectPropertys() { Position = new Vector3D(0, 0, +1) * sizeFactor, Size = sizeFactor, Orientation = new Vector3D(180, 0, 0), TextureFile = "#FFFFFF", NormalInterpolation = InterpolationMode.Flat, RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 10000 * sizeFactor * sizeFactor } }); //Decke

            if (withMediaBox)
            {
                float scatteringFromMedia = 0.1f * 2 / sizeFactor;
                float absorbationFromMedia = 0.025f * 2 / sizeFactor;

                graphic.AddCube(groundSize * 2, groundSize * 2, groundSize * 2, new ObjectPropertys()
                {
                    Position = new Vector3D(0, 0, 0),
                    TextureFile = "#FF0000",
                    Size = 0.5f * sizeFactor,
                    RefractionIndex = 1,
                    MediaDescription = new DescriptionForHomogeneousMedia()
                    {
                        ScatteringCoeffizent = new Vector3D(1, 1, 1) * scatteringFromMedia,
                        AbsorbationCoeffizent = new Vector3D(1, 1, 1) * absorbationFromMedia
                    }
                });
            }

            float imagePlaneSize = groundSize * 2;
            float imagePlaneDistance = 1.0f;
            float foV = (float)(Math.Atan(imagePlaneSize / 2 / imagePlaneDistance) / (2 * Math.PI) * 360) * 2;
            graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, -(groundSize + imagePlaneDistance) * sizeFactor, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), foV);

            //Hinweis: Die Photonmap darf nur einmal am Anfang erzeugt werden damit man das im Normalmode nachstellen kann
            //Außerdem muss die Erstellung der Beam2Beam-Map unterbunden werden, da das sonst zu lange dauert
            //graphic.GlobalSettings.RecursionDepth = 7;
            //graphic.GlobalSettings.PhotonCount = 10000;
            //graphic.GlobalSettings.SamplingCount = 20000;
            //graphic.GlobalSettings.CameraSamplingMode = PixelSamplingMode.Equal;
            //3, 3, new ImagePixelRange(1,1,1,1), (result) =>
        }

        //LegoMan
        //Hinweis: Ich habe die Parallax-Kugel mit mein AIS-Laptop (i7) mit 100 Samples mal gerendert und habe dafür 53 Sekunden gebraucht und mein i3-Laptop hat 249 Sekunden benötigt
        //D.h. i7 ist um Faktor 4,7 Schneller als i3
        public enum LegoMaterial
        {
            Diffuse, Plastic1, Plastic2, RoughMetal, SmoothMetal1, SmoothMetal2, Glas, ProceduralMirror, Copper, BlockMode, Parallax, Rust, MirrorRust, Spider, BumpGlas,
            MotionBlure, MicofacetGlas, RoughnessmapGlas, Anisotroph, Coffee, WaterBoy, Wax, WaterIce, Cloud
        }
        public static void AddTestscene_LegoMan(GraphicPanel3D graphic, LegoMaterial material)
        {
            //Wird von außen vorgegeben
            bool useEnvironmentLight = true;
            bool useSphere = false;

            //Wenn der Size-Faktor 1 ist, habe ich keine Shadowmap-Agne aber dafür ist das Umgebungslicht beim Raytracer zu dunkel. Deswegen nehme ich hier jetzt 0.01f
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + ("30_LegoMan.obj"), true, new ObjectPropertys() { Size = 0.01f, SpecularHighlightPowExponent = 20, NormalInterpolation = InterpolationMode.Flat, Albedo = 0.1f });

            var objList = graphic.GetAllObjects();
            foreach (var obj in objList)
            {
                obj.Name = obj.Name.Split(new string[] { "True:" }, StringSplitOptions.None).Last();
                obj.BrdfModel = BrdfModel.Diffus;
                if (obj.Color.Type == ColorSource.Texture) obj.Color.As<ColorFromTexture>().TextureMode = TextureMode.Clamp;
            }

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith(useEnvironmentLight ? "KameraStart2" : "KameraStart1").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd").Position - cameraStart);
            Vector3D spotDirection = Vector3D.Normalize(graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("KameraEnd").Id).Center - graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("LichtSpot").Id).Center);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart1").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart2").Id);

            //Wird intern entsprechend dem vorgegebenen material gesetzt
            bool useSpotLight = false;
            bool useWater = false;
            bool useFilledLegs = false;
            bool useGrid = false;
            bool useCloud = false;

            foreach (var id in graphic.GetObjectsByNameContainsSearch("Lego_"))
            {
                id.NormalInterpolation = InterpolationMode.Smooth;
            }

            switch (material)
            {
                case LegoMaterial.Diffuse://Diffus Smooth
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetDiffuseSmooth();
                    break;

                case LegoMaterial.Plastic1://Plastik1 (Diffuse + Glanzpunkt)
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetPlastic1();
                    break;

                case LegoMaterial.Plastic2://Plastik2 (Diffuse + Glanzpunkt + Mirror)
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetPlastic2();
                    break;

                case LegoMaterial.RoughMetal://Raues Metall
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetRoughMetall();
                    break;

                case LegoMaterial.SmoothMetal1://Spiegel glatt (Reflektion ist farblich verändert)
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetCleanMirror();
                    break;

                case LegoMaterial.SmoothMetal2://Fliese (Reflektion ist weiß)
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetTile();
                    break;

                case LegoMaterial.Glas://Glas ohne Media
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetNoMediaGlas();
                    foreach (var id in graphic.GetObjectsByNameContainsSearch("Kopf")) id.TextureFile = DataDirectory + "ScaledImage.bmp";

                    //Glas mit Media
                    //graphic.GetObjectsByNameContainsSearch("Lego_").SetGlasWithMedia();
                    break;

                case LegoMaterial.ProceduralMirror://Procedural Mirror
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetProceduralMirror();
                    foreach (var id in graphic.GetObjectsByNameContainsSearch("Kopf"))
                    {
                        id.NormalSource.As<NormalFromProcedure>().TextureMatrix = Matrix3x3.Scale(0.2f, 0.6f);
                        id.TextureCoordSource = new ProceduralTextureCoordSource() { TextureCoordsProceduralFunction = new CubeMapping(id.Position) };
                    }
                    break;

                case LegoMaterial.Copper://Kupfer über Normalmap
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetCopper(DataDirectory + "AvocadoSkinNormalMap.jpg");
                    break;

                case LegoMaterial.BlockMode://Block-Mode
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetDiffuseFlat();
                    useGrid = true;
                    break;

                case LegoMaterial.Parallax://Parallax-Mapping
                    GraphicPanel3D.CreateBumpmapFromObjFile(DataDirectory + "Huckel.obj", new Size(256, 256), 0).Save(DataDirectory + "Huckel.bmp");
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetParallaxMapping(DataDirectory + "Huckel.bmp");
                    foreach (var id in graphic.GetObjectsByNameContainsSearch("Kopf"))
                    {
                        id.NormalSource.As<NormalMapFromFile>().TextureMatrix = Matrix3x3.Scale(5, 5);
                        id.TextureCoordSource = new ProceduralTextureCoordSource() { TextureCoordsProceduralFunction = new CubeMapping(id.Position) };
                    }
                    break;

                case LegoMaterial.Rust://Rost über Textur
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetRepeatTexture(DataDirectory + "DSCF1036.jpg");
                    break;

                case LegoMaterial.MirrorRust://Spiegel mit Rost
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetRustMirror(DataDirectory + "istockphoto-827289002-1024x1024_.jpg");
                    break;

                case LegoMaterial.Spider://BlackIsTransparent
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetBlackIsTransparent(DataDirectory + "Image1.bmp");
                    foreach (var id in graphic.GetObjectsByNameContainsSearch("Kopf")) graphic.TransformToCubemappedObject(id.Id);
                    break;

                case LegoMaterial.BumpGlas://Glas ohne Media mit Normalmmap
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetNormalmappedGlas(DataDirectory + "AvocadoSkinNormalMap.jpg");
                    foreach (var id in graphic.GetObjectsByNameContainsSearch("Kopf")) id.TextureFile = DataDirectory + "ScaledImage.bmp";
                    break;

                case LegoMaterial.MotionBlure://Motion-Effekt wo sich der Arm mit Hand bewegt
                    float yWin = -Vector3D.AngleDegree(new Vector3D(0, 0, -1), new Vector3D(cameraDirection.X, 0, cameraDirection.Z));
                    var motionObjects = graphic.GetAllObjects().Where(x => x.Name.Contains("HandLinks") || x.Name.Contains("ArmLinks")).ToList(); //Diese Objekte sollen gedreht werden
                    var boxMotion = graphic.GetBoundingBoxFromObjects(motionObjects.Select(x => x.Id));  //Lege den Drehpunkt in der Schulter fest
                    Vector3D center = new Vector3D(boxMotion.Min.X + boxMotion.XSize * 0.5f, boxMotion.Min.Y + boxMotion.YSize * 0.88f, boxMotion.Min.Z + boxMotion.ZSize * 0.5f);
                    graphic.SetCenterOfObjectOrigin(motionObjects.Select(x => x.Id), center);
                    float size = motionObjects[0].Size;
                    var motion = new FuncMotionBlueMovementDescription()
                    {
                        GetTimeMatrizes = (time) =>
                        {
                            float xWinStart = -20;
                            float xWinEnd = -90;
                            float f = 1.0f / (float)Math.Exp(time * 1.1);
                            float xWin = (1 - f) * xWinStart + f * xWinEnd;

                            Matrix4x4 objToWorld = Matrix4x4.Ident();
                            objToWorld = Matrix4x4.Translate(center.X, center.Y, center.Z) * objToWorld;
                            objToWorld = Matrix4x4.Rotate(yWin, 0.0f, 1.0f, 0.0f) * objToWorld; //Drehe den Arm erst so, dass die X-Achse genau nach (1,0,0) zeigt
                            objToWorld = Matrix4x4.Rotate(xWin * time, 1.0f, 0.0f, 0.0f) * objToWorld; //Drehe nun laut Time-Wert den Arm an der X-Achse
                            objToWorld = Matrix4x4.Scale(size, size, size) * objToWorld;

                            Matrix4x4 worldToObj = Matrix4x4.Ident();
                            worldToObj = Matrix4x4.Scale(1 / size, 1 / size, 1 / size) * worldToObj;
                            worldToObj = Matrix4x4.Rotate(-xWin * time, 1.0f, 0.0f, 0.0f) * worldToObj;
                            worldToObj = Matrix4x4.Rotate(-yWin, 0.0f, 1.0f, 0.0f) * worldToObj;
                            worldToObj = Matrix4x4.Translate(-center.X, -center.Y, -center.Z) * worldToObj;

                            return new AffineMatrizes()
                            {
                                ObjectToWorldMatrix = objToWorld,
                                NormalObjectToWorldMatrix = objToWorld,
                                WorldToObjectMatrix = worldToObj
                            };
                        }
                    };
                    foreach (var id in motionObjects)
                    {
                        id.MotionBlurMovment = motion;
                    }
                    break;

                case LegoMaterial.MicofacetGlas://Microfacet Glas
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetMicrofacetGlas();
                    foreach (var id in graphic.GetObjectsByNameContainsSearch("Kopf")) id.TextureFile = DataDirectory + "ScaledImage.bmp";
                    break;

                case LegoMaterial.RoughnessmapGlas://Microfacet Glas mit Roughnesmap
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetMicrofacetGlas(DataDirectory + "CheckBoard.png");
                    foreach (var id in graphic.GetObjectsByNameContainsSearch("Kopf"))
                    {
                        id.TextureCoordSource = new ProceduralTextureCoordSource() { TextureCoordsProceduralFunction = new CubeMapping(id.Position) };
                    }
                    break;

                case LegoMaterial.Anisotroph://Anisotrophes Metal
                    graphic.CreateRoughnessMap(DataDirectory + "RoughnessMap.bmp");
                    graphic.SetAnisotrophicMetall(graphic.GetObjectsByNameContainsSearch("Lego_"), DataDirectory + "RoughnessMap.bmp");
                    foreach (var id in graphic.GetObjectsByNameContainsSearch("Kopf")) id.TextureFile = DataDirectory + "ScaledImage.bmp";
                    break;

                case LegoMaterial.Coffee://Farb-Procedural (Kaffee)
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetCoffeeMode();
                    break;

                case LegoMaterial.WaterBoy://Wasser + Moos
                    graphic.GetObjectsByNameContainsSearch("Lego_").SetRepeatTexture(DataDirectory + "stone-moss-3d-model-sbsar.jpg");
                    useWater = true;
                    break;

                case LegoMaterial.Wax://Wachs
                    graphic.SetWax(graphic.GetObjectsByNameContainsSearch("Lego_"), 0.8f, 1);
                    useFilledLegs = true;
                    useSpotLight = true;
                    graphic.GlobalSettings.RecursionDepth = 40;
                    //graphic.GetAllObjects().Where(x => x.Name.StartsWith("Wand") || x.Name.StartsWith("Boden")).ToList().ForEach(x => x.Albedo = 0.4f);
                    break;

                case LegoMaterial.WaterIce://Diffuse Media
                    graphic.SetWax(graphic.GetObjectsByNameContainsSearch("Lego_"), 0.5f, 0.1f);
                    useFilledLegs = true;
                    useSpotLight = true;
                    graphic.GlobalSettings.RecursionDepth = 40;
                    break;

                case LegoMaterial.Cloud://Wolke
                    graphic.TransformObjectsToCloud(graphic.GetAllObjects().Where(x => x.Name.Contains("Lego_") && x.Name.Contains("Grid") == false).Select(x => x.Id), 10, 1.3f);
                    useCloud = true;
                    break;
            }


            if (useWater == false)
            {
                graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Water").Id);
            }
            else
            {
                var water = graphic.GetObjectByNameStartsWith("Water");
                water.BrdfModel = BrdfModel.TextureGlass; //Glas ohne Media
                water.GlasIsSingleLayer = true;
                water.SpecularAlbedo = 1;
                water.NormalSource = new NormalFromProcedure() { Function = new NormalProceduralFunctionPerlinNoise() };
                water.TextureFile = "#DDEEFF";
                water.RefractionIndex = 1.33f;
            }

            if (useCloud == false)
            {
                if (useFilledLegs)
                {
                    graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Lego_BeinLinksLoch").Id);
                    graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Lego_BeinRechtsLoch").Id);
                }
                else
                {
                    graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Lego_BeinLinksFull").Id);
                    graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Lego_BeinRechtsFull").Id);
                }
            }


            if (useGrid)
            {
                foreach (var obj in graphic.GetObjectsByNameContainsSearch("Lego_"))
                    if (obj.Name.Contains("Grid") == false) graphic.RemoveObjekt(obj.Id);
            }
            else
            {
                foreach (var obj in graphic.GetObjectsByNameContainsSearch("Lego_"))
                    if (obj.Name.Contains("Grid")) graphic.RemoveObjekt(obj.Id);
            }

            //Lego-Grid-Objekt erzeugen (Im Blender im Edit-Mode alle Vertize markieren W->Remove Doubles;dann Decimate-Modifiert)
            //graphic.TransformObjectsToLego(graphic.GetObjectsByNameContainsSearch("Lego_").Select(x => x.Id));
            //File.WriteAllText("Export.obj", graphic.ExportToWavefront());


            if (useSphere)
            {
                //var torsoBox = graphic.GetBoundingBoxFromObject(torso.Id); //torsoBox.RadiusOutTheBox = 0.132102475f; torsoBox.Center = {new Vector3D(0.293577522f, 0.233518779f, 0.0464745387f)}
                Vector3D sphereCenter = new Vector3D(0.293577522f, 0.233518779f, 0.0464745387f);
                float sphereRadius = 0.132102475f;

                if (material == LegoMaterial.Cloud)
                {
                    var toRemove = graphic.GetAllObjects().Where(x => x.MediaDescription != null).ToList();
                    toRemove.ForEach(x => graphic.RemoveObjekt(x.Id));

                    graphic.TransformObjectsToCloud(new int[] { graphic.AddSphere(sphereRadius, 10, 10, new ObjectPropertys() { Position = sphereCenter }) }, 10, 2.0f);
                }
                else
                {
                    var torso = graphic.GetObjectsByNameContainsSearch("Torso").First();
                    torso.Position = sphereCenter;
                    torso.Size = 1;

                    var toRemove = graphic.GetAllObjects().Where(x => x.Name.StartsWith("Lego")).ToList();
                    toRemove.ForEach(x => graphic.RemoveObjekt(x.Id));

                    int kugel = graphic.AddSphere(sphereRadius, 10, 10, torso);
                    var kugelProp = graphic.GetObjectById(kugel);

                    if (material == LegoMaterial.BlockMode)
                    {
                        var blockKugel = graphic.GetObjectById(graphic.TransformObjectsToLego(new[] { kugel }, 10));
                        blockKugel.Color = torso.Color;
                        blockKugel.NormalInterpolation = InterpolationMode.Flat;
                    }

                    if (material == LegoMaterial.MotionBlure)
                    {
                        kugelProp.MotionBlurMovment = new TranslationMovementEulerDescription() { PositionStart = kugelProp.Position, PositionEnd = kugelProp.Position - new Vector3D(0, sphereRadius, 0), Factor = 2 };
                    }
                }
            }


            graphic.GetObjectByNameStartsWith("Boden").TextureFile = "#85A6CB";

            var box = graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("LichtSpot").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("LichtSpot").Id);
            int licht = graphic.AddSphere(box.RadiusInTheBox, 10, 10, new ObjectPropertys()
            {
                Position = box.Center,
                TextureFile = "#FFFFFF",
                RasterizerLightSource = new RasterizerLightSourceDescription() { SpotDirection = spotDirection, SpotCutoff = 180.0f, SpotExponent = 1, ConstantAttenuation = 0.5f }
            });
            if (useSpotLight)
            {
                graphic.GetObjectById(licht).RaytracingLightSource = new SphereWithSpotLightDescription() { SpotDirection = spotDirection, SpotCutoff = 90, Emission = 50 * 1.5f }; //5
            }
            else
            {
                graphic.RemoveObjekt(licht);
            }


            graphic.GetObjectByNameStartsWith("LichtDecke").RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 200 * 1.5f };//20
            graphic.GetObjectByNameStartsWith("LichtDecke").RasterizerLightSource = new RasterizerLightSourceDescription();


            if (useEnvironmentLight)
            {
                var toRemove = graphic.GetAllObjects().Where(x => x.Name.StartsWith("Boden") || x.Name.StartsWith("Wand") || x.Name.StartsWith("LichtDecke")).ToList();
                toRemove.ForEach(x => graphic.RemoveObjekt(x.Id));

                int backgroundLight = graphic.AddSphere(1, 10, 10, new ObjectPropertys()
                {
                    TextureFile = DataDirectory + "wide_street_01_1k.hdr",
                    //TextureFile = DataDirectory + "piazza_san_marco_1k.hdr",
                    RaytracingLightSource = new EnvironmentLightDescription() { Emission = 4, Rotate = 0.25f } //Emission = 4
                });
            }

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, useEnvironmentLight ? 23 : 40);
            graphic.Mode = Mode3D.FullBidirectionalPathTracing;

            graphic.GlobalSettings.Tonemapping = TonemappingMethod.GammaOnly;
        }

        public static void AddTestscene_AnisotrophTextureFilter(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";
            graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, 0, 0), new Vector3D(0, 0, -1), 45); //Einheitsmatrix bei der LookAtMatrix

            graphic.AddSquareXY(400, 400.0f, 1, new ObjectPropertys() 
            { 
                Color = new ColorFromTexture() { TextureFile = DataDirectory + "Linien.png", TextureFilter = TextureFilter.Anisotroph, TextureMatrix = Matrix3x3.Scale(300,300) }, 
                Position = new Vector3D(0, 1, -10),
                Orientation = new Vector3D(-65, 0, 0), 
                ShowFromTwoSides = false, NormalInterpolation = InterpolationMode.Flat, CanReceiveLight = false 
            });

        }
    }
}
