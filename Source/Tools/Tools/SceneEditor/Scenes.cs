using BitmapHelper;
using GraphicPanels;
using GraphicMinimal;
using System;
using System.Linq;
using System.IO;

namespace Tools.Tools.SceneEditor
{
    public class Scenes
    {
        public static string DataDirectory = FilePaths.DataDirectory;

        public static void CreateSceneFiles()
        {
            GraphicPanel3D graphic = new GraphicPanel3D();

            CreateSingleSceneFile(graphic, "01_RingSphere_json.txt", () => AddTestszene1_RingSphere(graphic));
            CreateSingleSceneFile(graphic, "01_RingSphereWithDiffuseGroundAndNoMotionBlur_json.txt", () => AddTestszene1_RingSphereWithDiffuseGroundAndNoMotionBlur(graphic));
            CreateSingleSceneFile(graphic, "02_NoWindowRoom_json.txt", () => AddTestszene2_NoWindowRoom(graphic));
            CreateSingleSceneFile(graphic, "03_SpheresWithTexture_json.txt", () => AddTestszene3_SpheresWithTexture(graphic));
            CreateSingleSceneFile(graphic, "04_ProceduralTextures_json.txt", () => AddTestszene4_ProceduralTextures(graphic));
            CreateSingleSceneFile(graphic, "05_Cornellbox_json.txt", () => AddTestszene5_Cornellbox(graphic));
            CreateSingleSceneFile(graphic, "05_BlenderCornellbox_json.txt", () => AddTestszene5_BlenderCornellbox(graphic));
            CreateSingleSceneFile(graphic, "05_WaterNoMediaCornellbox_json.txt", () => AddTestszene5_WaterNoMediaCornellbox(graphic));
            CreateSingleSceneFile(graphic, "05_WaterCornellbox_json.txt", () => AddTestszene5_WaterCornellbox(graphic));
            CreateSingleSceneFile(graphic, "05_MirrorCornellbox_json.txt", () => AddTestszene5_MirrorCornellbox(graphic));
            CreateSingleSceneFile(graphic, "05_RisingSmokeCornellbox_json.txt", () => AddTestszene5_RisingSmokeCornellbox(graphic));
            CreateSingleSceneFile(graphic, "06_ChinaRoom_json.txt", () => AddTestszene6_ChinaRoom(graphic));
            CreateSingleSceneFile(graphic, "07_Chessboard_json.txt", () => AddTestszene7_Chessboard(graphic));
            CreateSingleSceneFile(graphic, "08_WindowRoom_json.txt", () => AddTestszene8_WindowRoom(graphic));
            CreateSingleSceneFile(graphic, "09_MultipleImportanceSampling_json.txt", () => AddTestszene9_MultipleImportanceSampling(graphic));
            CreateSingleSceneFile(graphic, "10_MirrorGlassCaustic_json.txt", () => AddTestszene10_MirrorGlassCaustic(graphic));
            CreateSingleSceneFile(graphic, "11_PillarsOfficeGodRay_json.txt", () => AddTestszene11_PillarsOfficeGodRay(graphic));
            CreateSingleSceneFile(graphic, "11_PillarsOfficeMedia_json.txt", () => AddTestszene11_PillarsOfficeMedia(graphic));
            CreateSingleSceneFile(graphic, "11_PillarsOfficeNoMedia_json.txt", () => AddTestszene11_PillarsOfficeNoMedia(graphic));
            CreateSingleSceneFile(graphic, "11_PillarsOfficeNight_json.txt", () => AddTestszene11_PillarsOfficeNight(graphic));
            CreateSingleSceneFile(graphic, "12_Snowman_json.txt", () => AddTestszene12_Snowman(graphic));
            CreateSingleSceneFile(graphic, "13_MicrofacetGlas_json.txt", () => AddTestszene13_MicrofacetGlas(graphic));
            CreateSingleSceneFile(graphic, "14_MicrofacetSundial_json.txt", () => AddTestszene14_MicrofacetSundial(graphic));
            CreateSingleSceneFile(graphic, "15_MicrofacetSphereBox_json.txt", () => AddTestszene15_MicrofacetSphereBox(graphic));
            CreateSingleSceneFile(graphic, "16_Graphic6Memories_json.txt", () => AddTestszene16_Graphic6Memories(graphic));
            CreateSingleSceneFile(graphic, "17_TheFifthElement_json.txt", () => AddTestszene17_TheFifthElement(graphic));
            CreateSingleSceneFile(graphic, "18_CloudsForTestImage_json.txt", () => TestSzene18_CloudsForTestImage(graphic));
            CreateSingleSceneFile(graphic, "19_Stilllife_json.txt", () => AddTestszene19_StillLife(graphic));
            CreateSingleSceneFile(graphic, "20_Mirrorballs_json.txt", () => AddTestszene20_Mirrorballs(graphic));
            CreateSingleSceneFile(graphic, "21_Candle_json.txt", () => AddTestszene21_Candle(graphic));
            CreateSingleSceneFile(graphic, "22_ToyBox_json.txt", () => AddTestszene22_ToyBox(graphic));
            CreateSingleSceneFile(graphic, "23_MirrorShadowWithSphere_json.txt", () => AddTestszene23_MirrorShadowWithSphere(graphic));
            CreateSingleSceneFile(graphic, "23_MirrorShadowNoSphere_json.txt", () => AddTestszene23_MirrorShadowNoSphere(graphic));
            CreateSingleSceneFile(graphic, "24_EnvironmentMaterialTest_json.txt", () => AddTestszene24_EnvironmentMaterialTest(graphic));
            CreateSingleSceneFile(graphic, "25_SingleSphereForRapso_json.txt", () => AddTestszene25_SingleSphereForRapso(graphic));
            CreateSingleSceneFile(graphic, "26_SkyEnvironmapCreator_json.txt", () => AddTestszene26_SkyEnvironmapCreator(graphic));
            CreateSingleSceneFile(graphic, "27_MirrorsEdge_json.txt", () => AddTestszene27_MirrorsEdge(graphic));
            CreateSingleSceneFile(graphic, "32_LivingRoom_json.txt", () => AddTestszene32_LivingRoom(graphic));
        }

        private static void CreateSingleSceneFile(GraphicPanel3D graphic, string sceneFile, Action addScene)
        {
            addScene();

            //Schritt 1: Kontroll-Datei anlegen
            string all1 = graphic.GetBigExportData();            

            //Schritt 2: Scene-Datei erzeugen
            string json = graphic.GetExportDataAsJson().Replace(DataDirectory.Replace("\\", "\\\\"), "<DataFolder>");
            File.WriteAllText(DataDirectory + sceneFile, json);

            //Schritt 3: Kontrollladen (Prüfe das Scene vor den Speichern == Nach den Laden)          
            LoadScene(graphic, sceneFile);

            string all2 = graphic.GetBigExportData();
            if (all1.Length != all2.Length) throw new Exception($"Error saving or loading the scene all1.Length={all1.Length} all2.Length={all2.Length}");
            for (int i = 0; i < all1.Length && i < all2.Length; i++)
            {
                if (all1[i] != all2[i]) throw new Exception($"Error saving or loading the scene i={i} all1[i]={all1[i]} all2[i]={all2[i]}");
            }
        }

        public static void LoadScene(GraphicPanel3D graphic, string sceneFile)
        {
            graphic.LoadExportDataFromJson(File.ReadAllText(DataDirectory + sceneFile).Replace("<DataFolder>", DataDirectory.Replace("\\", "\\\\")));
        }


        //Hier soll geprüft werden, ob all die Sachen aus der Lokalen Beleuchtung (Schatten, Transparenz, Flat-Shadding, Bumpmpapping, Parallax-Mapping) gehen
        public static void AddTestszene1_RingSphere(GraphicPanel3D graphic)
        {
            bool addBlob = false;

            float sizeFactor = 0.1f;

            graphic.RemoveAllObjekts();

            //Ground
            graphic.AddSquareXY(1, 1, 1, new ObjectPropertys() { Position = new Vector3D(0, 0, 0), Orientation = new Vector3D(-90, 0, 180), Size = 80, Color = new ColorFromTexture() { TextureFile = DataDirectory + "Decal.bmp", TextureMatrix = Matrix3x3.Scale(3, 3) }, ShowFromTwoSides = true, BrdfModel = BrdfModel.Tile, SpecularAlbedo = 0.8f, TileDiffuseFactor = 0.5f, NormalInterpolation = InterpolationMode.Flat, NormalSource = new NormalFromObjectData() });
            
            graphic.AddRing(0.3f, 2, 5, 20, new ObjectPropertys() { Position = new Vector3D(0, 20, 0), Orientation = new Vector3D(0, 0, 45), Size = 10, HasStencilShadow = true, NormalInterpolation = InterpolationMode.Smooth, TextureFile = "#004400", SpecularHighlightPowExponent = 50, BrdfModel = BrdfModel.PlasticDiffuse, ShowFromTwoSides = false });
            graphic.AddSphere(1, 10, 10, new ObjectPropertys() //LightSource
            {
                Position = new Vector3D(0, 75, 30),
                Orientation = new Vector3D(0, 0, 45),
                Size = 10.0f,
                TextureFile = "#FFFFFF",
                ShowFromTwoSides = true,
                RasterizerLightSource = new RasterizerLightSourceDescription() { SpotDirection = Vector3D.Normalize(new Vector3D(0, 30, 0) - new Vector3D(0, 75, 30)), SpotCutoff = 180.0f, SpotExponent = 1, ConstantAttenuation = 1.1f },
                RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = 220000 * sizeFactor * sizeFactor }
            });

            graphic.AddSphere(1, 10, 10, new ObjectPropertys() { Position = new Vector3D(0, 30, 0), Orientation = new Vector3D(-45, 0, 45), Size = 10, TextureFile = @"#000033", NormalSource = new NormalFromMap() { NormalMap = DataDirectory + "bumpmap.png" }, NormalInterpolation = InterpolationMode.Smooth, SpecularAlbedo = 0.6f, BrdfModel = BrdfModel.PlasticDiffuse, SpecularHighlightPowExponent = 20, SpecularHighlightCutoff1 = 1, SpecularHighlightCutoff2 = 5 });

            graphic.AddBottle(1, 2, 6, new ObjectPropertys() { Position = new Vector3D(5, 12, 30), Orientation = new Vector3D(0, 40, 0), Size = 5, BlackIsTransparent = true, ShowFromTwoSides = true, NormalInterpolation = InterpolationMode.Flat, TextureFile = DataDirectory + "image1.bmp", SpecularHighlightPowExponent = 0 });

            if (addBlob)
            {
                graphic.AddBlob(new Vector3D[] { new Vector3D(0, 0, 0), new Vector3D(13, 0, 0), new Vector3D(25, 5, 0) }, 7, new ObjectPropertys() { Position = new Vector3D(20, 20, 10), TextureFile = "#FFFF00", BrdfModel = BrdfModel.Diffus });
            }else
            {
                //Mario1 ohne Bewegungsunschärfe
                graphic.Add3DBitmap(DataDirectory + "Mario.png", 2, new ObjectPropertys() { Position = new Vector3D(-40, 13, 3), Orientation = new Vector3D(0, 70, 0), Size = 0.5f, NormalInterpolation = InterpolationMode.Flat, TextureFile = DataDirectory + "Mario.png", BrdfModel = BrdfModel.Diffus, ShowFromTwoSides = false });

                //Mario2 mit Bewegungsunschärfe
                graphic.Add3DBitmap(DataDirectory + "Mario.png", 2, new ObjectPropertys() { Position = new Vector3D(+30, 13, 3), Orientation = new Vector3D(0, 70 + 90, 0), Size = 0.5f, NormalInterpolation = InterpolationMode.Flat, TextureFile = DataDirectory + "Mario.png", BrdfModel = BrdfModel.Diffus, ShowFromTwoSides = false, MotionBlurMovment = new TranslationMovementEulerDescription() { PositionStart = new Vector3D(+30, 13, 3) * sizeFactor, PositionEnd = (new Vector3D(+30, 13, 3) + new Vector3D(5, 0, 0)) * sizeFactor, Factor = 2 } });
            }


            foreach (var obj in graphic.GetAllObjects())
            {
                obj.Position *= sizeFactor;
                obj.Size *= sizeFactor;
                obj.Albedo = 0.8f; //Da ich ohne Tonemapping arbeiten will, verwende ich ein hohen Albedo
            }

            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";
            graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, 70, 100) * sizeFactor, new Vector3D(0, -0.5f, -1), 45.0f);
        }

        //Der Raytracersimple erzeugt nur dann das gleiche Bild, wenn der Boden diffuse ist und kein MotionBlur-Effekt verwendet wird
        public static void AddTestszene1_RingSphereWithDiffuseGroundAndNoMotionBlur(GraphicPanel3D graphic)
        {
            AddTestszene1_RingSphere(graphic);
            graphic.GetObjectById(1).BrdfModel = BrdfModel.Diffus;
            graphic.GetObjectById(7).MotionBlurMovment = null;
        }

        //Hier soll die Globale Beleuchtung überprüft werden (Schwere Testszene)
        public static void AddTestszene2_NoWindowRoom(GraphicPanel3D graphic)
        {
            bool useLowPoly = false;

            graphic.RemoveAllObjekts();

            string file = (useLowPoly ? "02_NoWindowRoom_low.obj" : "02_NoWindowRoom.obj");

            graphic.AddWaveFrontFileAndSplit(DataDirectory + file, false, new ObjectPropertys() { ShowFromTwoSides = true });

            graphic.GetAllObjects().ForEach(x => x.Albedo = 0.8f); //Da ich ohne Tonemapping arbeiten will, verwende ich ein hohen Albedo

            Vector3D tableLampBeamDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("TischlampeAnstrahlpunkt").Position - graphic.GetObjectByNameStartsWith("Glübirne_Tischlampe").Position);
            graphic.RemoveObjectStartsWith("TischlampeAnstrahlpunkt");

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("Start").Position;
            Vector3D cameraRichtung = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("Ende").Position - cameraStart);
            graphic.RemoveObjectStartsWith("Start");
            graphic.RemoveObjectStartsWith("Ende");

            float lightFactor = 50;

            var tableLamp = graphic.GetObjectByNameStartsWith("Glübirne_Tischlampe");
            tableLamp.RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = 0.8f * lightFactor };
            tableLamp.RasterizerLightSource = new RasterizerLightSourceDescription()
            {
                SpotDirection = tableLampBeamDirection,
                SpotCutoff = 30,
                ConstantAttenuation = 0.04f * lightFactor,
            };

            var floorLamp = graphic.GetObjectByNameStartsWith("Glübirne_Stehlampe");
            floorLamp.RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = 0.1f * lightFactor };
            floorLamp.RasterizerLightSource = new RasterizerLightSourceDescription()
            {
                SpotDirection = new Vector3D(0, 1, 0),
                SpotCutoff = 30,
                ConstantAttenuation = 0.08f * lightFactor,
            };

            var ceilingLamp1 = graphic.GetObjectByNameStartsWith("Deckenlampe1");
            ceilingLamp1.RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 1 * lightFactor };
            ceilingLamp1.RasterizerLightSource = new RasterizerLightSourceDescription()
            {
                SpotDirection = new Vector3D(0, -1, 0),
                SpotCutoff = 180,
                ConstantAttenuation = 0.1f * lightFactor,
            };

            var ceilingLamp2 = graphic.GetObjectByNameStartsWith("Deckenlampe2");
            ceilingLamp2.RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 1 * lightFactor };
            ceilingLamp2.RasterizerLightSource = new RasterizerLightSourceDescription()
            {
                SpotDirection = new Vector3D(0, -1, 0),
                SpotCutoff = 180,
                ConstantAttenuation = 0.1f * lightFactor,
            };

            //Wände=#888888; Kugel=#004400; Knauf=#443322; Tischlampe=#006666; Stehlampe=#AACCEE; Bild="dali_das_raetsel_der_begierde_0.jpg"
            //string blenderHex = PixelHelper.ColorStringToGammaCorrectColorString("#888888");

            var cylinder = graphic.GetObjectByName("Zylinder");
            cylinder.BlackIsTransparent = true;
            cylinder.Color.As<ColorFromTexture>().TextureMatrix = Matrix3x3.Scale(2, 2);

            var table = graphic.GetObjectByName("Tisch");
            table.Color.As<ColorFromTexture>().TextureMatrix = Matrix3x3.Scale(0.5f, 0.5f);

            var mirror = graphic.GetObjectByName("Spiegel");
            mirror.BrdfModel = BrdfModel.Phong;
            mirror.GlossyColor = new Vector3D(1, 1, 1);
            mirror.GlossyPowExponent = 1000;
            mirror.IsMirrorPlane = true;

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraRichtung, 80);
        }

        //Hier soll das Textturmapping von Kugeln überprüft werden
        public static void AddTestszene3_SpheresWithTexture(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();

            graphic.AddSphere(1, 10, 10, new ObjectPropertys() //Light
            {
                Position = new Vector3D(0, 75, 30),
                Orientation = new Vector3D(0, 0, 45),
                Size = 10,
                TextureFile = "#FFFFFF",
                ShowFromTwoSides = true,
                RasterizerLightSource = new RasterizerLightSourceDescription() { SpotDirection = Vector3D.Normalize(new Vector3D(0, 30, 0) - new Vector3D(0, 75, 30)), SpotCutoff = 180.0f, SpotExponent = 1, ConstantAttenuation = 1.5f },
                RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = 220000 }
            });

            for (int i = 0; i < 10; i++)
                graphic.AddSphere(1, 30, 30, new ObjectPropertys()
                {
                    Position = new Vector3D(-50 + i * 15, 60 - i * 15, 0),
                    Orientation = new Vector3D(45 + i * 30, 0, 45 + i * 20),
                    Size = 10,
                    Color = new ColorFromTexture() { TextureFile = DataDirectory + "nes_super_mario_bros.png", TextureMatrix = Matrix3x3.Scale(10, 10), },
                    NormalSource = new NormalFromMap() { NormalMap = DataDirectory + "Huckel.bmp", TextureMatrix = Matrix3x3.Scale(10, 10) },
                    NormalInterpolation = InterpolationMode.Smooth
                });

            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";
            graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, 70, 100), new Vector3D(0, -0.5f, -1), 45.0f);
        }

        //Hier sollen procedurale Texturen getestet werden
        public static void AddTestszene4_ProceduralTextures(GraphicPanel3D graphic)
        {
            float sizeFactor = 0.1f;
            graphic.RemoveAllObjekts();

            //Lightsource
            graphic.AddSquareXY(5, 1, 1, new ObjectPropertys() { Position = new Vector3D(10, 20 + 20, 30 + 15) * sizeFactor, Size = 3 * sizeFactor, Orientation = new Vector3D(100, 0, 0), TextureFile = @"#FFFFFF", RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 100 * sizeFactor * sizeFactor }, RasterizerLightSource = new RasterizerLightSourceDescription() });

            int cube = graphic.AddCube(10, 10, 10, new ObjectPropertys() { Position = new Vector3D(0, -10, 10) * sizeFactor, Size = 6 * sizeFactor, Color = new ColorFromTexture() { TextureFile = DataDirectory + "Envwall.bmp", TextureMatrix = Matrix3x3.Scale(3, 3) }, BrdfModel = BrdfModel.Diffus, ShowFromTwoSides = true,  });
            graphic.FlipNormals(cube);

            graphic.AddCube(0.8f, 4, 0.8f, new ObjectPropertys() { Position = new Vector3D(-20, 6, 15) * sizeFactor, Size = 3 * sizeFactor, Color = new ColorFromProcedure() { ColorProceduralFunction = ColorProceduralFunction.Hatch, ColorString = @"#FBFFFB" } });
            graphic.AddCube(0.8f, 4, 0.8f, new ObjectPropertys() { Position = new Vector3D(-10, 6, 12) * sizeFactor, Size = 3 * sizeFactor, Color = new ColorFromProcedure() { ColorProceduralFunction = ColorProceduralFunction.Wood, ColorString = @"#FBFFFB" } });
            graphic.AddCube(0.8f, 4, 0.8f, new ObjectPropertys() { Position = new Vector3D(0, 6, 9) * sizeFactor, Size = 3 * sizeFactor, Color = new ColorFromProcedure() { ColorProceduralFunction = ColorProceduralFunction.Tile, ColorString = @"#FBFFFB" } });
            graphic.AddCube(0.8f, 4, 0.8f, new ObjectPropertys() { Position = new Vector3D(+10, 6, 9) * sizeFactor, Size = 3 * sizeFactor, Color = new ColorFromProcedure() { ColorProceduralFunction = ColorProceduralFunction.ToonShader, ColorString = @"#FBFFFB" } });
            graphic.AddCube(0.8f, 4, 0.8f, new ObjectPropertys() { Position = new Vector3D(+20, 6, 12) * sizeFactor, Size = 3 * sizeFactor, TextureFile = @"#FBFFFB", NormalSource = new NormalFromProcedure() { Function = new NormalProceduralFunctionPerlinNoise() { NormalNoiseFactor = 0.1f } } });
            graphic.AddCube(0.8f, 4, 0.8f, new ObjectPropertys() { Position = new Vector3D(+30, 6, 15) * sizeFactor, Size = 3 * sizeFactor, TextureFile = @"#FBFFFB", NormalSource = new NormalFromProcedure() { Function = new NormalProceduralFunctionSinUCosV() } });

            graphic.AddSphere(1, 6, 6, new ObjectPropertys() { Position = new Vector3D(-20, -5, 25) * sizeFactor, Size = 3 * sizeFactor, Color = new ColorFromProcedure() { ColorProceduralFunction = ColorProceduralFunction.Hatch, ColorString = @"#FBFFFB" } });
            graphic.AddSphere(1, 6, 6, new ObjectPropertys() { Position = new Vector3D(-10, -5, 22) * sizeFactor, Size = 3 * sizeFactor, Color = new ColorFromProcedure() { ColorProceduralFunction = ColorProceduralFunction.Wood, ColorString = @"#FBFFFB" } });
            graphic.AddSphere(1, 6, 6, new ObjectPropertys() { Position = new Vector3D(0, -5, 19) * sizeFactor, Size = 3 * sizeFactor, Color = new ColorFromProcedure() { ColorProceduralFunction = ColorProceduralFunction.Tile, ColorString = @"#FBFFFB" } });
            graphic.AddSphere(1, 6, 6, new ObjectPropertys() { Position = new Vector3D(+10, -5, 19) * sizeFactor, Size = 3 * sizeFactor, Color = new ColorFromProcedure() { ColorProceduralFunction = ColorProceduralFunction.ToonShader, ColorString = @"#FBFFFB" } });
            graphic.AddSphere(1, 6, 6, new ObjectPropertys() { Position = new Vector3D(+20, -5, 22) * sizeFactor, Size = 3 * sizeFactor, TextureFile = @"#FBFFFB", NormalSource = new NormalFromProcedure() { Function = new NormalProceduralFunctionPerlinNoise() { NormalNoiseFactor = 0.1f } } });
            graphic.AddSphere(1, 6, 6, new ObjectPropertys() { Position = new Vector3D(+30, -5, 25) * sizeFactor, Size = 3 * sizeFactor, TextureFile = @"#FBFFFB", NormalSource = new NormalFromProcedure() { Function = new NormalProceduralFunctionSinUCosV() } });

            //Ground
            graphic.AddSquareXY(10, 10, 1, new ObjectPropertys() { Position = new Vector3D(0, -6, 0) * sizeFactor, Orientation = new Vector3D(-90, 0, 180), Size = 6 * sizeFactor, Color = new ColorFromTexture() { TextureFile = DataDirectory + "Decal.bmp", TextureMatrix = Matrix3x3.Scale(4, 4) }, NormalInterpolation = InterpolationMode.Flat, BrdfModel = BrdfModel.Diffus });

            graphic.GlobalSettings.Camera = new Camera(new Vector3D(5, 40, 60) * sizeFactor, new Vector3D(-0.0f, -0.4f, -0.5f), 45.0f);//Für die Kugeln + Würfel
        }

        //Cornellbox
        public static void AddTestszene5_Cornellbox(GraphicPanel3D graphic)
        {
            bool addSphere = true;
            bool groundIsRough = false;
            bool leftCubeIsRough = false;

            graphic.RemoveAllObjekts();

            int[] ids = graphic.AddCornellBox();

            if (addSphere)
            {
                int sphere = graphic.AddSphere(0.1f, 10, 10, new ObjectPropertys()
                {
                    Orientation = new Vector3D(0, 90, 0),
                    Position = new Vector3D(0.1f, 0.1f, -0.1f),
                    ShowFromTwoSides = true,
                    RefractionIndex = 1.5f,
                    BrdfModel = BrdfModel.TextureGlass,
                    TextureFile = "#FFFFFF"
                });
            }
            
            if (groundIsRough)
            {
                //Fußboden
                graphic.GetObjectById(ids[1]).BrdfModel = BrdfModel.Phong;
                graphic.GetObjectById(ids[1]).TextureFile = "#FFFFFF";
            }
            
            if (leftCubeIsRough)
            {
                //Linker Würfel
                graphic.GetObjectById(ids[6]).BrdfModel = BrdfModel.HeizMetal;
                graphic.GetObjectById(ids[6]).NormalSource = new NormalFromMicrofacet();
            }
            

            graphic.GetAllObjects().ForEach(x => x.Albedo = 0.8f); //Da ich ohne Tonemapping arbeiten will, verwende ich ein hohen Albedo

            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";
            graphic.GlobalSettings.Camera = new Camera(new Vector3D(0.278f, 0.275f, 0.789f), new Vector3D(0, 0, -1), 38);
        }

        //Blender-Cornellbox
        public static void AddTestszene5_BlenderCornellbox(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "05_BlenderCornellbox.obj", false, new ObjectPropertys() { Albedo = 0.8f });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            graphic.GetObjectByNameStartsWith("Lampe").RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 2 };
            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 38);
        }

        //Blender-Cornellbox (Wasser ohne Media)
        public static void AddTestszene5_WaterNoMediaCornellbox(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "05_WaterNoMediaCornellbox.obj", false, new ObjectPropertys());

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            graphic.GetObjectByNameStartsWith("Wasser").BrdfModel = BrdfModel.TextureGlass;
            graphic.GetObjectByNameStartsWith("Wasser").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("Wasser").RefractionIndex = 1.5f;

            graphic.GetObjectByNameStartsWith("Lampe").RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 5 };

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 38);
        }

        //Wasser-Cornellbox (Wasser mit Media)
        public static void AddTestszene5_WaterCornellbox(GraphicPanel3D graphic)
        {
            //Beim MediaSubpathsampler muss jedes Objekt, was aus Glas oder Luft ist(Ein Brechungsindex besitzt) ein Medium zugewiesen bekomme, da der Brechungsindex
            //nur bei MediaBorder/NullMediaBorder ausgewertet wird
            //Ausßerdem muss die Prioität so angeordnet werden, dass die Objekte, die drinnen liegen, eine höhere Prio haben.
            //Luft-Würfel ist außen und bekommt die 2; Wasser1 (Wasseroberfläche) ist in der Luft enthalten und umschließt gedanklich Wasser2;
            //Wasser2 ist in Wasser1 und bekommt somit noch höhere Prio. RechterWürfel soll Wasser1 und Wasser2 überdecken und bekommt somit höchste Prio

            float sizeFactor = 100; //Wenn die Szenengröße zu klein ist, ist die Segmentlänge vom Wasser kleiner als die MinSegmentLength wordurch dann kein Scattering im Wasser passiert
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "05_WaterCornellbox.obj", false, new ObjectPropertys() { Size = 0.1f * sizeFactor, Albedo = 0.8f });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            //Die Luft
            graphic.GetObjectByNameStartsWith("LuftWuerfel").RefractionIndex = 1; //Wenn der Brechungsindex 1 ist, dann hat das Medium kein Glas-Rand
            graphic.GetObjectByNameStartsWith("LuftWuerfel").MediaDescription =
                new DescriptionForHomogeneousMedia()
                {
                    Priority = 2,
                    AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 2.0f / sizeFactor,
                    //ScatteringCoeffizent = new Vector3D(0.5f, 0.5f, 1) * 2.0f, //Blaue Godrays
                    ScatteringCoeffizent = new Vector3D(1, 1, 1) * 2.0f / sizeFactor, //Weiße Godrays
                    AnisotropyCoeffizient = 0.30f,
                    PhaseFunctionExtraFactor = 1
                };


            //Wasseroberfläche aus Glas
            graphic.GetObjectByNameStartsWith("Wasser1").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Wasser1").NormalSource = new NormalFromObjectData();
            graphic.GetObjectByNameStartsWith("Wasser1").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("Wasser1").BrdfModel = BrdfModel.MirrorGlass;
            graphic.GetObjectByNameStartsWith("Wasser1").GlasIsSingleLayer = true;
            graphic.GetObjectByNameStartsWith("Wasser1").MirrorColor = new Vector3D(1, 1, 1);
            graphic.GetObjectByNameStartsWith("Wasser1").RefractionIndex = 1.5f;
            //Hinweis: Obwohl Wasser1 ja nur eine Fläche ist und kein Objekt, wo ein was drin ist muss ich trotzdem dem ein Medium hinzufügen, da der MediaSubpathsampler nur
            //bei MediaBorder-Punkten den Brechungsindex verwendet und dann daraus eine Glas-Brdf erstellt.
            graphic.GetObjectByNameStartsWith("Wasser1").MediaDescription = new DescriptionForVacuumMedia() { Priority = 3 };
            
            //Wasserpartikel (Wenn ich das hier als Glas mache, sehen die Kugeln verzehrt aus)
            graphic.GetObjectByNameStartsWith("Wasser2").RefractionIndex = 1.0f; //Wenn der Brechungsindex 1 ist, dann hat das Medium kein Glas-Rand
            graphic.GetObjectByNameStartsWith("Wasser2").MediaDescription =
                new DescriptionForHomogeneousMedia()
                {
                    Priority = 4,
                    AbsorbationCoeffizent = new Vector3D(1, 1, 0.5f) * 40 / sizeFactor,
                    ScatteringCoeffizent = new Vector3D(0, 0, 1) * 1 / sizeFactor,
                    PhaseFunctionExtraFactor = 5,
                    AnisotropyCoeffizient = 0.30f,
                };

            //Rechter Würfel
            graphic.GetObjectByNameStartsWith("WuerfelRechts").TextureFile = "#CCFFCC";
            graphic.GetObjectByNameStartsWith("WuerfelRechts").BrdfModel = BrdfModel.TextureGlass;
            graphic.GetObjectByNameStartsWith("WuerfelRechts").RefractionIndex = 1.5f;
            graphic.GetObjectByNameStartsWith("WuerfelRechts").MediaDescription = new DescriptionForVacuumMedia() { Priority = 5 };

            //Die Kugeln
            foreach (var sphere in graphic.GetObjectsByNameContainsSearch("Kugel"))
            {
                sphere.BrdfModel = BrdfModel.MicrofacetTile;
                sphere.NormalSource = new NormalFromMicrofacet();
                sphere.NormalInterpolation = InterpolationMode.Smooth;

                graphic.TransformToSphere(sphere.Id);
            }

            //Decken-Richtungslicht
            foreach (var lamp in graphic.GetObjectsByNameContainsSearch("Lampe"))
            {
                //lamp.TextureFile = "#AAAAFF"; //Blaues Licht
                lamp.TextureFile = "#FFFFFF"; //Weißes Licht
                lamp.RaytracingLightSource = new SurfaceWithSpotLightDescription()
                {
                    Emission = 0.03f * (sizeFactor * sizeFactor),
                    SpotMix = 0.1f, //10% gehen Richtung SpotDirection und der Rest diffus
                    SpotDirection = new Vector3D(-0.253765255f, -0.934307277f, 0.250345945f), //Vector3D spotDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("GroßeKugel1").Position - graphic.GetObjectByNameStartsWith("Lampe4").Position);
                    SpotCutoff = 5.1f
                };
            }

            //Umgebungslicht
            graphic.GetObjectByNameStartsWith("Umgebungslicht").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Umgebungslicht").RaytracingLightSource = new SurfaceWithSpotLightDescription()
            {
                Emission = 0.01f * (sizeFactor * sizeFactor),
                SpotMix = 0.9f, //90% Spotlicht
                SpotCutoff = 5, //5 Grad Öffnungswinkel
            };

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 38);
            graphic.GlobalSettings.RecursionDepth = 40;

            graphic.GlobalSettings.Tonemapping = TonemappingMethod.None;
            graphic.GlobalSettings.SamplingCount = 1000;
            graphic.Mode = Mode3D.MediaFullBidirectionalPathTracing; //MediaBidirectionalPathTracing geht auch
        }

        //Spiegel-Cornellbox e^(-x*(A+S)) = e^(-10*x*(A+S)*f) -> f=0.1
        public static void AddTestszene5_MirrorCornellbox(GraphicPanel3D graphic)
        {
            bool withoutCubes = false;

            //Wenn ich Zahlen zwischen 1 und 1000 wähle klappt das.
            //Nehme ich Zahl kleiner 1, dann sieht das Bild anders aus. Das liegt an den MagicNumbers.MinAllowedPathPointDistance. 
            //Diese Zahl muss im Verhältnis zur Szenengröße deutlich kleiner sein.
            float sizeFactor = 100.0f; //Quadratische Zusammenhang beim Emissionterm; Linearer Zusammenhang beim ScatteringCoeffizient
                                       //Hinweis: Wird die Zahl zu klein, dann bekommt man Ärger mit MagicNumbers.MinAllowedPathPointDistance

            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "05_MirrorCornellbox.obj", false, new ObjectPropertys() { Size = 0.1f * sizeFactor, Albedo = 0.8f });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            //Die Luft
            graphic.GetObjectByNameStartsWith("LuftWuerfel").RefractionIndex = 1; //Wenn der Brechungsindex 1 ist, dann hat das Medium kein Glas-Rand
            graphic.GetObjectByNameStartsWith("LuftWuerfel").MediaDescription =
                new DescriptionForHomogeneousMedia()
                {

                    AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 2.0f / sizeFactor,
                    //ScatteringCoeffizent = new Vector3D(0.5f, 0.5f, 1) * 2.0f, //Blaue Godrays
                    ScatteringCoeffizent = new Vector3D(1, 1, 1) * 2.0f / sizeFactor, //Weiße Godrays
                    AnisotropyCoeffizient = 0.30f,
                    PhaseFunctionExtraFactor = 1
                };
            //var box = graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("LuftWuerfel").Id);

            //Rechter Würfel
            graphic.GetObjectByNameStartsWith("WuerfelRechts").TextureFile = "#CCFFCC";
            graphic.GetObjectByNameStartsWith("WuerfelRechts").BrdfModel = BrdfModel.TextureGlass;
            graphic.GetObjectByNameStartsWith("WuerfelRechts").RefractionIndex = 1.5f;
            graphic.GetObjectByNameStartsWith("WuerfelRechts").MediaDescription = new DescriptionForVacuumMedia(); //Da der Media-Tracer verwendet wird, müssen zwangsweise alle Glasobjekte mit ein Medium gefüllt werden. Selbst wenn es Vacuum ist

            //Linker Würfel
            graphic.GetObjectByNameStartsWith("WuerfelLinks").TextureFile = "#CCFFCC";
            graphic.GetObjectByNameStartsWith("WuerfelLinks").BrdfModel = BrdfModel.TextureGlass;
            graphic.GetObjectByNameStartsWith("WuerfelLinks").RefractionIndex = 1.5f;
            graphic.GetObjectByNameStartsWith("WuerfelLinks").MediaDescription = new DescriptionForVacuumMedia(); //Da der Media-Tracer verwendet wird, müssen zwangsweise alle Glasobjekte mit ein Medium gefüllt werden. Selbst wenn es Vacuum ist

            if (withoutCubes)
            {
                graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("WuerfelLinks").Id);
                graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("WuerfelRechts").Id);
            }            

            //Die Spiegel
            foreach (var spiegel in graphic.GetObjectsByNameContainsSearch("Spiegel"))
            {
                spiegel.BrdfModel = BrdfModel.Mirror;
                spiegel.TextureFile = "#FFFFFF";
            }

            var lamp = graphic.GetObjectByNameStartsWith("Lampe");
            lamp.TextureFile = "#FFFFFF"; //Weißes Licht
            lamp.RaytracingLightSource = new SurfaceWithSpotLightDescription()
            {
                Emission = 0.1f * (sizeFactor * sizeFactor),
                SpotMix = 0.20f, //20% gehen Richtung SpotDirection und der Rest diffus                
                SpotCutoff = 1 //1 Grad
            };

            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";
            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 38);
            graphic.GlobalSettings.SamplingCount = 10000;
            graphic.GlobalSettings.PhotonCount = 20000;
            graphic.GlobalSettings.MetropolisBootstrapCount = 10000;
            graphic.Mode = Mode3D.MediaVCM;
        }

        //RisingSmoke-Cornellbox
        public static void AddTestszene5_RisingSmokeCornellbox(GraphicPanel3D graphic)
        {
            float sizeFactor = 1;
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "05_RisingSmokeCornellbox.obj", false, new ObjectPropertys() { Size = sizeFactor, Albedo = 0.8f });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            graphic.GetObjectByNameStartsWith("Lampe").RaytracingLightSource = new DiffuseSurfaceLightDescription()
            {
                Emission = 2,
            };

            float smokeRadius1 = graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("Cylinder1").Id).XSize / graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("Rauch1").Id).XSize;
            graphic.GetObjectByNameStartsWith("Rauch1").RefractionIndex = 1;
            graphic.GetObjectByNameStartsWith("Rauch1").MediaDescription = new DescriptionForRisingSmokeMedia()
            {
                RandomSeed = 0,
                ScatteringCoeffizent = new Vector3D(0, 1, 0) * 100 / sizeFactor,
                AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 100 / sizeFactor,
                AnisotropyCoeffizient = 0.30f,
                MinRadius = smokeRadius1
            };

            float smokeRadius2 = graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("Cylinder2").Id).XSize / graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("Rauch2").Id).XSize;
            graphic.GetObjectByNameStartsWith("Rauch2").RefractionIndex = 1;
            graphic.GetObjectByNameStartsWith("Rauch2").MediaDescription = new DescriptionForRisingSmokeMedia()
            {
                RandomSeed = 1,
                ScatteringCoeffizent = new Vector3D(0, 0, 1) * 50 / sizeFactor,
                AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 50 / sizeFactor,
                AnisotropyCoeffizient = 0.30f,
                MinRadius = smokeRadius2,
            };

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 38);
        }

        //China-Raum; 100k=6 Monate
        //Mit den TonemappingTest und der ChinaFireMask.png dann noch die Fireflys weg machen und mit ACESFilmicToneMapping speichern
        public static void AddTestszene6_ChinaRoom(GraphicPanel3D graphic)
        {
            //Gelerntes Wissen:
            //-Man braucht eine Pfadlänge von 30, um schwarze Ränder am Glas zu vermeiden
            //-Um so kleiner das Albedo der Wände/Boden, um so heller wird der Lichtfleck vom direkten Licht und um so dunkler werden die Wände, die kein direktes Licht bekommen (Hoher Konstrast bei kleinen Albedo)

            int contrastLevel = 0;
            float allAlbedo = float.NaN, letterAlbedo = float.NaN, luminance = float.NaN;
            if (contrastLevel == 0) //Hoher Kontrast / Dunkler Ecken (Dunkler Abstellraum) -> Sieht am besten aus
            {
                allAlbedo = 0.3f;
                letterAlbedo = 0.1f;
                luminance = 2000; //0.3² * 2000 = 180
            }
            if (contrastLevel == 1) //Mittel1 Kontrast (Mitteldunkler Abstellraum)
            {
                allAlbedo = 0.35f;
                letterAlbedo = 0.13f;
                luminance = 1500;// 1469; //=180 / 0.35²
            }
            if (contrastLevel == 2) //Mittel1 Kontrast (Mitteldunkler Abstellraum)
            {
                allAlbedo = 0.45f;
                letterAlbedo = 0.2f;
                luminance = 888; //=180 / 0.45²
            }
            if (contrastLevel == 3) //Mittel2 Kontrast (Mitteldunkler Abstellraum)
            {
                allAlbedo = 0.6f;
                letterAlbedo = 0.3f;
                luminance = 600;
            }
            if (contrastLevel == 4) //Kleiner Kontrast; Wände sind mehr gleichmäßig hell (Heller Abstellraum)
            {
                allAlbedo = 0.8f;
                letterAlbedo = 0.5f;
                luminance = 300;
            }

            graphic.RemoveAllObjekts(); //0 = Schräge Ebene mit Abstrand; 1 = Ebene direkt am Fenster; 2 = Kugel
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "06_ChinaRoom.obj", false, new ObjectPropertys() { Size = 0.1f, Albedo = allAlbedo });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraRichtung").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraRichtung").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            var idsWoodLight = graphic.GetObjectsByNameContainsSearch("HolzHell");
            foreach (var id in idsWoodLight)
            {
                id.TextureFile = DataDirectory + "holz_mittel.jpg";
            }

            var idsWoodMiddle = graphic.GetObjectsByNameContainsSearch("HolzMittel");
            foreach (var id in idsWoodMiddle)
            {
                id.TextureFile = DataDirectory + "holz_dunkel1.jpg";
            }

            var windowWood = graphic.GetObjectsByNameContainsSearch("FensterHolz");
            foreach (var id in windowWood)
            {
                id.TextureFile = "#888888";
            }

            var idsBlack = graphic.GetObjectsByNameContainsSearch("Schwarz");
            foreach (var id in idsBlack)
            {
                id.TextureFile = "#000000";
                id.BrdfModel = BrdfModel.PlasticDiffuse;
                id.MirrorColor = new Vector3D(1, 1, 1) * 0.5f;
                id.NormalInterpolation = InterpolationMode.Smooth;
                id.SpecularHighlightPowExponent = 20;
                id.SpecularHighlightCutoff1 = 1;
                id.SpecularHighlightCutoff2 = 2;
            }

            var idsWhite = graphic.GetObjectsByNameContainsSearch("Weiss");
            foreach (var id in idsWhite)
            {
                id.TextureFile = "#FFFFFF";
                id.BrdfModel = BrdfModel.PlasticDiffuse;
                id.MirrorColor = new Vector3D(1, 1, 1) * 0.1f;
                id.NormalInterpolation = InterpolationMode.Smooth;
                id.SpecularHighlightPowExponent = 20;
                id.SpecularHighlightCutoff1 = 1;
                id.SpecularHighlightCutoff2 = 2;
            }
            graphic.GetObjectByNameStartsWith("Weiss1").BrdfModel = BrdfModel.Diffus;

            var idsCloth = graphic.GetObjectsByNameContainsSearch("Stoff");
            foreach (var id in idsCloth)
            {
                id.TextureFile = DataDirectory + "stoff_hell.png";
            }

            var idsCarpet = graphic.GetObjectsByNameContainsSearch("Teppich");
            foreach (var id in idsCarpet)
            {
                id.Color = new ColorFromTexture() { TextureFile = DataDirectory + "stoff_hell1.png", TextureFilter = TextureFilter.Linear };
                id.NormalSource = new NormalFromMap() { NormalMap = DataDirectory + "stoff_hell1.png", ConvertNormalMapFromColor = true };
                id.NormalInterpolation = InterpolationMode.Flat;
                id.BrdfModel = BrdfModel.Diffus;
            }
            graphic.GetObjectByNameStartsWith("Teppich1").Color.As<ColorFromTexture>().TextureMatrix = Matrix3x3.Scale(1, 0.4f);

            var idsMetal = graphic.GetObjectsByNameContainsSearch("Metall");
            foreach (var id in idsMetal)
            {
                id.TextureFile = "#AAAAAA";
                id.MirrorColor = new Vector3D(1, 1, 1) * 0.5f;
                id.BrdfModel = BrdfModel.Tile;
                id.NormalInterpolation = InterpolationMode.Smooth;
            }

            var idsWalls = graphic.GetObjectsByNameContainsSearch("Waende");
            foreach (var id in idsWalls)
            {
                id.TextureFile = "#AAAAAA";
            }

            var idsWindowBorder = graphic.GetObjectsByNameContainsSearch("FensterRand");
            foreach (var id in idsWindowBorder)
            {
                id.TextureFile = "#AAAAAA";
            }

            graphic.GetObjectByNameStartsWith("HolzDunkel1").Color = new ColorFromTexture() { TextureFile = DataDirectory + "holz_dunkel.jpg", TextureMatrix = Matrix3x3.Scale(0.8f, 0.8f) * Matrix3x3.Translate(0.4f, 0.6f) };
            graphic.GetObjectByNameStartsWith("HolzDunkel2").TextureFile = DataDirectory + "92.jpg";
            graphic.GetObjectByNameStartsWith("Gruen1").TextureFile = DataDirectory + "383.jpg";
            graphic.GetObjectByNameStartsWith("Erde").TextureFile = DataDirectory + "265.jpg";
            graphic.GetObjectByNameStartsWith("Bild1").Color = new ColorFromTexture() { TextureFile = DataDirectory + "schriftzeichen-energie.png", TextureMode = TextureMode.Clamp };
            graphic.GetObjectByNameStartsWith("Bild1").Albedo = letterAlbedo;
            graphic.GetObjectByNameStartsWith("Figur1").TextureFile = "#444444";
            graphic.GetObjectByNameStartsWith("Boden1").TextureFile = DataDirectory + "wood-floorboards-texture-klein.jpg";
            graphic.GetObjectByNameStartsWith("Boden2").TextureFile = DataDirectory + "wood-floorboards-texture-klein.jpg";

            graphic.GetObjectByNameStartsWith("Glas1").TextureFile = "#FBFFFB";
            graphic.GetObjectByNameStartsWith("Glas1").BrdfModel = BrdfModel.TextureGlass;
            graphic.GetObjectByNameStartsWith("Glas1").RefractionIndex = 1.5f;
            graphic.GetObjectByNameStartsWith("Glas1").NormalInterpolation = InterpolationMode.Flat;
            graphic.GetObjectByNameStartsWith("Glas1").SpecularAlbedo = 0.8f; //Durch lange (30) Pfade bekomme ich hellen Flecken an den Wänden, wenn ich ein SpecularAlbedo von 1 habe

            Vector3D spotDirection = null;
            if (graphic.GetObjectByNameStartsWith("SpotDirection") != null)
            {
                spotDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("SpotDirection").Position - graphic.GetObjectByNameStartsWith("Licht1").Position);
                graphic.RemoveObjectStartsWith("SpotDirection");
            }


            graphic.GetObjectByNameStartsWith("Licht1").TextureFile = "#FFCCAA";
            graphic.GetObjectByNameStartsWith("Licht1").RaytracingLightSource = new SurfaceWithSpotLightDescription() // ImportanceSurfaceWithSpotLightDescription
            {
                Emission = luminance,
                SpotCutoff = 0.1f,
                SpotMix = 0.1f,
                SpotDirection = spotDirection,
                //CellSurfaceCount = 80 * 2 //Das Importancelicht erzeugt komische Muster in den Lichtflecken unterm Tisch wenn CellSurfaceCount zu klein ist. Wenn es größer ist, dann ist der Fehler zwar weniger aber es rendert viel langsamer
            };
            //graphic.GetObjectByNameStartsWith("Licht1").CreateQuads = true; //Wenn ich ImportanceSurfaceWithSpotLightDescription verwende

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 80);

            graphic.GlobalSettings.BackgroundImage = "#FFCCAA";
            graphic.GlobalSettings.BackgroundColorFactor = 1000000;

            graphic.GlobalSettings.RecursionDepth = 30; //Das Glas vom Tisch bekommt schwarze Ränder, wenn die Rekursionstiefe zu klein ist

            graphic.GlobalSettings.Tonemapping = TonemappingMethod.ACESFilmicToneMappingCurve;
            graphic.GlobalSettings.SamplingCount = 50000; //100k machen Qualitativ kein Unterschied zu 50k
            graphic.GlobalSettings.PhotonCount = 60000;
            graphic.Mode = Mode3D.VertexConnectionMerging;
        }

        //Chessboard
        public static void AddTestszene7_Chessboard(GraphicPanel3D graphic)
        {
            //Hinweis: Die dunklen Flecken auf den Figuren entstehen wenn das Licht inerhalb des Objektes total reflektiert wird.
            //Man kann durch auskommentieren der "newDirection.Brdf *= (relativeIOR * relativeIOR);"-Zeile aus dem SubPathSampler
            //das ganze heller machen so dass es wie das Bild vom letzten Rendenr aussieht.
            //Eigentlich wird ja beim reingehen in das Objekt das Pfadgewicht durch IOR² erst kleiner und beim Rausgehen wieder größer.
            //Hier in den Fall wird intern am Glas reflektiert und dadurch verläßt der Subpath das Objekt wegen Smoothshading beim Brdf-Sampling
            float sizeFactor = 1; //Der Tiefenunschärfeeffekt klappt nicht, wenn ich den SizeFaktor auf != 1 stelle
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "07_Chessboard_Low.obj", false, new ObjectPropertys() { Size = 0.01f * sizeFactor, Albedo = 0.8f });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart1").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraRichtung1").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraRichtung1").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart1").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraRichtung2").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart2").Id);

            var idsWoodLight = graphic.GetObjectsByNameContainsSearch("Weiss");
            foreach (var id in idsWoodLight)
            {
                id.TextureFile = DataDirectory + "schachbrett3.jpg";
            }

            graphic.GetObjectByNameStartsWith("Fenster").TextureFile = "#000000";

            graphic.GetObjectByNameStartsWith("Seite1").TextureFile = DataDirectory + "schachbrett1.jpg";
            graphic.GetObjectByNameStartsWith("Seite2").TextureFile = DataDirectory + "schachbrett0.jpg";

            var idsCloth = graphic.GetObjectsByNameContainsSearch("Oben");
            foreach (var id in idsCloth)
            {
                id.TextureFile = DataDirectory + "schachbrett2.jpg";
            }

            var idsBlue = graphic.GetObjectsByNameContainsSearch("Blau");
            foreach (var id in idsBlue)
            {
                id.TextureFile = "#AAAAFF";
                id.BrdfModel = BrdfModel.TextureGlass;
                id.NormalInterpolation = InterpolationMode.Smooth;
                id.RefractionIndex = 1.5f;
            }

            var idsYellow = graphic.GetObjectsByNameContainsSearch("Gelb");
            foreach (var id in idsYellow)
            {
                id.TextureFile = "#FFDD44";
                id.BrdfModel = BrdfModel.TextureGlass;
                id.NormalInterpolation = InterpolationMode.Smooth;
                id.RefractionIndex = 1.5f;
            }

            graphic.GlobalSettings.DistanceDephtOfFieldPlane = (new Vector3D(0.0731124878f, 0.0263275318f, 0.0547367409f) * sizeFactor - cameraStart).Length(); //Ich habe den ersten Schnittpunkt mit den Pixel hiermit genommen und dessen Position ist der Focalpoint: this.graphicPanel3D1.GetColorFromSinglePixel(420, 328, null, 203, 54, 1)
            graphic.GlobalSettings.WidthDephtOfField = graphic.GlobalSettings.DistanceDephtOfFieldPlane * 10; //Hier muss keine SizeFactor hin, da sich alles bis auf die 10 wegkürzt. Siehe: float startVerschiebungslänge = Math.Abs(this.distanceDephtOfFieldPlane) * DoFFaktor / Math.Abs(this.breiteDephtOfField);
            graphic.GlobalSettings.DepthOfFieldIsEnabled = true;

            graphic.GetObjectByNameStartsWith("Licht").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Licht").RaytracingLightSource = new ImportanceSurfaceLightDescription() { Emission = 2.6f * (sizeFactor * sizeFactor), ImportanceSamplingMode = LightImportanceSamplingMode.IsVisibleFromCamera };

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 35);

            graphic.GlobalSettings.CameraSamplingMode = PixelSamplingMode.Equal; //Tent-Sampling geht bei Tiefenunschärfe nicht
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.None;
            graphic.GlobalSettings.SamplingCount = 1000;
            graphic.Mode = Mode3D.FullBidirectionalPathTracing; //Lighttracing ist erlaubt so lange ich Equal-Kamera-Sampling nutze
        }

        //Raum mit eckigen Säulen
        public static void AddTestszene8_WindowRoom(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "08_WindowRoom.obj", false, new ObjectPropertys());

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("Start").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("Ende").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Ende").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Start").Id);

            graphic.GetObjectByNameStartsWith("Raum").TextureFile = "#888888";
            graphic.GetObjectByNameStartsWith("Raum").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("Fusboden").Color = new ColorFromTexture() { TextureFile = DataDirectory + "wood-floorboards-texture-klein.jpg", TextureMatrix = Matrix3x3.Scale(2, 2) };
            graphic.GetObjectByNameStartsWith("Fusboden").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("Fenster1").TextureFile = "#FFFFFF";
            graphic.FlipNormals(graphic.GetObjectByNameStartsWith("Fenster1").Id);
            graphic.GetObjectsByNameContainsSearch("Fenster1").First().RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 230 * 5 };

            graphic.GetObjectByNameStartsWith("Fenster2").TextureFile = "#FFFFFF";
            graphic.FlipNormals(graphic.GetObjectByNameStartsWith("Fenster2").Id);
            graphic.GetObjectsByNameContainsSearch("Fenster2").First().RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 230 * 5 };

            graphic.GetObjectByNameStartsWith("Fenster3").TextureFile = "#FFFFFF";
            graphic.FlipNormals(graphic.GetObjectByNameStartsWith("Fenster3").Id);
            graphic.GetObjectsByNameContainsSearch("Fenster3").First().RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 230 * 5 };

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 90);
            graphic.Mode = Mode3D.RadiositySolidAngle;
            graphic.GlobalSettings.RadiositySettings.MaxAreaPerPatch = 0.005f;
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.JimHejlAndRichardBurgessDawson;
        }

        //Multiple Importance Sampling
        public static void AddTestszene9_MultipleImportanceSampling(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "09_MultipleImportanceSampling.obj", false, new ObjectPropertys());

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("Start").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("Ende").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Ende").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Start").Id);

            do
            {
                var plane = graphic.GetObjectByNameStartsWith("Plane");
                if (plane != null) graphic.RemoveObjekt(plane.Id); else break;
            } while (true);

            graphic.GetObjectByNameStartsWith("Platte1").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Platte1").BrdfModel = BrdfModel.Phong;
            graphic.GetObjectByNameStartsWith("Platte1").GlossyPowExponent = 500;

            graphic.GetObjectByNameStartsWith("Platte2").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Platte2").BrdfModel = BrdfModel.Phong;
            graphic.GetObjectByNameStartsWith("Platte2").GlossyPowExponent = 300f;

            graphic.GetObjectByNameStartsWith("Platte3").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Platte3").BrdfModel = BrdfModel.Phong;
            graphic.GetObjectByNameStartsWith("Platte3").GlossyPowExponent = 200f;

            graphic.GetObjectByNameStartsWith("Platte4").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Platte4").BrdfModel = BrdfModel.Phong;
            graphic.GetObjectByNameStartsWith("Platte4").GlossyPowExponent = 100f;

            float emission = 10;
            graphic.GetObjectByNameStartsWith("Licht1").TextureFile = "#FF0000";
            graphic.GetObjectByNameStartsWith("Licht1").RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = emission };

            graphic.GetObjectByNameStartsWith("Licht2").TextureFile = "#FFFF00";
            graphic.GetObjectByNameStartsWith("Licht2").RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = emission };

            graphic.GetObjectByNameStartsWith("Licht3").TextureFile = "#00FF00";
            graphic.GetObjectByNameStartsWith("Licht3").RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = emission };

            graphic.GetObjectByNameStartsWith("Licht4").TextureFile = "#0000FF";
            graphic.GetObjectByNameStartsWith("Licht4").RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = emission };

            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 90);
        }

        //MirrorGlassCaustic
        public static void AddTestszene10_MirrorGlassCaustic(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "10_MirrorGlassCaustic.obj", false, new ObjectPropertys());


            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnde").Position - cameraStart);
            Vector3D lichtRichtung = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("LichtEnde").Position - graphic.GetObjectByNameStartsWith("LichtStart").Position);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnde").Id);

            graphic.GetObjectByNameStartsWith("Tisch").Color = new ColorFromTexture() { TextureFile = DataDirectory + "Envwall.bmp", TextureMatrix = Matrix3x3.Scale(0.7f, 0.7f) };
            graphic.GetObjectByNameStartsWith("Tisch").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("Spiegel").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Spiegel").BrdfModel = BrdfModel.Mirror;

            float emission = 500 * 10;

            graphic.GetObjectByNameStartsWith("LichtStart").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("LichtStart").RaytracingLightSource = new SphereWithSpotLightDescription() { Emission = 0.01f * emission, SpotCutoff = 15, SpotDirection = lichtRichtung };

            graphic.GetObjectByNameStartsWith("BackLight").TextureFile = "#FFFFFF";
            graphic.FlipNormals(graphic.GetObjectByNameStartsWith("BackLight").Id);
            graphic.GetObjectsByNameContainsSearch("BackLight").First().RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = emission };

            graphic.GetObjectByNameStartsWith("Vase").Color = new ColorFromTexture() { TextureFile = DataDirectory + "Mario.png", TextureMatrix = Matrix3x3.Scale(0.2f, 0.2f) };
            graphic.GetObjectByNameStartsWith("Vase").BrdfModel = BrdfModel.TextureGlass;
            graphic.GetObjectByNameStartsWith("Vase").ShowFromTwoSides = true;
            graphic.GetObjectByNameStartsWith("Vase").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("Vase").RefractionIndex = 1.5f;
            graphic.GetObjectByNameStartsWith("Vase").MediaDescription = new DescriptionForVacuumMedia() { Priority = 3 };

            graphic.GetObjectByNameStartsWith("Rueckwand").TextureFile = DataDirectory + "120klein.jpg";
            graphic.GetObjectByNameStartsWith("Rueckwand").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("Wand2").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Wand2").BrdfModel = BrdfModel.Diffus;

            graphic.GlobalSettings.GlobalParticipatingMedia = new DescriptionForHomogeneousMedia()
            {
                AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 0.01f,
                ScatteringCoeffizent = new Vector3D(1, 1, 1) * 0.01f,
                AnisotropyCoeffizient = 0.5f
            };

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 50);
            graphic.Mode = Mode3D.UPBP;
            graphic.GlobalSettings.PhotonCount = 10000;
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.None;
            graphic.GlobalSettings.SamplingCount = 1000;
        }

        public static void AddTestszene11_PillarsOfficeGodRay(GraphicPanel3D graphic)
        {
            AddTestszene11_PillarsOffice(graphic, true, true);
            graphic.Mode = Mode3D.MediaBeamTracer;     //Wenn ich Godrays will (Mit Media)

            //So habe ich die Maske erzeugt, welche Säule10 markiert. Einfach mit dem SimpleRaytracer rendern (Säule10 wird schwarz, Rest weiß)
            //graphic.GetAllObjects().Where(x => x.Name.StartsWith("Saule10") == false).Select(x => x.Id).ToList().ForEach(x => graphic.RemoveObjekt(x));
        }

        public static void AddTestszene11_PillarsOfficeMedia(GraphicPanel3D graphic)
        {
            AddTestszene11_PillarsOffice(graphic, true, true);           
            graphic.Mode = Mode3D.MediaEdgeSampler;    //Wenn ich keine Godrays will (Mit Media)
        }

        public static void AddTestszene11_PillarsOfficeNoMedia(GraphicPanel3D graphic)
        {
            AddTestszene11_PillarsOffice(graphic, true, false);
            graphic.Mode = Mode3D.FullBidirectionalPathTracing; //Wenn ich kein Media will
        }

        public static void AddTestszene11_PillarsOfficeNight(GraphicPanel3D graphic)
        {
            AddTestszene11_PillarsOffice(graphic, true, false);
            graphic.Mode = Mode3D.FullBidirectionalPathTracing; //Wenn ich kein Media will
        }

        //Säulen-Büro
        private static void AddTestszene11_PillarsOffice(GraphicPanel3D graphic, bool atDay, bool withMedia)
        {
            float sizeFactor = 10.0f; //Der Sizefaktor darf von 0.1 bis 100 überall liegen (Der Beam2Beam-Searchradius wird unten angepasst)

            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "11_PillarsOffice.obj", false, new ObjectPropertys() { Size = 1 * sizeFactor, Albedo = 1 });

            foreach (var id in graphic.GetAllObjects())
            {
                id.TextureFile = "#888888";
            }

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnde").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnde").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            Vector3D lightDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("LEnd").Position - graphic.GetObjectByNameStartsWith("LichtKugel").Position);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("LEnd").Id);

            var idsWindowHandle = graphic.GetObjectsByNameContainsSearch("Griff");
            foreach (var id in idsWindowHandle)
            {
                id.TextureFile = "#888888";
            }

            graphic.GetObjectByNameStartsWith("Fußboden").Color = new ColorFromTexture() { TextureFile = DataDirectory + "wood-floorboards-texture.jpg", TextureMatrix = Matrix3x3.Scale(2, 2) };
            graphic.GetObjectByNameStartsWith("Fußboden").BrdfModel = BrdfModel.MicrofacetTile;
            graphic.GetObjectByNameStartsWith("Fußboden").NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.03f, 0.03f) };
            graphic.GetObjectByNameStartsWith("Fußboden").RefractionIndex = 1.5f;
            graphic.GetObjectByNameStartsWith("Fußboden").TileDiffuseFactor = 0.5f;

            graphic.GetObjectByNameStartsWith("Decke").Color = new ColorFromTexture() { TextureFile = DataDirectory + "16102906-Schwarz-wei-e-Marmor-Stein-Mosaik-Textur-mit-hoher-Aufl-sung-Lizenzfreie-Bilder.jpg", TextureMatrix = Matrix3x3.Scale(10, 10) };
            graphic.GetObjectByNameStartsWith("Decke").BrdfModel = BrdfModel.Diffus;
            graphic.GetObjectByNameStartsWith("Decke").Albedo = 0.8f;

            var idsPilars = graphic.GetObjectsByNameContainsSearch("Saule");
            foreach (var id in idsPilars)
            {
                id.Color = new ColorFromTexture() { TextureFile = DataDirectory + "thumb_COLOURBOX5847554.jpg", TextureMatrix = Matrix3x3.Scale(2, 1) };
                id.Orientation.Y += 90;               
                id.BrdfModel = BrdfModel.Diffus;
                id.NormalInterpolation = InterpolationMode.Smooth;
                id.Albedo = 0.8f;
            }

            //Säule ganz vorne
            var texMatrix = Matrix3x3.Scale(1.5f, 2.5f) * Matrix3x3.Translate(0, -0.15f);
            graphic.GetObjectByNameStartsWith("Saule10").Color.As<ColorFromTexture>().TextureMatrix = texMatrix;
            graphic.GetObjectByNameStartsWith("Saule10").Orientation = new Vector3D(0, 297, 0);
            graphic.GetObjectByNameStartsWith("Saule10").NormalSource = new NormalFromParallax() { ParallaxMap = DataDirectory + "thumb_COLOURBOX5847554_bumpmap4.bmp", TexturHeightFactor = 0.014f, IsParallaxEdgeCutoffEnabled = true, TextureMatrix = texMatrix };
            graphic.GetObjectByNameStartsWith("Saule10").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("Saule10").Color.As<ColorFromTexture>().TextureFilter = TextureFilter.Linear;
            graphic.GetObjectByNameStartsWith("Saule10").BrdfModel = BrdfModel.PlasticDiffuse;
            graphic.GetObjectByNameStartsWith("Saule10").SpecularHighlightPowExponent = 20;
            graphic.GetObjectByNameStartsWith("Saule10").SpecularHighlightCutoff1 = 1;
            graphic.GetObjectByNameStartsWith("Saule10").SpecularHighlightCutoff2 = 2;
            graphic.GetObjectByNameStartsWith("Saule10").ShowFromTwoSides = false;

            var idsChair = graphic.GetObjectsByNameContainsSearch("Stuhl");
            foreach (var id in idsChair)
            {
                id.TextureFile = DataDirectory + "holz_mittel.jpg";
            }

            var idsFan = graphic.GetObjectsByNameContainsSearch("Ventilator");
            foreach (var id in idsFan)
            {
                id.TextureFile = "#884433";
                id.NormalInterpolation = InterpolationMode.Smooth;
            }

            int propellerInFront = graphic.MergeTwoObjects(graphic.MergeTwoObjects(graphic.GetObjectByNameStartsWith("Propeller1").Id, graphic.GetObjectByNameStartsWith("Propeller2").Id), graphic.GetObjectByNameStartsWith("Propeller3").Id);
            int propellerBehind = graphic.MergeTwoObjects(graphic.MergeTwoObjects(graphic.GetObjectByNameStartsWith("Propeller4").Id, graphic.GetObjectByNameStartsWith("Propeller5").Id), graphic.GetObjectByNameStartsWith("Propeller6").Id);

            graphic.GetObjectById(propellerInFront).TextureFile = "#AA6633";
            graphic.GetObjectById(propellerBehind).TextureFile = "#AA6633";
            graphic.GetObjectById(propellerInFront).NormalInterpolation = InterpolationMode.Flat;
            graphic.GetObjectById(propellerBehind).NormalInterpolation = InterpolationMode.Flat;

            if (atDay) graphic.GetObjectById(propellerInFront).MotionBlurMovment = new RotationMovementEulerDescription() { RotationStart = 0, RotationEnd = -20, Factor = 2, Axis = 1 };

            var idsStalk = graphic.GetObjectsByNameContainsSearch("Stiel");
            foreach (var id in idsStalk)
            {
                id.TextureFile = "#000000";
            }

            var idsLamps = graphic.GetObjectsByNameContainsSearch("Lampe");
            foreach (var id in idsLamps)
            {
                id.TextureFile = "#888888";
            }

            graphic.GetObjectByNameStartsWith("SitzFlaeche").TextureFile = DataDirectory + "leder05_th.jpg";

            graphic.GetObjectByNameStartsWith("Metallzeug").TextureFile = "#AAAAAA";
            graphic.GetObjectByNameStartsWith("Metallzeug").BrdfModel = BrdfModel.Tile;

            graphic.GetObjectByNameStartsWith("Raeder").TextureFile = "#111111";
            graphic.GetObjectByNameStartsWith("Raeder").BrdfModel = BrdfModel.PlasticDiffuse;

            graphic.GetObjectByNameStartsWith("Hund").Color = new ColorFromTexture() { TextureFile = DataDirectory + "1692912-illustration-of-the-brpwn-tile-background.jpg", TextureMatrix = Matrix3x3.Scale(5, 5) };
            graphic.GetObjectByNameStartsWith("Hund").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("Hund").Albedo = 0.8f;

            graphic.GetObjectByNameStartsWith("Luft").MediaDescription = new DescriptionForHomogeneousMedia()
            {
                ScatteringCoeffizent = new Vector3D(1, 1, 1) * 0.005f / sizeFactor,
                AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 0.25f / sizeFactor,
                AnisotropyCoeffizient = 0.00f
            };
            graphic.GetObjectByNameStartsWith("Luft").RefractionIndex = 1;

            if (withMedia == false) graphic.RemoveObjectStartsWith("Luft");

            if (atDay)
            {
                graphic.RemoveObjectStartsWith("LichtKugel");
                graphic.GetObjectByNameStartsWith("LichtPlane").TextureFile = "#FFCCAA";

                var lightWindow = graphic.GetObjectsByNameContainsSearch("FensterLicht");
                foreach (var id in lightWindow)
                {
                    id.TextureFile = "#FFCCAA";
                    id.RaytracingLightSource = new SurfaceWithSpotLightDescription()
                    {
                        //750 wenn ich ein Scatteringmedium verwende und Fliesen-Boden mit Albedo von 0.5 oder FresnelFliese (Mit MediaBeamTracer)
                        //50 wenn ich ohne Media arbeite
                        Emission = (withMedia ? 750 : 50) * (sizeFactor * sizeFactor),
                        SpotDirection = lightDirection,
                        SpotCutoff = 2.5f,
                        SpotMix = 0.9f,//0.70f
                        UseWithoutDiffuseDictionSamplerForLightPathCreation = true
                    };
                }

                graphic.GetObjectByNameStartsWith("LichtKlein").RaytracingLightSource = new SurfaceWithSpotLightDescription()
                {
                    Emission = 0.5f * (sizeFactor * sizeFactor), //Wenn ich über die LichtPlane beleuchte, ist 0.5 ok; 
                    SpotCutoff = 1,
                    SpotMix = 1
                };
                graphic.GetObjectByNameStartsWith("LichtKlein").RasterizerLightSource = new RasterizerLightSourceDescription() { SpotCutoff = 90, SpotDirection = cameraDirection, ConstantAttenuation = 0.5f };
                graphic.GetObjectByNameStartsWith("LichtKlein").TextureFile = "#FFCCAA";

                graphic.GlobalSettings.BackgroundImage = "#CCCCFF";
                graphic.GlobalSettings.BackgroundColorFactor = 1000000;
            }
            else
            {
                var idsCeilingLight = graphic.GetObjectsByNameContainsSearch("Innenlicht");
                foreach (var id in idsCeilingLight)
                {
                    id.TextureFile = "#FFFFFF";
                    id.RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 20 * (sizeFactor * sizeFactor) };
                }

                var windowLight = graphic.GetObjectsByNameContainsSearch("FensterLicht");
                foreach (var id in windowLight)
                {
                    id.TextureFile = "#000000";
                }

                graphic.GlobalSettings.BackgroundImage = "#000000";
            }



            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 60);

            graphic.GlobalSettings.LightPickStepSize = 4; //Damit die kleine Lampe an der Säule vorne 25% Gewicht bekommt
            graphic.GlobalSettings.SearchRadiusForMediaBeamTracer = 0.05f * sizeFactor;
            graphic.GlobalSettings.PhotonCount = 200;
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.None;
            graphic.GlobalSettings.SamplingCount = 10000;
        }

        //Snowman
        public static void AddTestszene12_Snowman(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "12_Snowman.obj", false, new ObjectPropertys() { Albedo = 0.8f });

            foreach (var id in graphic.GetAllObjects())
            {
                id.TextureFile = "#888888";
            }

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            graphic.GetObjectByNameStartsWith("Haus").TextureFile = "#888888";
            graphic.GetObjectByNameStartsWith("Tisch").TextureFile = "#BB8844";

            graphic.GetObjectByNameStartsWith("Pferd").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Pferd").BrdfModel = BrdfModel.Phong;
            graphic.GetObjectByNameStartsWith("Pferd").GlossyPowExponent = 400;
            graphic.GetObjectByNameStartsWith("Pferd").GlossyColor = new Vector3D(1, 1, 1);

            graphic.GlobalSettings.DistanceDephtOfFieldPlane = (graphic.GetObjectByNameStartsWith("SchneePlatte").Position - cameraStart).Length();
            graphic.GlobalSettings.WidthDephtOfField = graphic.GlobalSettings.DistanceDephtOfFieldPlane / 4;


            var idsGlas = graphic.GetObjectsByNameContainsSearch("Glas");
            foreach (var id in idsGlas)
            {
                id.TextureFile = "#FFFFFF";
                id.BrdfModel = BrdfModel.TextureGlass;
                id.NormalInterpolation = InterpolationMode.Flat;
                id.RefractionIndex = 1.5f;
            }

            graphic.GetObjectByNameStartsWith("SchneePlatte").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("SchneePlatte").BrdfModel = BrdfModel.Mirror;

            var idsBlack = graphic.GetObjectsByNameContainsSearch("Schwarz");
            foreach (var id in idsBlack)
            {
                id.TextureFile = "#111111";
                id.BrdfModel = BrdfModel.TextureGlass;
                id.NormalInterpolation = InterpolationMode.Flat;
                id.RefractionIndex = 1.5f;
            }

            var idsRed = graphic.GetObjectsByNameContainsSearch("Rot");
            foreach (var id in idsRed)
            {
                id.TextureFile = "#FF0000";
                id.BrdfModel = BrdfModel.TextureGlass;
                id.NormalInterpolation = InterpolationMode.Flat;
                id.RefractionIndex = 1.5f;
            }

            var idsRing = graphic.GetObjectsByNameContainsSearch("Ring");
            foreach (var id in idsRing)
            {
                id.TextureFile = "#FF9900";
                id.BrdfModel = BrdfModel.PlasticDiffuse;
                id.NormalInterpolation = InterpolationMode.Smooth;
                id.SpecularHighlightCutoff1 = 1.2f;
                id.SpecularHighlightCutoff2 = 1;
            }

            graphic.GetObjectByNameStartsWith("Vase").TextureFile = "#FF00CC";
            graphic.GetObjectByNameStartsWith("Vase").BrdfModel = BrdfModel.PlasticDiffuse;
            graphic.TransformToWireObject(graphic.GetObjectByNameStartsWith("Vase").Id, 0.1f);

            var idsSphere = graphic.GetObjectsByNameContainsSearch("Kugel");
            foreach (var id in idsSphere)
            {
                id.TextureFile = "#00FFFF";
                id.MirrorColor = new Vector3D(0, 1, 1);
                id.SpecularAlbedo = 0.8f;
                id.BrdfModel = BrdfModel.Tile;
            }

            graphic.GetObjectByNameStartsWith("Regal").TextureFile = "#775533";
            graphic.GetObjectByNameStartsWith("Regal").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("Buchruecken").TextureFile = DataDirectory + "wellensittiche.JPG";
            graphic.GetObjectByNameStartsWith("Buchruecken").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("BuchSeiten").TextureFile = "#CCCCCC";
            graphic.GetObjectByNameStartsWith("BuchSeiten").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("Topf").TextureFile = "#441100";
            graphic.GetObjectByNameStartsWith("Topf").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("Stiel").TextureFile = "#663300";
            graphic.GetObjectByNameStartsWith("Stiel").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("Blaetter").TextureFile = "#003300";
            graphic.GetObjectByNameStartsWith("Blaetter").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("Erde").TextureFile = "#111111";
            graphic.GetObjectByNameStartsWith("Erde").BrdfModel = BrdfModel.Diffus;

            graphic.GetObjectByNameStartsWith("XMAMan").TextureFile = "#ABFFAB";
            graphic.GetObjectByNameStartsWith("XMAMan").BrdfModel = BrdfModel.HeizGlass;
            graphic.GetObjectByNameStartsWith("XMAMan").NormalSource = new NormalFromMicrofacet();
            graphic.GetObjectByNameStartsWith("XMAMan").RefractionIndex = 1.5f;

            graphic.GlobalSettings.RecursionDepth = 20;

            bool atDay = true;

            float emission = 28800;
 
            if (atDay)
            {
                bool indirect = false;
                if (indirect)
                {
                    graphic.GetObjectByNameStartsWith("Licht1").TextureFile = "#FFFFFF";
                    graphic.GetObjectByNameStartsWith("Licht1").RaytracingLightSource = new SurfaceWithSpotLightDescription() { Emission = emission };

                    graphic.GetObjectByNameStartsWith("Licht3").TextureFile = "#FFFFFF"; //Vorne am Fenster

                    graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Licht2").Id); //Decke oben
                    graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Licht4").Id); //Rechts am Fenster
                }
                else
                {
                    graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Licht1").Id); //Schräg außen
                    graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Licht2").Id); //Decke oben

                    graphic.GetObjectByNameStartsWith("Licht3").TextureFile = "#FFFFFF"; //Vorne am Fenster
                    graphic.GetObjectByNameStartsWith("Licht3").RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = emission * 1.4f };

                    graphic.GetObjectByNameStartsWith("Licht4").TextureFile = "#FFFFFF"; //Rechts am Fenster
                    graphic.GetObjectByNameStartsWith("Licht4").RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = emission * 0.9f };
                }
            }
            else
            {
                graphic.GetObjectByNameStartsWith("Licht2").TextureFile = "#FFFFFF"; //Decke oben
                graphic.GetObjectByNameStartsWith("Licht2").RaytracingLightSource = new DiffuseSurfaceLightDescription();


                graphic.GlobalSettings.BackgroundImage = "#000000";
            }

            graphic.GlobalSettings.DepthOfFieldIsEnabled = true;
            graphic.GlobalSettings.CameraSamplingMode = PixelSamplingMode.Equal; //Tent-Sampling geht bei Tiefenunschärfe nicht
            graphic.Mode = Mode3D.FullBidirectionalPathTracing; //Lighttracing ist erlaubt so lange ich Equal-Kamera-Sampling nutze

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 20);

            graphic.GlobalSettings.Tonemapping = TonemappingMethod.None;
            graphic.GlobalSettings.SamplingCount = 1000;
        }

        //Microfacet-Glas
        public static void AddTestszene13_MicrofacetGlas(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "13_GlasMicrofacet.obj", false, new ObjectPropertys() { Albedo = 0.8f });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnde").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnde").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            graphic.GetObjectByNameStartsWith("Wurfel").TextureFile = "#00FFFF";

            graphic.GetObjectByNameStartsWith("Tisch").Color = new ColorFromTexture() { TextureFile = DataDirectory + "Kreise.jpg", TextureMatrix = Matrix3x3.Scale(2, 2) };
            graphic.GetObjectByNameStartsWith("Glas").TextureFile = "#FBFFFB";
            graphic.GetObjectByNameStartsWith("Glas").BrdfModel = BrdfModel.WalterGlass;
            graphic.GetObjectByNameStartsWith("Glas").NormalSource = new NormalFromMicrofacet();
            graphic.GetObjectByNameStartsWith("Glas").RefractionIndex = 1.5f;

            graphic.GetObjectByNameStartsWith("Licht1").TextureFile = "#FFFFFF";
            graphic.FlipNormals(graphic.GetObjectByNameStartsWith("Licht1").Id);

            graphic.GetObjectByNameStartsWith("Licht2").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Licht2").RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 1000 };

            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";
            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 60);
        }

        //Microfacet-Sundial
        public static void AddTestszene14_MicrofacetSundial(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "14_Sundial.obj", false, new ObjectPropertys());

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnde").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnde").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            graphic.GetObjectByNameStartsWith("UhrFlat").BrdfModel = BrdfModel.WalterMetal;
            graphic.GetObjectByNameStartsWith("UhrFlat").NormalSource = new NormalFromMicrofacet(){ MicrofacetRoughness = new Vector2D(0.1f, 0.1f)};
            graphic.GetObjectByNameStartsWith("UhrRing1").BrdfModel = BrdfModel.WalterMetal;
            graphic.GetObjectByNameStartsWith("UhrRing1").NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.1f, 0.1f) };
            graphic.GetObjectByNameStartsWith("UhrRing1").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("UhrRing2").BrdfModel = BrdfModel.WalterMetal;
            graphic.GetObjectByNameStartsWith("UhrRing2").NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.1f, 0.1f) };
            graphic.GetObjectByNameStartsWith("UhrRing2").NormalInterpolation = InterpolationMode.Smooth;

            graphic.AddSphere(1, 10, 10, new ObjectPropertys()
            {
                TextureFile = DataDirectory + "chinese_garden_1k.hdr", //https://hdrihaven.com/hdri/?c=outdoor&h=chinese_garden
                RaytracingLightSource = new EnvironmentLightDescription() { Emission = 5, Rotate = 0 }
            });

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 50);
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.GammaOnly;
        }

        //Microfacet-KugelBox
        public static void AddTestszene15_MicrofacetSphereBox(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "15_MicrofacetSphereBox.obj", false, new ObjectPropertys() { Albedo = 0.8f });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnde").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnde").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            graphic.GetObjectByNameStartsWith("Links").TextureFile = "#B23333";
            graphic.GetObjectByNameStartsWith("Rechts").TextureFile = "#33B233";
            graphic.GetObjectByNameStartsWith("Boden").TextureFile = "#998E7C";
            graphic.GetObjectByNameStartsWith("Boden").Albedo = 0.6f;
            graphic.GetObjectByNameStartsWith("Decke").TextureFile = "#B2B2B2";
            graphic.GetObjectByNameStartsWith("Hinten").TextureFile = DataDirectory + "WalterStreifen.png";
            graphic.GetObjectByNameStartsWith("Vorne").TextureFile = "#AAAAAA";
            graphic.GetObjectByNameStartsWith("ObenLinks").TextureFile = "#AAAAAA";
            graphic.GetObjectByNameStartsWith("ObenRechts").TextureFile = "#AAAAAA";

            //Wichtiger Hinweis zum Thema Glaskugeln:
            //Eine Glaskugel, welche drinnen nicht hohl ist und mit Glas komplett gefüllt ist, Spiegelt das Bild sowohl vertikal als auch Horizonetal
            //Es wird also um die Sichtachse um 180 Grad gedreht. Man sieht alles auf dem Kopf.
            //Eine Kugel die innen hohl ist (Weihnachtskugel) dreht das Bild nicht um. Sie sorgt ledglich für leichte verzehrung.

            Vector3D spherePosition = graphic.GetObjectByNameStartsWith("Kugel").Position;
            float radius = graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("Kugel").Id).RadiusInTheBox;
            graphic.AddSphere(radius, 10, 10, new ObjectPropertys()
            {
                NormalInterpolation = InterpolationMode.Smooth,
                //GlasIsSingleLayer = true, //Bei Walter_2007 wird das Bild vertikal gespiegelt. Also verwendet er keine Hohlkugel
                Position = spherePosition,
                ShowFromTwoSides = true,
                Orientation = new Vector3D(0, 30, 3),

                BrdfModel = BrdfModel.HeizGlass,
                RefractionIndex = 1.7f,

                NormalSource = new NormalFromMicrofacet() { TextureMatrix = Matrix3x3.Scale(1.0f, 1.0f), RoughnessMap = DataDirectory + "Weltkarte1.png" }, //Walter scheint bei der Kugelrückwand kein Microfacet zu haben
                TextureFile = "#FFFFFF",
            });
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("Kugel").Id);

            graphic.GetObjectByNameStartsWith("Licht").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Licht").RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = 250 * 8 };
            graphic.GetObjectByNameStartsWith("Licht").RasterizerLightSource = new RasterizerLightSourceDescription();

            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";
            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 16);
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.None;
            graphic.GlobalSettings.SamplingCount = 1000;
            graphic.Mode = Mode3D.FullBidirectionalPathTracing;
        }

        //Graphic6Memories
        public static void AddTestszene16_Graphic6Memories(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "16_Graphic6Memories.obj", false, new ObjectPropertys() { Size = 0.1f, Albedo = 0.2f });
            float lightFactor = 4.74f;

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnde").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnde").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            graphic.GetObjectByNameStartsWith("Flasche").TextureFile = DataDirectory + "Image1.bmp";
            graphic.GetObjectByNameStartsWith("Flasche").BlackIsTransparent = true;
            graphic.GetObjectByNameStartsWith("Kugel").TextureFile = "#008822";

            graphic.GetObjectByNameStartsWith("Mario").TextureFile = DataDirectory + "Mario.png";

            var mario = graphic.GetObjectByNameStartsWith("Mario");
            var box = graphic.GetBoundingBoxFromObject(mario.Id);
            graphic.SetCenterOfObjectOrigin(mario.Id, new Vector3D(box.Min.X + box.XSize * 0.5f, box.Min.Y, box.Min.Z + box.ZSize * 0.5f));     
            mario.Albedo = 0.8f;

            graphic.GetObjectByNameStartsWith("Pflanze").TextureFile = DataDirectory + "Pflanze.png";
            graphic.GetObjectByNameStartsWith("Pflanze").BrdfModel = BrdfModel.HeizMetal;
            graphic.GetObjectByNameStartsWith("Pflanze").NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.1f, 0.1f) };

            graphic.GetObjectByNameStartsWith("Pilz").TextureFile = DataDirectory + "Pilz.png";
            graphic.GetObjectByNameStartsWith("Pilz").BrdfModel = BrdfModel.WalterGlass;
            graphic.GetObjectByNameStartsWith("Pilz").NormalSource = new NormalFromMicrofacet();
            graphic.GetObjectByNameStartsWith("Pilz").RefractionIndex = 1.5f;

            graphic.GetObjectByNameStartsWith("Schildkroete").TextureFile = DataDirectory + "Schildkroete.png";
            graphic.GetObjectByNameStartsWith("Schildkroete").Albedo = 0.8f;
            graphic.GetObjectByNameStartsWith("Spirale").Color = new ColorFromTexture() { TextureFile = DataDirectory + "DecalGreen.bmp", TextureMatrix = Matrix3x3.Scale(2, 2) };
            graphic.GetObjectByNameStartsWith("Spirale").BrdfModel = BrdfModel.Diffus;
            graphic.GetObjectByNameStartsWith("Spirale").NormalInterpolation = InterpolationMode.Smooth;

            graphic.GetObjectByNameStartsWith("Tetrisstein1").TextureFile = DataDirectory + "Mario.png";
            graphic.GetObjectByNameStartsWith("Tetrisstein2").TextureFile = DataDirectory + "Pflanze.png";
            graphic.GetObjectByNameStartsWith("Tetrisstein3").TextureFile = DataDirectory + "Taler.png";
            graphic.GetObjectByNameStartsWith("Tetrisstein4").TextureFile = DataDirectory + "Pilz.png";
            graphic.GetObjectByNameStartsWith("Tetrisstein5").TextureFile = DataDirectory + "Schildkroete.png";

            var stones = graphic.GetObjectsByNameContainsSearch("Tetrisstein");
            foreach (var id in stones)
            {
                id.NormalSource = new NormalFromMap() { NormalMap = id.Color.As<ColorFromTexture>().TextureFile, ConvertNormalMapFromColor = true };
                id.NormalInterpolation = InterpolationMode.Smooth;
            }

            graphic.GetObjectByNameStartsWith("Boden").TextureFile = DataDirectory + "Envwall.bmp";
            graphic.GetObjectByNameStartsWith("Boden").BrdfModel = BrdfModel.Diffus;
            graphic.GetObjectByNameStartsWith("Boden").Albedo = 0.15f;

            graphic.GetObjectByNameStartsWith("Wand1").Color = new ColorFromTexture() { TextureFile = DataDirectory + "Decal.bmp", TextureMatrix = Matrix3x3.Rotate(90) };
            graphic.GetObjectByNameStartsWith("Wand1").TileDiffuseFactor = 0.6f;
            graphic.GetObjectByNameStartsWith("Wand1").BrdfModel = BrdfModel.Diffus; //DiffusePlatic
            graphic.GetObjectByNameStartsWith("Wand1").SpecularAlbedo = 0.4f;
            graphic.GetObjectByNameStartsWith("Wand1").NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.03f, 0.03f) };
            graphic.GetObjectByNameStartsWith("Wand1").Albedo = 0.4f;
            graphic.GetObjectByNameStartsWith("Wand2").Albedo = 0.4f;
            graphic.GetObjectByNameStartsWith("Wand3").Albedo = 0.4f;
            graphic.GetObjectByNameStartsWith("Wand1").RefractionIndex = 1.5f;
            graphic.GetObjectByNameStartsWith("Wand2").RefractionIndex = 1.5f;
            graphic.GetObjectByNameStartsWith("Wand3").RefractionIndex = 1.5f;

            graphic.GetObjectByNameStartsWith("Wand2").Color = new ColorFromTexture() { TextureFile = DataDirectory + "Decal.bmp", TextureMatrix = Matrix3x3.Rotate(90) };
            graphic.GetObjectByNameStartsWith("Wand3").Color = new ColorFromTexture() { TextureFile = DataDirectory + "Decal.bmp", TextureMatrix = Matrix3x3.Rotate(90) };

            graphic.GetObjectByNameStartsWith("Wand2").TileDiffuseFactor = 0.6f;
            graphic.GetObjectByNameStartsWith("Wand2").SpecularAlbedo = 0.4f;
            graphic.GetObjectByNameStartsWith("Wand2").BrdfModel = BrdfModel.FresnelTile;
            graphic.GetObjectByNameStartsWith("Wand2").NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.03f, 0.03f) };


            graphic.GetObjectByNameStartsWith("Kooper").TextureFile = DataDirectory + "Kooper.png";
            graphic.GetObjectByNameStartsWith("Schuss").TextureFile = DataDirectory + "Schuss.png";
            Vector3D t1 = graphic.GetObjectByNameStartsWith("Schuss").Position - graphic.GetObjectByNameStartsWith("Kooper").Position;
            float angle = Vector3D.AngleDegree(new Vector3D(1, 0, 0), t1) + 180;
            graphic.GetObjectByNameStartsWith("Kooper").Orientation = new Vector3D(0, angle, 0);
            graphic.GetObjectByNameStartsWith("Schuss").Orientation = new Vector3D(0, angle, 0);
            Vector3D schussPosition = graphic.GetObjectByNameStartsWith("Schuss").Position;
            graphic.GetObjectByNameStartsWith("Schuss").MotionBlurMovment = new TranslationMovementEulerDescription() { PositionStart = schussPosition, PositionEnd = schussPosition - new Vector3D((float)Math.Cos(angle), 0, (float)Math.Sin(angle)) * 0.1f, Factor = 5 };
            graphic.GetObjectByNameStartsWith("Schuss").RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = lightFactor * 1 };


            graphic.GetObjectByNameStartsWith("Pictogram").TextureFile = DataDirectory + "Pictogram1.png";

            graphic.GetObjectByNameStartsWith("RaytracingText").TextureFile = "#6B8CFF"; //Mariohimmelblau
            graphic.GetObjectByNameStartsWith("RaytracingText").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("RaytracingText").BrdfModel = BrdfModel.Tile;
            graphic.GetObjectByNameStartsWith("RaytracingText").TileDiffuseFactor = 0.8f;

            graphic.GetObjectByNameStartsWith("Licht").TextureFile = "#FFFFFF";
            graphic.GetObjectByNameStartsWith("Licht").RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = lightFactor * 100 };

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 65);

            graphic.GlobalSettings.Tonemapping = TonemappingMethod.None;
            graphic.GlobalSettings.SamplingCount = 1000;
            graphic.Mode = Mode3D.FullBidirectionalPathTracing;
        }

        //TheFifthElement
        public static void AddTestszene17_TheFifthElement(GraphicPanel3D graphic)
        {
            float sizeFactor = 10;
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "17_TheFifthElement.obj", false, new ObjectPropertys() { Size = 0.1f * sizeFactor });
            float lightFactor = 35f;

            graphic.GlobalSettings.BackgroundImage = "#4444DD";

            var objList = graphic.GetAllObjects().Select(x => x.Name + " " + x.Color.ToString()).ToList();

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart1").Position; //Kamera schaut aus Balkon auf die Straße
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd1").Position - cameraStart);
            //Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart2").Position; //Kamera schaut von Straße in Auto/Balkon
            //Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd2").Position - cameraStart);

            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd1").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart1").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd2").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart2").Id);

            graphic.GetObjectByNameStartsWith("AmpelVorne").Albedo = 0.2f;
            graphic.GetObjectByNameStartsWith("AmpelStange").Albedo = 0.3f;

            graphic.GetObjectByNameStartsWith("LampenStander").BrdfModel = BrdfModel.Phong;
            graphic.GetObjectByNameStartsWith("LampenStander").GlossyPowExponent = 400;
            graphic.GetObjectByNameStartsWith("LampenStange").BrdfModel = BrdfModel.Phong;
            graphic.GetObjectByNameStartsWith("LampenStange").GlossyPowExponent = 400;

            graphic.GetObjectByNameStartsWith("Fahrer").TextureFile = DataDirectory + "FahrerTex.png";
            graphic.GetObjectByNameStartsWith("Sitz").TextureFile = "#3F1112";

            var windowPanes = graphic.GetObjectsByNameContainsSearch("Scheibe");
            foreach (var id in windowPanes)
            {
                id.BrdfModel = BrdfModel.TextureGlass;
                id.SpecularAlbedo = 1;
                id.RefractionIndex = 1.5f;
            }

            graphic.GetObjectByNameStartsWith("Licht1").RaytracingLightSource = new ImportanceSurfaceWithSpotLightDescription() { Emission = lightFactor * 5 * (sizeFactor * sizeFactor), SpotCutoff = 10, ImportanceSamplingMode = LightImportanceSamplingMode.IsVisibleFromCamera };
            graphic.GetObjectByNameStartsWith("Licht1").TextureFile = "#FFFFFF";

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 85); //Kamera1: 85; Kamera2: 45
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.Ward;
            graphic.Mode = Mode3D.FullBidirectionalPathTracing;
        }

        public static void TestSzene18_CloudsForTestImage(GraphicPanel3D graphic)
        {
            AddTestszene18_SkyWithClouds(graphic, 0, 2, 1200, "#004400", 0, Scenes.CloudShape.Sphere); //Mittags
            //AddTestszene18_SkyWithClouds(graphic, 85, 7, 800, "#00AA00", 1, Scenes.CloudShape.Sphere);//Abends
        }

        //Erde + Atmospähre + Richtungslicht für die Sonne + Wolken
        //Hinweis: Die MinAllowedPathPointDistance muss hier 10 sein, da die Float-Zahlen mit der großen Erdekugel sonst nicht klar kommen
        public enum CloudShape { Cube, Sphere };
        public static void AddTestszene18_SkyWithClouds(GraphicPanel3D graphic, float sunDegree, int positionSeed, int cloudHeigh, string groundColor, int cameraPosition, CloudShape shape)
        {
            graphic.RemoveAllObjekts();

            float sizeFactor = 1;// 0.001f;

            var defaultSky = new DescriptionForSkyMedia();

            DescriptionForSkyMedia skyMedia = new DescriptionForSkyMedia()
            {
                EarthRadius = defaultSky.EarthRadius * sizeFactor,
                AtmosphereRadius = defaultSky.AtmosphereRadius * sizeFactor,
                RayleighScaleHeight = defaultSky.RayleighScaleHeight * sizeFactor,
                MieScaleHeight = defaultSky.MieScaleHeight * sizeFactor,
                RayleighScatteringCoeffizientOnSeaLevel = defaultSky.RayleighScatteringCoeffizientOnSeaLevel / sizeFactor,
                MieScatteringCoeffizientOnSeaLevel = defaultSky.MieScatteringCoeffizientOnSeaLevel / sizeFactor,
            };

            graphic.AddSphere(skyMedia.EarthRadius, 10, 10, new ObjectPropertys() { TextureFile = groundColor });
            graphic.AddSphere(skyMedia.AtmosphereRadius, 10, 10, new ObjectPropertys() { TextureFile = "#FFFFFF", MediaDescription = skyMedia, RefractionIndex = 1, ShowFromTwoSides = true });
            graphic.AddSquareXY(1, 1, 1, new ObjectPropertys() { Orientation = new Vector3D(90, -sunDegree, 0), RaytracingLightSource = new FarAwayDirectionLightDescription() { Emission = 20 } });

            int cloudCount = 7;
            float size = 2000; //So breit/lang ist das Wolkenrechteck
            Random rand = new Random(positionSeed); //1 = Tagsüber-Wolkenposition; 2 = Abends-Wolkenposition

            for (int i = 0; i < cloudCount; i++)
            {
                if (shape == CloudShape.Cube)
                {
                    //Cloud
                    graphic.AddCube(300 + ((float)rand.NextDouble() * 100), (300 + ((float)rand.NextDouble() * 100)) * 0.8f, 300 + ((float)rand.NextDouble() * 500), new ObjectPropertys()
                    {
                        Position = new Vector3D((float)rand.NextDouble() * size * 1.0f + 1000, defaultSky.EarthRadius + cloudHeigh + (float)rand.NextDouble() * 100, (float)rand.NextDouble() * size - size / 2) * sizeFactor,
                        TextureFile = "#FF0000",
                        Size = 0.18f * 5 * sizeFactor,
                        RefractionIndex = 1, //Wenn der Brechungsindex 1 ist, dann hat das Medium kein Glas-Rand
                        MediaDescription = new DescriptionForCloudMedia()
                        {
                            //Für Cirrus-Wolken setze den DensityScalingFactor auf 0.1 und den PowExponent auf 0.5
                            RandomSeed = i,
                            ScatteringCoeffizent = new Vector3D(1, 1, 1) * 0.01f / sizeFactor,
                            AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 0.002f / sizeFactor,
                            AnisotropyCoeffizient = 0.30f,
                            TurbulenceFactor = 0.1f,
                            DensityScalingFactor = 0.4f,
                            PowExponent = 0.2f,
                            MinMetaballCount = 5,
                            MaxMetaballCount = 15,
                            ShellType = DescriptionForCloudMedia.CloudDrawingObject.AxialCube,
                        }
                    });
                }else
                {
                    //Cloud
                    graphic.AddSphere(100 + ((float)rand.NextDouble() * 400), 10, 10, new ObjectPropertys()
                    {
                        Position = new Vector3D((float)rand.NextDouble() * size * 1.0f + 1000, defaultSky.EarthRadius + cloudHeigh + (float)rand.NextDouble() * 100, (float)rand.NextDouble() * size - size / 2) * sizeFactor,
                        TextureFile = "#FF0000",
                        Size = 1.5f * sizeFactor,
                        RefractionIndex = 1, //Wenn der Brechungsindex 1 ist, dann hat das Medium kein Glas-Rand
                        MediaDescription = new DescriptionForCloudMedia()
                        {
                            RandomSeed = i,
                            ScatteringCoeffizent = new Vector3D(1, 1, 1) * 0.01f / sizeFactor,
                            AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 0.002f / sizeFactor,
                            AnisotropyCoeffizient = 0.30f,
                            TurbulenceFactor = 0.2f,
                            BlendingBetweenMetaballAndTurbulence = 0.9f,
                            DensityScalingFactor = 0.9f,
                            PowExponent = 0.2f,
                            MinMetaballCount = 10,
                            MaxMetaballCount = 15,
                            ShellType = DescriptionForCloudMedia.CloudDrawingObject.Sphere
                        }
                    });
                }
            }

            int treeCount;
            int treeArea;
            int b;
            if (cameraPosition == 0)
            {
                b = 100; treeArea = 600; treeCount = 30;      //Tagsüber
                graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, defaultSky.EarthRadius + 30, 0) * sizeFactor, Vector3D.Normalize(new Vector3D(1, 0.4f, 0)), 60.0f); //Tagsüber 
            }
            else
            {
                b = 20; treeArea = 300; treeCount = 30;      //Abends
                graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, defaultSky.EarthRadius + 10, 0) * sizeFactor, Vector3D.Normalize(new Vector3D(1, 0.3f, 0)), 60.0f); //Abends
            }
            for (int i = 0; i < treeCount; i++)
            {
                Vector3D pos = new Vector3D((float)rand.NextDouble() * treeArea * 2 + b, defaultSky.EarthRadius, (float)rand.NextDouble() * treeArea - treeArea / 2);
                graphic.AddCylinder(100, 1, 1, false, 4, new ObjectPropertys() { Position = pos * sizeFactor, TextureFile = "#663333", Size = 1 * sizeFactor });
                graphic.AddSphere(10, 10, 10, new ObjectPropertys() { Position = (pos + new Vector3D(0, 50, 0)) * sizeFactor, TextureFile = "#226600", Size = 1 * sizeFactor });
            }

            graphic.Mode = Mode3D.ThinMediaMultipleScattering;
        }

        //Stilllife aus dem SmallUpbp-Projekt
        public static void AddTestszene19_StillLife(GraphicPanel3D graphic)
        {
            bool useLowPoly = false;
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + (useLowPoly ? "19_StilllifeLowPoly.obj" : "19_Stilllife.obj"), true);

            graphic.GetAllObjects().ForEach(x => x.Name = x.Name.Split(new string[] { "True:" }, StringSplitOptions.None).Last());
            graphic.GetAllObjects().ForEach(x => x.Albedo = 1); //Bei den Objekten, wo die Textur FFFFFF ist, wurde Albedo bereits gesetzt (Graue-Wand über Albedo)
            graphic.GetAllObjects().ForEach(x => x.NormalInterpolation = InterpolationMode.Smooth);

            //Da ich nur 255 verschiedene Grautöne bei der TextureFile-Property habe, ist die Diffuse-Color etwas zu dunkel. Deswegen regel ich das über die Albedo 
            var table = graphic.GetObjectByName("Pozadi");
            table.Albedo = 0.2627f;
            table.TextureFile = "#FFFFFF";

            var wall = graphic.GetObjectByName("Pozadi2");
            wall.Albedo = 0.2627f;
            wall.TextureFile = "#FFFFFF";

            var environmentLight = graphic.GetAllObjects().Where(x => x.RaytracingLightSource is EnvironmentLightDescription).FirstOrDefault();
            if (environmentLight != null)
            {
                (environmentLight.RaytracingLightSource as EnvironmentLightDescription).CameraUpVector = new Vector3D(0, 0, 1);
                environmentLight.TextureFile = environmentLight.Color.As<ColorFromTexture>().TextureFile.Replace("<DataFolder>", DataDirectory);
            }


            //Tschechisch: sklo = Glas; Vino = Wein; svicka = Kerze
            //Pozadi = Tisch
            //Pozadi2 = Rückwand
            //Svetlo = Linkes AreaLight
            //Svetlo2 = Rechtes AreaLight
            //Objekt 1 - Kerze: svicka, knot
            //Objekt 2 - Eisblock oben: houbaNahore
            //Objekt 2 - Eisblock unten: houbaDole
            //Objekt 3 WeinGlas: skloVino, mediumVino
            //Objekt 4 SaftGlas: skloValec, mediumSklenice
            //Objekt 5 Grüne Karaffe: skloDzban, mediumDzban            

            //Fireflys-Erkentnisse: Die ContinuationPdf darf beim Media nicht niedrige als 0.8 sein, da sonst die Pfade beim grünen
            //Topf zu kurz werden. Außerdem benötigt man Point2Point-Merging (Surface + Media) um Fireflys effektiv zu beseitigen
            //Da die PdfLReverse beim PointDataPointQuerry vergesen wurde, war die Kerze zu dunkel.
            //Achtung: Beim Topf braucht man die ShadedNormal um die Background-Light-Reflektion richtig zu sehen

            //Sommer 2019: Ich habe unter Graphic8 zum ersten mal begonnen diese Szene zu probieren
            //22.5.2021: Ursachen für den Fleck am grünen Topf, den zu dunklen Boden und der angebliche PdfWReverse-Fehler bei UPBP (Alle 5 Fehler wurden nun gefixt)
            //-in der stilllife.obj.aux und stilllife.mtl-Datei waren die Diffuse- und Spekular-Farben verstellt gewesen (Diffuse war zu hell; Dunkler Fleck kam von Rückwand)
            //-Verwendung von Fliese(Gewichtete Summe) anstatt DiffuseAndMirrorWithSumBrdf(Ungewichtet) als Material für den Boden
            //-Fehlanname: BrdfSampleEvent.PdfW == Brdf.PdfW() (Sample-Ereignis != Summe aller Diffuse-PdfWs)
            //-Fehlanname: SubpathPoint.IsDiffuse == FullpathPoint.IsDiffuse (Enthält Material diffuse Komponene != Wurde Strahl diffuse reflektiert)
            //-Die Refracted-PdfWReverse bei SmallUPBP war doch nicht falsch. Ich hatte in der BrdfSampler-Klasse zwar ein BrdfGlas-Objekt mit 
            // verdrehter Input-Output und vertauschenten Brechungsindizes erstellt aber ich hatte die Normale nicht mit gedreht. Deswegen hat
            // Vector3D.FresnelTerm dann doch nicht die gewünschte PdfWReverse berechnet. Es gilt also bei Specular: PdfW==PdfWReverse
            //-SmallUPBP verwendet kein CosAtCamera-Faktor (Lichtsensoren in Kamera sind Kugel/Kugelförmig angeordnet)

            //6.6.2021: Die Kerze war noch etwas zu hell, da der Photonmap-Searchradius zu groß war. Ich regel sie nun mit PhotonmapSearchRadiusFactor runter
            //Der Suchradius hängt ja vom Pixelfootprint ab. Bei den großen 1600x700-Bild ist der Pixelfootprint kleiner und deswegen war der
            //Photonmap-Fehler kleiner und die Farbe der Kerze ok. Beim [160*4;70*4]-Bild ist der Kerzenfehler dann aufgefallen. 
            //Offene Frage: Wenn ich ein zu großen Suchradius angebe, dann ist klar, dass dort ein Kernelfehler ist, der nicht duch noch mehr Samples weggeht.
            //Wenn ich aber beim 1600x700-Bild ein Radiusfaktor von 0.1 habe, dann ist nach 300 Samples das Bild immer noch zu dunkel. Nach 900 Samples ist PP alleine dann ok. 
            //BPT+PP ist nach 900 Samples noch nicht ganz ok aber die Tendenz ist da das es gegen das Korrekte läuft.
            //Kann ein Suchradius zu klein sein und dafür sorgen, dass das Bild zu dunkel bleibt? Anscheinend nicht. Man braucht nur mehr Samples.
            //PhotonmapSearchRadiusFactor dient nur für die Media aber nicht für SurfacePhotonmap, da ich sonst Fireflys erhalte, wenn SurfaceSuchradius zu klein ist
            //-Der Tent-Filter erzeugt ein helleres Bild (bei dem 10*10-Kerzenbild) als der Equal-Filter
            //21.6.2021: Bei der PdfW/PdfA-Berechung vom Hdr-Light scheint es noch Probleme zu geben, weswegen Pathtracing+VertexConnection was anders liefert, als nur Pathtracing
            //Hinweis: Wenn man CBPPBL-Pfade vom VertexConnection gegen Pathtracing vergleichen will, braucht man viele Samples da der
            //Tisch das Umgebungslicht groß macht und die Lighttracing-Trefferwahrscheinlichkeit gering. In solchen Fällen, wo man
            //ein Verfahren hat, was wenig Samples braucht und eins, was viel braucht, kann man per MIS das leichter untersuchen indem
            //ich erst Pathtracing alleine für Referenzwert und dann Pathtracing+VertexConnection per MIS.
            //25.6.2021: Ich habe beim Hdr-Light 2 Dinge gefixt: Korrekte toLight für GetPdfWFromLightDirectionSampling; SphereWithImageSampler->SamplePointOnSphere->PdfA = GetPdfAFromPointOnSphere(direction)
            //Wenn ich nun die PixelRadiance-Summe (Mit Kamera-Equal) mit UPBP erzeuge, bekomme ich:1552 [104,496208;50,8509026;25,2200603]; Referenz=[101,786827;49,8002434;24,6436749]
            //7.8.2021: Gelerntes Wissen, was ich beim BPT-Kerze-Pixel-Problem gelernt habe: (BPT-Pixel-Farbe war zu dunkel)
            // Die Continuation bei der Brdf hat ein großen Einfluß auf die Konvergenzgeschwindigkeit. Man sollte für jeden Selected Pfad, den eine Brdf erzeugen kann die
            // BrdfColorAfterSampling = Color / (SelectionPdf * ContinuationPdf) berechnen und darauf achten, dass dort ein Pfadgewicht von 1 raus kommt. Über die Pathtracing - PathContribution
            // kann ich leicht kontrollieren, ob denn das Pfadgewicht immer 1 ist und meine ContinuationPdf die korrekte Formel ergibt. 
            // Die LightPickProp sollte laut Emission gewählt werden, insofern nicht eine kleine Lampe besonders nah an der Kamera ist und somit heller wirkt, als sie im Verhältnis zu den anderen
            // Lampen eigentlich ist. 
            //4.9.2021: BPT erzeugt eine zu dunkle Kerze. Pathtracing alleine stimmt aber PT+VC erzeugt den Fehler. PT+DL ist ok. Bei g=0.8 ist VC zu dunkel. Bei g=0 stimmt VC.
            //1.10.2021 (Ich feiere 10 Jahre AIS): BPT war zu dunkel, da bei VC an den Connectionspoints in Reverse-Richtung die ReversePdfA vom Subpfad genommen wurde anstatt sie neu über die ReversePdfW aus der GetBrdf zu berechnen
            //25.10.2021: Wenn ich PhotonmapSearchRadiusFactor=0.5 verwende, dann stimmt die Kerze nun. 
            //            Der Tisch (nicht die Rückwand) ist sowohl bei BPT als auch UPBP noch etwas zu hell.
            //            Pixel[60;270]: UPBP-Referenz=[219;213;204]; BPT-Referenz=[219;213;205]; PT-Referenz(1 Million Samples)=[217;211;203]
            //            Pixel[60;270]: UPBP=[215;209;201]; BPT=[216;210;201];  -> Wenn ich Albedo vom Tisch auf 1 setze und TextureFile auf #424242
            //            Pixel[60;270]: PT/BPT=[217;211;203]                    -> Wenn ich Albedo vom Tisch auf 0.2627 setze und TextureFile auf #FFFFFF
            //16.11.2021: Die grüne Kaustik sieht beim Referenz-UPBP etwas heller aus als mein UPBP und bei mein BPT sieht die Kaustik gleich aus wie beim Referenz UPBP/BPT
            //            Ich denke das ist nur ein scheinbarer Fehler bei mein UPBP/Photonmapping da sowohl bei mein BPT als auch Referenz-UPBP/BPT kommt die Helligkeit durch
            //            Fireflys zustande. Wenn ich einzelne Pixel von mein UPBP mit mein BPT mit 500k vergleiche, dann stimmt es.
            //            Beim Differenzbild sehe ich bei UPBP links und rechts im Farbton unterschiede. Ich denke es kommt weil mein Surface-Searchradius aus dem PixelFootprint kommt und SmallUPBP hat ein globalen Radius
            //            Das heißt für mich mit den heutigen Tage ist für mich Stilllife geschaft.
            //            Bei Mirrorballs ist der vordere Bildbereich zu dunkel und der hintere Bereich zu hell. Ursache: Unbekannt

            //Warum nimmt SmallUPBP ShortBeams für die LightSubpaths und keine LongBeams?

            //So bestimme ich, wie viel Partikel pro Längeneinheit in der Kerze sind
            //var kerze = graphic.GetObjectByName("svicka");
            //var box = graphic.GetBoundingBoxFromObject(kerze.Id);
            //RadiusInTheBox: 9.79995, RadiusOutTheBox: 17.7948952, XSize: 20.3517017, YSize: 21.6399, ZSize: 19.5999
            //BrdfModel: MirrorGlas, MirrorColor: { new Vector3D(0.600000024f, 0.600000024f, 0.600000024f)}, RefractionIndex: 1.446, TextureFile: "#999999", 
            //AbsorbationCoeffizent: { new Vector3D(0.0299999993f, 0.100000001f, 0.200000003f)}, AnisotropyCoeffizient: 0.8, ScatteringCoeffizent: { new Vector3D(1.5f, 1.5f, 1.5f)}

            graphic.Mode = Mode3D.UPBP;
            graphic.GlobalSettings.UseCosAtCamera = false;
            //graphic.GlobalSettings.AutoSaveMode = RaytracerAutoSaveMode.FullScreen; //Mein UnitTest soll kein AutoSave verwenden
            graphic.GlobalSettings.RecursionDepth = 80;
            graphic.GlobalSettings.BackgroundImage = "#000000";
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.GammaOnly;
            //graphic.GlobalSettings.PhotonCount = 160 * 70 * 2 * 2; //Mehr Photonen nicht da ich sonst eine OutOfMemoryException bekomme
            graphic.GlobalSettings.PhotonCount = 100000;
            graphic.GlobalSettings.BeamDataLineQueryReductionFactor = 0.1f;
            graphic.GlobalSettings.PhotonmapSearchRadiusFactor = 1.0f; //Bildgröße: [160*1:70*1] -> 0.1; [160*4:70*4] -> 0.5; [160*10:70*10] -> 1.0
            graphic.GlobalSettings.SamplingCount = 50000;
            graphic.GlobalSettings.CameraSamplingMode = PixelSamplingMode.Equal;
            graphic.GlobalSettings.LightPickStepSize = 0; //Damit Pathtracing gegenüber VertexConnection mehr gewicht bekommt, da sonst 25% aller Lighttracing-Pfade vom Environment starten obwohl es nur 1% des Emission-Anteils hat; 2: Die AreaLights werden mit 40% Ausgewählt, das Environment mit 20%; 0: AreaLight1 45%, AreaLight2 54%, Environment 1%

            graphic.GlobalSettings.Camera = GraphicPanel3D.ReadCameraFromObjAuxFile(DataDirectory + "19_Stilllife.obj.aux");
        }

        //Mirrorballs aus dem SmallUpbp-Projekt
        public static void AddTestszene20_Mirrorballs(GraphicPanel3D graphic)
        {
            //Gefundene Fehler:
            //-Die Scene war sehr groß skaliert. Deswegen hat der Visible-Test oft nicht geklappt, da Visible-Point zu TestPoint größer MagicNumbers.DistanceForPoint2PointVisibleCheck  war 
            //-Ich muss beim Specularen reflektieren/brechen auf jeden Fall die ShadedNormal nehmen und nicht die FlatNormal! Sonst bekomme ich komische Muster an den Wänden
            //-Die linke Lample sieht bei BPT ok aus und bei UPBP mit grauen Schleier. Im großen Referenzbild scheint das auch so zu sein. Ist die Reflektion der Decke.
            //-Das Bild ist etwas zu dunkel. Wenn ich ein PhotonmapSearchRadiusFactor von 0.5 verwende, hat das kein Einfluß

            //mirrorballs = Originalscene; mirrorballs1 = Ohne Luft mit Chromekugeln; mirrorballs2=Nur rechte Lampe; mirrorballs3 = Ohne Luft

            graphic.RemoveAllObjekts();

            float sizeFactor = 0.01f; //Die Scene ist sehr groß. Dadurch sagt dann der Visibletester aufgrund
                                      //zu großen Abstands zwischen Sichtstrahlpunkt und Zielpunkt, dass der
                                      //Punkt nicht sichtbar ist. Ich könnte nun entweder MagicNumbers.DistanceForPoint2PointVisibleCheck
                                      //vergrößern (Globale Einstellung) oder ich skaliere die Scene kleiner
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "20_Mirrorballs.obj", true, new ObjectPropertys() { Size = sizeFactor });

            graphic.GetAllObjects().ForEach(x => x.Name = x.Name.Split(new string[] { "True:" }, StringSplitOptions.None).Last());
            graphic.GetAllObjects().ForEach(x => x.Albedo = 1); //Bei den Objekten, wo die Textur FFFFFF ist, wurde Albedo bereits gesetzt (Graue-Wand über Albedo)
            graphic.GetAllObjects().ForEach(x => x.NormalInterpolation = InterpolationMode.Smooth);

            //So habe ich die Objekte ermittelt, dessen Farbe ich über die Albedo und nicht über die TextureFile einstelle
            var grayObjects = graphic.GetAllObjects().Where(x => x.Color.Type == ColorSource.ColorString && PixelHelper.IsGrayColor(x.Color.As<ColorFromRgb>().Rgb) && x.RaytracingLightSource == null && x.RefractionIndex != 1).ToList();

            //Da ich nur 255 verschiedene Grautöne bei der TextureFile-Property habe, ist die Diffuse-Color etwas zu dunkel. Deswegen regel ich das über die Albedo 
            var ceiling = graphic.GetObjectByName("CeilingCorner_0001_diffuse_white");
            ceiling.Albedo = 0.588f;
            ceiling.TextureFile = "#FFFFFF";

            var walls = graphic.GetObjectByName("Walls_0001_diffuse_white");
            walls.Albedo = 0.588f;
            walls.TextureFile = "#FFFFFF";

            graphic.GetAllObjects().Where(x => x.RaytracingLightSource != null).ToList().ForEach(x => x.RaytracingLightSource.Emission *= (sizeFactor * sizeFactor));
            foreach (var m in graphic.GetAllObjects().Where(x => x.MediaDescription != null && x.MediaDescription is DescriptionForHomogeneousMedia))
            {
                (m.MediaDescription as DescriptionForHomogeneousMedia).AbsorbationCoeffizent /= sizeFactor;
                (m.MediaDescription as DescriptionForHomogeneousMedia).ScatteringCoeffizent /= sizeFactor;
            }


            graphic.Mode = Mode3D.UPBP;
            graphic.GlobalSettings.UseCosAtCamera = false;
            graphic.GlobalSettings.RecursionDepth = 12;
            graphic.GlobalSettings.BackgroundImage = "#000000";
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.GammaOnly;
            graphic.GlobalSettings.PhotonCount = 160 * 70 * 1 * 1; //Mehr Photonen nicht da ich sonst eine OutOfMemoryException bekomme
            graphic.GlobalSettings.BeamDataLineQueryReductionFactor = 0.1f;
            graphic.GlobalSettings.PhotonmapSearchRadiusFactor = 0.5f;
            graphic.GlobalSettings.SamplingCount = 50000;
            graphic.GlobalSettings.CameraSamplingMode = PixelSamplingMode.Equal;

            graphic.GlobalSettings.Camera = GraphicPanel3D.ReadCameraFromObjAuxFile(DataDirectory + "20_Mirrorballs.obj.aux");
            graphic.GlobalSettings.Camera.Position *= sizeFactor;
        }

        //Candle aus dem SmallUpbp-Projekt
        public static void AddTestszene21_Candle(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();

            graphic.AddWaveFrontFileAndSplit(DataDirectory + "21_Candle.obj", true);

            graphic.GetAllObjects().ForEach(x => x.Name = x.Name.Split(new string[] { "True:" }, StringSplitOptions.None).Last());
            graphic.GetAllObjects().ForEach(x => x.Albedo = 1);
            graphic.GetAllObjects().ForEach(x => x.NormalInterpolation = InterpolationMode.Smooth);


            graphic.Mode = Mode3D.UPBP;
            graphic.GlobalSettings.UseCosAtCamera = false;
            graphic.GlobalSettings.RecursionDepth = 12;
            graphic.GlobalSettings.BackgroundImage = "#000000";
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.GammaOnly;
            graphic.GlobalSettings.PhotonCount = 160 * 70 * 2 * 2; //Mehr Photonen nicht da ich sonst eine OutOfMemoryException bekomme
            graphic.GlobalSettings.BeamDataLineQueryReductionFactor = 0.1f;
            graphic.GlobalSettings.SamplingCount = 50000;

            graphic.GlobalSettings.Camera = GraphicPanel3D.ReadCameraFromObjAuxFile(DataDirectory + "21_Candle.obj.aux");
        }

        //Testscene für Texturmapping, Normalmapping und Parallaxmapping -> Ziel: Alle Rasterizer-Verfahren/Raytracer/Blender-ScreenShoot sehen mit und ohne Blender-Import gleich aus
        //Außerdem kann ich so testen, dass meine CreateSquareXY und CreateCube-Funktion das gleiche anzeigt wie Blender
        public static void AddTestszene22_ToyBox(GraphicPanel3D graphic)
        {
            bool takeDateFromBlender = false;
            graphic.RemoveAllObjekts();
            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";
            graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, 0, 0), new Vector3D(0, 0, -1), 45); //Einheitsmatrix bei der LookAtMatrix
            if (takeDateFromBlender)
            {
                graphic.AddWaveFrontFileAndSplit(DataDirectory + "22_ToyCube.obj", false, new ObjectPropertys() { Position = new Vector3D(0, 0, 0), Orientation = new Vector3D(0, 0, 0), Size = 1.0f, BrdfModel = BrdfModel.Diffus, NormalInterpolation = InterpolationMode.Flat });
                foreach (var obj in graphic.GetAllObjects()) obj.Name = obj.Name.Split(new string[] { "True:" }, StringSplitOptions.None).Last();
                graphic.GetObjectByNameStartsWith("Licht").RasterizerLightSource = new RasterizerLightSourceDescription() { SpotCutoff = 180.0f, SpotExponent = 1, ConstantAttenuation = 1.5f };
                graphic.GetObjectByNameStartsWith("Licht").RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = 220000 };
                return;
            }

            graphic.AddCube(0.5f, 0.5f, 0.5f, new ObjectPropertys()
            {
                Position = new Vector3D(0, 0, -5),
                Orientation = new Vector3D(0, 20, 0),
                TextureFile = DataDirectory + "toy_box_diffuse.png",
                BrdfModel = BrdfModel.Diffus,
                SpecularHighlightPowExponent = 0, //20 (Der Rasterizer erstellt ein Phong-Material, wenn hier != 0 steht)
                NormalSource = new NormalFromParallax() { ParallaxMap = DataDirectory + "toy_box_NormalAndHigh.png", TexturHeightFactor = 0.2f },
                NormalInterpolation = InterpolationMode.Flat,
                ShowFromTwoSides = false
            });

            //Hiermit zeige ich wie das Koordinatensystem aussieht
            //Die X-Achse geht von Fenster-Links bis Fenster-Rechts (Im Blender ist das die rote X-Achse)
            //Die Y-Achse geht von Fenster-Unten bis Fenster-Oben (Im Blender ist das die blaue Z-Achse)
            //Die Z-Achse geht von -1 (Nah) bis -10 (Weit weg) (In Blender ist das die grüne negierte Y-Achse)
            Vector3D sphereCenter = new Vector3D(-1.8f, 1.2f, -5);
            graphic.AddSphere(0.3f, 10, 10, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Envwall.bmp", TextureMatrix = Matrix3x3.Scale(10, 10) },  NormalSource = new NormalFromMap() { NormalMap = DataDirectory + "Huckel.bmp", TextureMatrix = Matrix3x3.Scale(10, 10) }, Position = sphereCenter + new Vector3D(0, 0, 0), Orientation = new Vector3D(0, 0, 0), SpecularHighlightPowExponent = 20, NormalInterpolation = InterpolationMode.Smooth });
            graphic.AddSphere(0.3f, 10, 10, new ObjectPropertys() { TextureFile = "#00FF00", NormalSource = new NormalFromParallax() { ParallaxMap = DataDirectory + "Huckel.bmp", TextureMatrix = Matrix3x3.Scale(10, 10) }, Position = sphereCenter + new Vector3D(0.6f, 0, 0), Orientation = new Vector3D(20 + 80, -30 + 80, 40 + 80), SpecularHighlightPowExponent = 20, NormalInterpolation = InterpolationMode.Smooth });
            graphic.AddSphere(0.3f, 20, 10, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Schildkroete.png", TextureMatrix = Matrix3x3.Scale(10, 10) }, NormalSource = new NormalFromMap() { NormalMap = DataDirectory + "Huckel.bmp", TextureMatrix = Matrix3x3.Scale(10, 10) }, Position = sphereCenter + new Vector3D(0, 0.6f, 0), Orientation = new Vector3D(20 + 180, -30 + 180, 40 + 80), SpecularHighlightPowExponent = 20, NormalInterpolation = InterpolationMode.Smooth });

            //Texturmapping
            graphic.AddSquareXY(0.5f, 0.5f, 1, new ObjectPropertys() { TextureFile = DataDirectory + "Weltkarte.png", Position = new Vector3D(1.8f, 1.2f, -5), Orientation = new Vector3D(0, 0, 0), NormalInterpolation = InterpolationMode.Flat, ShowFromTwoSides = false });

            //Normalmapping
            graphic.AddSquareXY(0.4f, 0.4f, 1, new ObjectPropertys() { TextureFile = DataDirectory + "nes_super_mario_bros.png", NormalSource = new NormalFromMap() { NormalMap = DataDirectory + "normalMap.png" }, Position = new Vector3D(1.3f, 0.4f, -5), Orientation = new Vector3D(-45, 0, 0), ShowFromTwoSides = false });

            //Flat-Parallax-Mapping mit ColorString mit Edge-Cutoff
            graphic.AddSquareXY(0.4f, 0.4f, 1, new ObjectPropertys() { TextureFile = "#0088FF", Position = new Vector3D(1.3f, -0.1f, -5), Orientation = new Vector3D(-70, 0, 0), ShowFromTwoSides = false, NormalSource = new NormalFromParallax() { ParallaxMap = DataDirectory + "thumb_COLOURBOX5847554_Bumpmap.bmp", TexturHeightFactor = 0.003f, IsParallaxEdgeCutoffEnabled = true, TextureMatrix = Matrix3x3.Scale(0.1f, 0.1f) } });

            //Flat-Parallax-Mapping mit ColorString ohne Edge-Cutoff
            graphic.AddSquareXY(0.4f, 0.4f, 1, new ObjectPropertys() { TextureFile = "#0088FF", Position = new Vector3D(2.1f, -0.2f, -5), Orientation = new Vector3D(-70, 0, 0), Size = 0.8f, ShowFromTwoSides = false, NormalSource = new NormalFromParallax() { ParallaxMap = DataDirectory + "thumb_COLOURBOX5847554_Bumpmap.bmp", TexturHeightFactor = 0.003f, IsParallaxEdgeCutoffEnabled = false, TextureMatrix = Matrix3x3.Scale(0.1f, 0.1f) } });


            //Textur-Spriteeffekt
            graphic.AddSquareXY(0.1f, 0.1f, 1, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Zahlen.png", TextureMatrix = Matrix3x3.SpriteMatrix(3, 2, 0) }, Position = new Vector3D(1.9f, 0.4f, -5), Orientation = new Vector3D(0, 0, 0), ShowFromTwoSides = false,  });
            graphic.AddSquareXY(0.1f, 0.1f, 1, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Zahlen.png", TextureMatrix = Matrix3x3.SpriteMatrix(3, 2, 1) }, Position = new Vector3D(2.2f, 0.4f, -5), Orientation = new Vector3D(0, 0, 0), ShowFromTwoSides = false });
            graphic.AddSquareXY(0.1f, 0.1f, 1, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Zahlen.png", TextureMatrix = Matrix3x3.SpriteMatrix(3, 2, 2) }, Position = new Vector3D(2.5f, 0.4f, -5), Orientation = new Vector3D(0, 0, 0), ShowFromTwoSides = false });
            graphic.AddSquareXY(0.1f, 0.1f, 1, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Zahlen.png", TextureMatrix = Matrix3x3.SpriteMatrix(3, 2, 3) }, Position = new Vector3D(1.9f, 0.1f, -5), Orientation = new Vector3D(0, 0, 0), ShowFromTwoSides = false });
            graphic.AddSquareXY(0.1f, 0.1f, 1, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Zahlen.png", TextureMatrix = Matrix3x3.SpriteMatrix(3, 2, 4) }, Position = new Vector3D(2.2f, 0.1f, -5), Orientation = new Vector3D(0, 0, 0), ShowFromTwoSides = false });
            graphic.AddSquareXY(0.1f, 0.1f, 1, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Zahlen.png", TextureMatrix = Matrix3x3.SpriteMatrix(3, 2, 5) }, Position = new Vector3D(2.5f, 0.1f, -5), Orientation = new Vector3D(0, 0, 0), ShowFromTwoSides = false });

            //Flat-Parallaxmapping mit ColorTextur und Edge-Cutoff
            graphic.AddSquareXY(3, 3, 1, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Decal.bmp", TextureFilter = TextureFilter.Linear }, Position = new Vector3D(0, -1, -5), Orientation = new Vector3D(-80, 0, 0), ShowFromTwoSides = false, NormalSource = new NormalFromParallax() { ParallaxMap = DataDirectory + "Decal.bmp", ConvertNormalMapFromColor = true, TexturHeightFactor = 0.04f, IsParallaxEdgeCutoffEnabled = true}, NormalInterpolation = InterpolationMode.Flat,   HasStencilShadow = false });

            //Texturfilter - Point
            graphic.AddSquareXY(0.4f, 4.0f, 1, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Linien.png", TextureFilter = TextureFilter.Point }, Position = new Vector3D(-2.2f - 1, 0.4f, -5 - 3), Orientation = new Vector3D(-80, 0, 20), ShowFromTwoSides = false, NormalInterpolation = InterpolationMode.Flat });

            //Texturfilter - Linear
            graphic.AddSquareXY(0.4f, 4.0f, 1, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "Linien.png", TextureFilter = TextureFilter.Linear }, Position = new Vector3D(-1.3f - 1, 0.4f, -5 - 3), Orientation = new Vector3D(-80, 0, 20), ShowFromTwoSides = false, NormalInterpolation = InterpolationMode.Flat });

            Vector3D lightPos = new Vector3D(-5, 2, 0);
            graphic.AddSphere(1, 10, 10, new ObjectPropertys() //Light
            {
                Position = lightPos,
                TextureFile = "#FFFFFF",
                RasterizerLightSource = new RasterizerLightSourceDescription() { SpotCutoff = 90.0f, SpotExponent = 1, ConstantAttenuation = 1.0f, SpotDirection = Vector3D.Normalize(new Vector3D(0, -1, -5) - lightPos), CreateShadows = true },
                RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = 220000 }
            });
        }

        public static void AddTestszene23_MirrorShadowWithSphere(GraphicPanel3D graphic)
        {
            AddTestszene23_MirrorShadow(graphic, true);
        }

        public static void AddTestszene23_MirrorShadowNoSphere(GraphicPanel3D graphic)
        {
            AddTestszene23_MirrorShadow(graphic, false);
        }

        //Rasterizer-Testzene für Schatten/Blending/SchwarzIsTransparent/Mirror/Cubemapping/Siolette/ExplosionsEffekt
        private static void AddTestszene23_MirrorShadow(GraphicPanel3D graphic, bool showMirrorSphere)
        {
            graphic.RemoveAllObjekts();
            graphic.GlobalSettings.BackgroundImage = "#FFFFFF";

            //Fackel mit SchwarzIsTransparent, Billboard-Effekt und NoLight
            graphic.AddSquareXY(0.5f, 0.7f, 1, new ObjectPropertys() { Position = new Vector3D(1, 2.5f, -8), Orientation = new Vector3D(0, 0, 0), Color = new ColorFromTexture() { TextureFile = DataDirectory + "Fire2.jpg", TextureMode = TextureMode.Clamp, TextureMatrix = Matrix3x3.SpriteMatrix(5, 3, 8) }, BlackIsTransparent = true, HasBillboardEffect = true, CanReceiveLight = false, HasStencilShadow = true });
            graphic.AddTorch(0.2f, 5, new ObjectPropertys() { Position = new Vector3D(1, 0, -8), TextureFile = "#224400", NormalInterpolation = InterpolationMode.Smooth, HasStencilShadow = true });

            if (showMirrorSphere)
            {
                graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, 3, 10), new Vector3D(0, 0, -1), 12);

                graphic.AddSphere(0.25f, 10, 10, new ObjectPropertys() { Position = new Vector3D(3, 3, 0), NormalInterpolation = InterpolationMode.Smooth, TextureFile = "#FF0000" });
                graphic.AddSphere(0.25f, 10, 10, new ObjectPropertys() { Position = new Vector3D(0, 6, 0), NormalInterpolation = InterpolationMode.Smooth, TextureFile = "#00FF00" });

                // Metallkugel (Cubemapping)
                graphic.AddSphere(1, 10, 10, new ObjectPropertys() { Position = new Vector3D(0, 3, 0), NormalInterpolation = InterpolationMode.Smooth, TextureFile = "#FFFFFF", UseCubemap = true, BrdfModel = BrdfModel.Mirror, RefractionIndex = 1, SpecularAlbedo = 1 });
            }
            else
            {
                graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, 3, 10), new Vector3D(0, -0.2f, -1), 55);
            }

            //Boden
            graphic.AddSquareXY(10, 10, 1, new ObjectPropertys() { Position = new Vector3D(0, 0, 0), Orientation = new Vector3D(-90, 0, 0), TextureFile = "#AAAAAA", ShowFromTwoSides = false, NormalInterpolation = InterpolationMode.Flat });

            //Spiegel (MirrorPlane)
            graphic.AddMirrorFrame(5, 6, 0.5f, 5, new ObjectPropertys() { Position = new Vector3D(-5, 3, -3), Orientation = new Vector3D(0, 40, 0), NormalInterpolation = InterpolationMode.Smooth, TextureFile = "#FFFFFF", ShowFromTwoSides = true, HasStencilShadow = false, UseCubemap = false, HasSilhouette = true });
            graphic.AddSquareXY(3.0f, 1.5f, 1, new ObjectPropertys() { Position = new Vector3D(-5, 3, -3), Orientation = new Vector3D(0, 40, 90), NormalInterpolation = InterpolationMode.Flat, TextureFile = "#0000FF", IsMirrorPlane = true, BrdfModel = BrdfModel.Mirror, MirrorColor = PixelHelper.StringToColorVector("#CCCCFF"), RefractionIndex = 1, SpecularAlbedo = 1 });

            //Schrift die Schatten wirft
            graphic.Add3DText("Hallo", 10, 2, new ObjectPropertys() { Position = new Vector3D(0, 1, 0), Orientation = new Vector3D(0, -20, 0), Size = 0.2f, NormalInterpolation = InterpolationMode.Flat, TextureFile = "#FFFF00", HasStencilShadow = true });

            //Blending
            graphic.Add3DBitmap(DataDirectory + "Pflanze.png", 2, new ObjectPropertys() { TextureFile = DataDirectory + "Pflanze.png", Size = 0.03f, Position = new Vector3D(3, 0.8f, 3), NormalInterpolation = InterpolationMode.Flat, HasStencilShadow = true, Opacity = 0.5f, BrdfModel = BrdfModel.TextureGlass, RefractionIndex = 1.5f, MirrorColor = new Vector3D(1, 1, 1) * 1.0f, SpecularAlbedo = 1 });

            //Perlin Noise Heightmap
            graphic.AddPerlinNoiseHeightmap(15, 15, 1, new ObjectPropertys() { Size = 0.2f, Position = new Vector3D(-4, 0.5f, 4), Orientation =new Vector3D(90,0,0), NormalInterpolation = InterpolationMode.Smooth, TextureFile = DataDirectory + "Mario.png", HasStencilShadow = false, CanReceiveLight = false, IsWireFrame = true });

            //Hintergrund-Bild
            graphic.AddCube(20, 20, 20, new ObjectPropertys() { Color = new ColorFromTexture() { TextureFile = DataDirectory + "nes_super_mario_bros.png", TextureMatrix = Matrix3x3.Scale(2, 3) }, ShowFromTwoSides = true,  NormalInterpolation = InterpolationMode.Flat });

            //Explosionseffekt
            graphic.AddSphere(0.5f, 10, 10, new ObjectPropertys() { Position = new Vector3D(6, 3, -3), TextureFile = "#FF00FF", ShowFromTwoSides = true, HasExplosionEffect = true });
            graphic.GlobalSettings.ExplosionRadius = 1;
            graphic.GlobalSettings.Time = 7000;

            //Lichtquelle 1 = Spotlicht
            Vector3D lightPos = new Vector3D(-7, 9, 5);
            Vector3D spotDirection = Vector3D.Normalize(new Vector3D(0, 0, 0) - lightPos);
            graphic.AddSphere(1, 10, 10, new ObjectPropertys() { Position = lightPos, HasStencilShadow = false, CanReceiveLight = false, TextureFile = "#FFFFFF", RasterizerLightSource = new RasterizerLightSourceDescription() { SpotDirection = spotDirection, SpotCutoff = 70, ConstantAttenuation = 0.8f }, RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = 220000 } });

            //Lichtquelle 2 = Umgebungslicht
            graphic.AddSphere(1, 10, 10, new ObjectPropertys() { Position = new Vector3D(0, 10, 0), HasStencilShadow = false, CanReceiveLight = false, TextureFile = "#FFFFFF", RasterizerLightSource = new RasterizerLightSourceDescription() { ConstantAttenuation = 1.0f, LinearAttenuation = 0.0002f, QuadraticAttenuation = 0.0004f, CreateShadows = false }, RaytracingLightSource = new DiffuseSphereLightDescription() { Emission = 220000 } });
        }

        public static void AddTestszene24_EnvironmentMaterialTest(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            Vector3D cameraLookAtPosition = new Vector3D(0, 1, 0);
            Vector3D cameraPosition = new Vector3D(0, 5.5f, 15);
            graphic.AddSquareXY(17, 17, 1, new ObjectPropertys() { Orientation = new Vector3D(-90, 0, 0), Color = new ColorFromTexture() { TextureFile = DataDirectory + "CheckBoardPlus.png", TextureMatrix = Matrix3x3.Scale(22, 22) }, Albedo = 0.3f });

            //Hinweis: Bei Wide-Street hat man ein unbewölkten Himmel. D.h. eine kleine Stelle der Leuchtet ganz stark und der Rest ganz wenig
            //Pathtracing trifft nur ganz schlecht beim diffusen Brdf-Sampeln die Sonne, was dazu führt, dass der Boden sehr dunkel aussieht.
            //Bei der piazza_san_marco-Szene ist der Himmel bewölkt so dass es in alle Himmelsrichtungen ungefähr gleichhell ist (Diffuses Licht)
            //Deswegen sieht dort der Boden vom Pathtracing gleich aus wie der vom Ligthtracing

            graphic.AddSphere(1, 10, 10, new ObjectPropertys() { Position = new Vector3D(-4.5f, 1, 0), TextureFile = "#FFFFFF", BrdfModel = BrdfModel.TextureGlass, RefractionIndex = 1.5f, SpecularAlbedo = 1 });
            graphic.AddSphere(1, 10, 10, new ObjectPropertys() { Position = new Vector3D(-1.5f, 1, 0), TextureFile = "#FFFFFF", BrdfModel = BrdfModel.Diffus, Albedo = 0.6f });
            graphic.AddSphere(1, 10, 10, new ObjectPropertys() { Position = new Vector3D(+1.5f, 1, 0), TextureFile = "#FFFFFF", BrdfModel = BrdfModel.Mirror, SpecularAlbedo = 1.0f });
            graphic.AddSphere(1, 10, 10, new ObjectPropertys() { Position = new Vector3D(+4.5f, 1, 0), TextureFile = "#40CCDD", BrdfModel = BrdfModel.FresnelTile, RefractionIndex = 1.3f, Albedo = 0.8f, SpecularAlbedo = 0.8f });


            graphic.AddSphere(1, 10, 10, new ObjectPropertys()
            {
                Position = new Vector3D(0, -5, 0),
                TextureFile = DataDirectory + "wide_street_01_1k.hdr", //https://hdrihaven.com/hdri/?h=wide_street_01
                //TextureFile = DataDirectory + "piazza_san_marco_1k.hdr", //https://hdrihaven.com/hdri/?h=piazza_san_marco
                RaytracingLightSource = new EnvironmentLightDescription() { Emission = 1, Rotate = 0 }
            });

            graphic.GlobalSettings.Camera = new Camera(cameraPosition, Vector3D.Normalize(cameraLookAtPosition - cameraPosition), 11.75f);
            graphic.Mode = Mode3D.VertexConnectionMerging;
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.GammaOnly;
            graphic.GlobalSettings.SamplingCount = 1000;
        }

        //Um Rapso eine Anfragen mit der einzelnen Kugel + Hdr-Map (Von ihn Cubemap genannt!!!) zu bearbeiten, habe ich diese Szene hier
        public static void AddTestszene25_SingleSphereForRapso(GraphicPanel3D graphic)
        {
            bool showHdrLightOnly = false;
            graphic.RemoveAllObjekts();
            
            graphic.AddSphere(1, 10, 10, new ObjectPropertys()
            {
                TextureFile = DataDirectory + "wide_street_01_1k.hdr",
                RaytracingLightSource = new EnvironmentLightDescription() { Emission = 1, Rotate = 0.375f }
            });

            if (showHdrLightOnly)
            {
                graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, 0, 0), new Vector3D(0, 0, +1), 360); 
            }else
            {
                graphic.AddSphere(2, 10, 10, new ObjectPropertys() { Position = new Vector3D(0, 0, 0), TextureFile = "#FFFFFF", BrdfModel = BrdfModel.Mirror, SpecularAlbedo = 1.0f });
                graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, 0, 10), new Vector3D(0, 0, -1), 80);
            }
            
        }

        //Hiermit kann ich mir selber meine Hdr-Bilder für den Himmel erstellen
        public static void AddTestszene26_SkyEnvironmapCreator(GraphicPanel3D graphic)
        {
            bool useHdrLight = false; //True wenn ich Schritt 1 mache; sonst false
            //Vorgehen, um eine gegebene Hdr-Map künstlich nachzubauen

            //Schritt 1: Bild mit Vorgabe-Environmap rendern. Z.B. mit 420*181 Pixeln
            //           Im erzeugten Bild sehe ich dass die Sonne beim  qwantani_1k.hdr-Bild bei Pixel (252,62) liegt
            if (useHdrLight)
            {
                graphic.AddSphere(1, 10, 10, new ObjectPropertys()
                {
                    TextureFile = DataDirectory + "qwantani_1k.hdr",
                    //TextureFile = DataDirectory + "MySky.hdr",
                    //TextureFile = DataDirectory + "wide_street_01_1k.hdr",
                    RaytracingLightSource = new EnvironmentLightDescription() { Emission = 1, Rotate = 0 }
                });
                graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, 0, 0), new Vector3D(0, 0, -1), 360);
                return;
            }            

            //Schritt 2: Im Bild qwantani_1k.hdr liegt die Sonnenmitte bei Pixel (252,62). 
            //           Setze die Pixelsonnenmitte und die Bildgröße unten in die theta/phi-Formel ein
            float sizeFactor = 1;
            var defaultSky = new DescriptionForSkyMedia();
            DescriptionForSkyMedia skyMedia = new DescriptionForSkyMedia()
            {
                EarthRadius = defaultSky.EarthRadius * sizeFactor,
                AtmosphereRadius = defaultSky.AtmosphereRadius * sizeFactor,
                RayleighScaleHeight = defaultSky.RayleighScaleHeight * sizeFactor,
                MieScaleHeight = defaultSky.MieScaleHeight * sizeFactor,
                RayleighScatteringCoeffizientOnSeaLevel = defaultSky.RayleighScatteringCoeffizientOnSeaLevel / sizeFactor,
                MieScatteringCoeffizientOnSeaLevel = defaultSky.MieScatteringCoeffizientOnSeaLevel / sizeFactor,
            };
            graphic.AddSphere(skyMedia.EarthRadius, 10, 10, new ObjectPropertys() { TextureFile = "#004400" });
            graphic.AddSphere(skyMedia.AtmosphereRadius, 10, 10, new ObjectPropertys() { TextureFile = "#FFFFFF", MediaDescription = skyMedia, RefractionIndex = 1, ShowFromTwoSides = true });
            float theta = 62 / 181f * 180;
            float phi = 252 / 420f * 360;
            Vector3D spin = new Vector3D(0, -phi, theta);
            graphic.AddSquareXZ(1, 1, 1, new ObjectPropertys() { Orientation = spin, RaytracingLightSource = new FarAwayDirectionLightDescription() { Emission = 20 } });
            graphic.GlobalSettings.Camera = new Camera(new Vector3D(0, defaultSky.EarthRadius + 30, 0) * sizeFactor, new Vector3D(0, 0, -1), 360);

            //Schritt 3: Himmel rendern und rawImageData.dat erzeugen. Mit ToneMappingTest als MySky.hdr speichern
            //Mein Himmel scheint ein zu hohen Grünanteil zu haben. Nächste Aufgabe wäre es sich das neue Sky-Model sich anzusehen 
            //und zu testen. Siehe: D:\C#\Forschungen\ParticipatingMedia_2019\Sky\An Analytic Model for Full Spectral Sky-Dome Radiance 2012

            graphic.Mode = Mode3D.ThinMediaSingleScatteringBiased;
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.None;
            graphic.GlobalSettings.SamplingCount = 1;
        }

        //Microfacet-KugelBox
        public static void AddTestszene27_MirrorsEdge(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "27_MirrorsEdge.obj", true, new ObjectPropertys() { TileDiffuseFactor = 0.9f, Albedo = 0.6f });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("KameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("KameraEnd").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraEnd").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("KameraStart").Id);

            //Wegen Fehlern beim Blender-Exporter muss ich diese Texturen hier neu zuweisen
            graphic.GetObjectByNameStartsWith("Tur1").TextureFile = "#CC2105";
            graphic.GetObjectByNameStartsWith("Kuelkasten").TextureFile = DataDirectory + "brown_mud_leaves_01_diff_1k.jpg";
            graphic.GetObjectByNameStartsWith("Haus2").TextureFile = DataDirectory + "Kacheln1.png";
            graphic.GetObjectByNameStartsWith("Stack1").TextureFile = DataDirectory + "DSCF1036.jpg";
            graphic.GetObjectByNameStartsWith("Stack2").TextureFile = "#CC2105"; ;
            graphic.GetObjectByNameStartsWith("Stack3").TextureFile = DataDirectory + "9164.jpg_wh860.jpg";
            graphic.GetObjectByNameStartsWith("Stack4").TextureFile = DataDirectory + "DSCF1036.jpg";
            graphic.GetObjectByNameStartsWith("Brett1").TextureFile = DataDirectory + "holz_dunkel.jpg";
            graphic.GetObjectByNameStartsWith("Haus2Wand1").TextureFile = DataDirectory + "Fenster.png";
            graphic.GetObjectByName("Haus2").TextureFile = DataDirectory + "Kacheln1.png";
            graphic.GetObjectByName("Kette").TextureFile = "#FFFF00";

            foreach (var obj in graphic.GetObjectsByNameContainsSearch("Metall"))
            {
                obj.BrdfModel = BrdfModel.Tile;
                obj.TileDiffuseFactor = 0.2f;
                obj.Albedo = 1.0f;
            }

            graphic.GetObjectsByNameContainsSearch("Rohr").ToList().ForEach(x => x.BrdfModel = BrdfModel.Tile);
            graphic.GetObjectByNameStartsWith("Gitter1").BrdfModel = BrdfModel.Tile;
            foreach (var obj in graphic.GetObjectsByNameContainsSearch("Glas"))
            {
                obj.BrdfModel = BrdfModel.Tile;
                obj.TileDiffuseFactor = 0.2f;
            }

            graphic.GetObjectByNameStartsWith("Scheibe1").NormalInterpolation = InterpolationMode.Smooth;

            graphic.GetObjectByNameStartsWith("Scheibe1").BrdfModel = BrdfModel.Tile;
            graphic.GetObjectByNameStartsWith("Scheibe1").TileDiffuseFactor = 0.6f;

            graphic.GetObjectByNameStartsWith("Tank1").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("Metall5").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("Metall6").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("Leiter1").NormalInterpolation = InterpolationMode.Smooth;
            graphic.GetObjectByNameStartsWith("Haus5").NormalSource = new NormalFromParallax() { ParallaxMap = DataDirectory + "Fenster5_bumpmap.bmp", TexturHeightFactor = 0.1f };

            float smokeRadius1 = graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("RauchCylinder1").Id).XSize / graphic.GetBoundingBoxFromObject(graphic.GetObjectByNameStartsWith("Rauch1").Id).XSize;
            graphic.GetObjectByNameStartsWith("Rauch1").RefractionIndex = 1;
            graphic.GetObjectByNameStartsWith("Rauch1").MediaDescription = new DescriptionForRisingSmokeMedia()
            {
                RandomSeed = 0,
                ScatteringCoeffizent = new Vector3D(1, 1, 1) * 20,
                AbsorbationCoeffizent = new Vector3D(1, 1, 1) * 4,
                AnisotropyCoeffizient = 0.30f,
                MinRadius = smokeRadius1,
                MaxRadius = smokeRadius1 * 8,
                Turbulence = 4,
                WindDirection = new Vector2D(-2, 0),
            };

            foreach (var obj in graphic.GetObjectsByNameContainsSearch("Stange"))
            {
                obj.NormalInterpolation = InterpolationMode.Smooth;
                obj.BrdfModel = BrdfModel.PlasticDiffuse;
                obj.Albedo = 0.8f;
            }
            graphic.GetObjectByNameStartsWith("Stange1").BrdfModel = BrdfModel.Diffus;

            var sceneBox = graphic.GetBoundingBoxFromAllObjects();

            int backgroundLight = graphic.AddSphere(1, 10, 10, new ObjectPropertys()
            {
                TextureFile = DataDirectory + "qwantani_1k.hdr", //Mittags ohne Wolken https://hdrihaven.com/hdri/?c=outdoor&h=qwantani (Sonnenstand mittelhoch und wolkenloser Himmel) 
                //TextureFile = DataDirectory + "MySky.hdr", //Dämmerung mit starken Grünstich
                //TextureFile = DataDirectory + "lakes_1k.hdr", //Morgens https://hdrihaven.com/hdri/?c=outdoor&h=lakes       (Sonnenstand ganz tief und gelb) 
                //TextureFile = DataDirectory + "kloofendal_48d_partly_cloudy_1k.hdr", //Mittags teilweise bewölkt https://hdrihaven.com/hdri/?c=outdoor&h=kloofendal_48d_partly_cloudy (Mittagssonne mit Wolken)
                RaytracingLightSource = new EnvironmentLightDescription() { Emission = 1, Rotate = 0.5f }
            });

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 55);
            graphic.Mode = Mode3D.ThinMediaMultipleScattering; //Brauch ich für die Rauchwolkte. Sonst würde auch BidirectionalPathTracing reichen

            graphic.GlobalSettings.Tonemapping = TonemappingMethod.ACESFilmicToneMappingCurve;
            graphic.GlobalSettings.SamplingCount = 10000;
        }

        public static void AddTestszene32_LivingRoom(GraphicPanel3D graphic)
        {
            graphic.RemoveAllObjekts();
            graphic.AddWaveFrontFileAndSplit(DataDirectory + "32_LivingRoom.obj", true, new ObjectPropertys() { TileDiffuseFactor = 0.9f, Albedo = 0.5f });

            Vector3D cameraStart = graphic.GetObjectByNameStartsWith("CameraStart").Position;
            Vector3D cameraDirection = Vector3D.Normalize(graphic.GetObjectByNameStartsWith("CameraEnd").Position - cameraStart);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("CameraEnd").Id);
            graphic.RemoveObjekt(graphic.GetObjectByNameStartsWith("CameraStart").Id);

            graphic.GetObjectByNameStartsWith("Cube.027").TextureFile = DataDirectory + "Fenster3.png"; //Figur
            graphic.GetObjectByNameStartsWith("Cube.015").TextureFile = DataDirectory + "Envwall.bmp";//Tisch
            graphic.GetObjectByNameStartsWith("Cube.028").TextureFile = DataDirectory + "wellensittiche.JPG"; //Großes Buch
            graphic.GetObjectByNameStartsWith("Cube.029").TextureFile = DataDirectory + "Weltkarte.png"; //Kleines Buch
            graphic.GetObjectByNameStartsWith("Cube.025").TextureFile = DataDirectory + "stoff_hell.png"; //Teppich
            graphic.GetObjectByNameStartsWith("Cube.016").TextureFile = DataDirectory + "toy_box_diffuse.png"; //Tischbeine
            graphic.GetObjectByNameStartsWith("Ball").TextureFile = DataDirectory + "Wabe.png"; //Ball
            foreach (var obj in graphic.GetObjectsByNameContainsSearch("RegalA")) obj.TextureFile = DataDirectory + "holz_dunkel1.jpg";
            foreach (var obj in graphic.GetObjectsByNameContainsSearch("RegalB")) obj.TextureFile = DataDirectory + "holz_mittel.jpg";
            foreach (var obj in graphic.GetObjectsByNameContainsSearch("Metall"))
            {
                obj.BrdfModel = BrdfModel.Phong;
                obj.TextureFile = "#FFFFFF";
                obj.GlossyPowExponent = 400;
            }
            foreach (var obj in graphic.GetObjectsByNameContainsSearch("Bottle"))
            {
                obj.RefractionIndex = 1.5f;
                obj.BrdfModel = BrdfModel.TextureGlass;
            }
            foreach (var obj in graphic.GetObjectsByNameContainsSearch("RegalC"))
            {
                obj.BrdfModel = BrdfModel.PlasticDiffuse;
                obj.SpecularAlbedo = 0.8f;
                obj.SpecularHighlightPowExponent = 30;
                obj.SpecularHighlightCutoff1 = 10;
                obj.SpecularHighlightCutoff2 = 10;
            }

            int lightType = 0;
            switch (lightType)
            {
                case 0:
                    {
                        graphic.AddSphere(1, 10, 10, new ObjectPropertys()
                        {
                            TextureFile = DataDirectory + "qwantani_1k.hdr", //Mittags ohne Wolken https://hdrihaven.com/hdri/?c=outdoor&h=qwantani (Sonnenstand mittelhoch und wolkenloser Himmel)
                            RaytracingLightSource = new EnvironmentLightDescription() { Emission = 1, Rotate = 0.4f }
                        });
                        graphic.GlobalSettings.BrightnessFactor = 10;
                        break;
                    }
                case 1:
                    {
                        graphic.AddSphere(1, 10, 10, new ObjectPropertys()
                        {
                            TextureFile = DataDirectory + "rustig_koppie_puresky_1k.hdr", //Mittags ohne Wolken ganz hell https://polyhaven.com/a/rustig_koppie_puresky       (Sonnenstand ganz tief und gelb)
                            RaytracingLightSource = new EnvironmentLightDescription() { Emission = 1, Rotate = 0.41f }
                        });
                        graphic.GlobalSettings.BrightnessFactor = 4;
                        break;
                    }
            }

            graphic.GlobalSettings.Camera = new Camera(cameraStart, cameraDirection, 55);

            graphic.GlobalSettings.PhotonCount = 10000;
            graphic.GlobalSettings.Tonemapping = TonemappingMethod.ACESFilmicToneMappingCurve;
            graphic.Mode = Mode3D.VertexConnectionMerging;
        }
    }
}
