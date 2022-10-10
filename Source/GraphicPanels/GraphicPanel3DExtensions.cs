using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TextureHelper.TexturMapping;

namespace GraphicPanels
{
    //Hier kommen all die Hilfsfunktionen rein, um meine Material-Testzene zu erstellen
    public static class GraphicPanel3DExtensions
    {
        //Löscht all angegebenen Objekte und platziert stattdessen eine Wolke, die so aussieht wie die Objekte (Darstellung am Besten mit ThinMediaMultipleScattering)
        //absorbationFactor = 1 => Wolke ist weiß
        //                    10=> Wolke ist grau
        public static int TransformObjectsToCloud(this GraphicPanel3D graphic, IEnumerable<int> ids, float absorbationFactor, float turbulenceFactor)
        {
            //Wolke 1 aus SkyWithClouds-Szene Tagsüber
            //RadiusInTheBox: 582.670837; RadiusOutTheBox: 1044.03516; XSize: 1225.31262; YSize: 1225; ZSize: 1165.34167
            //RefractionIndex = 1; ScatteringCoeffizent: {new Vector3D(0.00999999978f, 0.00999999978f, 0.00999999978f)}; AbsorbationCoeffizent: {new Vector3D(0.00200000009f, 0.00200000009f, 0.00200000009f)}; AnisotropyCoeffizient: 0.3

            var legoGrid = graphic.GetLegoGrid(ids, 60); //60
            float sf = 1225 / legoGrid.Box.MaxEdge;
            var gridCloud = new DescriptionForGridCloudMedia()
            {
                LegoGrid = legoGrid,
                ScatteringCoeffizent = new Vector3D(0.00999999978f, 0.00999999978f, 0.00999999978f) * sf,
                AbsorbationCoeffizent = new Vector3D(0.00200000009f, 0.00200000009f, 0.00200000009f) * sf * absorbationFactor,
                AnisotropyCoeffizient = 0.30f,
                StepCountForAttenuationIntegration = 5
            };
            int cloudCube = graphic.AddCube(legoGrid.Box, new ObjectPropertys() { Size = turbulenceFactor, MediaDescription = gridCloud, RefractionIndex = 1 });
            
            foreach (var id in ids) graphic.RemoveObjekt(id);

            return cloudCube;
        }

        //Anleitung um Lego-Objekt zu erzeugen:
        //Schritt 1: Das Objekt aus der Szene durch Lego-Transform ersetzen:
        //  graphic.TransformObjectsToLego(graphic.GetObjectsByNameContainsSearch("Lego_").Select(x => x.Id));
        //Schritt 2: Szene Exportieren: File.WriteAllText("Export.obj", graphic.ExportToWavefront());
        //Schritt 3: Im Blender im Edit-Mode alle Vertize markieren W->Remove Doubles;dann Decimate-Modifiert
        public static int TransformObjectsToLego(this GraphicPanel3D graphic, IEnumerable<int> ids, int separations = 60)
        {
            int legoNew = graphic.CreateLegoObject(ids, separations);
            foreach (var l in ids) graphic.RemoveObjekt(l);
            return legoNew;
        }

        public static void SetDiffuseFlat(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.Diffus;
                obj.NormalInterpolation = InterpolationMode.Flat;
            }
        }

        public static void SetDiffuseSmooth(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.Diffus;
                obj.NormalInterpolation = InterpolationMode.Smooth;
            }
        }

        //Plastik1 (Diffuse + Glanzpunkt)
        public static void SetPlastic1(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.PlasticDiffuse;
                obj.SpecularAlbedo = 0.8f;
                obj.Albedo = 0.2f;
                obj.SpecularHighlightPowExponent = 30;
                obj.SpecularHighlightCutoff1 = 10;
                obj.SpecularHighlightCutoff2 = 10;
            }
        }

        //Plastik2 (Diffuse + Glanzpunkt + Mirror)
        public static void SetPlastic2(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.FresnelTile;
                obj.Albedo = 0.2f;
                obj.SpecularAlbedo = 0.6f;
                obj.RefractionIndex = 1.3f;
            }
        }

        //Spiegel glatt (Reflektion ist farblich verändert)
        public static void SetCleanMirror(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.Mirror;

                if (obj.Color.Type == ColorSource.ColorString)
                {
                    obj.TextureFile = PixelHelper.VectorToHexColor(ColorSourceToVector(obj.Color) / 2 + new Vector3D(1, 1, 1) / 2);
                }                
            }
        }

        //Spiegel mit Rost
        //Hinweis um eine Rosttextur zu erstellen:
        //BitmapHelp.TransformWhiteColorToWhitestWhit(new Bitmap(DataDirectory + "istockphoto-827289002-1024x1024.jpg"), 2.1f).Save(DataDirectory + "istockphoto-827289002-1024x1024_.jpg");
        public static void SetRustMirror(this IEnumerable<ObjectPropertys> objects, string rustTexture)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.MirrorWithRust;
  
                obj.MirrorColor = new Vector3D(1, 1, 1);
                obj.TextureFile = rustTexture;
            }
        }

        //Fliese (Reflektion ist weiß)
        public static void SetTile(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.Tile;
                obj.TileDiffuseFactor = 0.4f;
                obj.SpecularAlbedo = 0.8f;
            }
        }

        //Texture im RepeatMode
        public static void SetRepeatTexture(this IEnumerable<ObjectPropertys> objects, string texture)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.Diffus;
                obj.TextureFile = texture;
            }
        }

        //Kaffee-Modus
        public static void SetCoffeeMode(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.Diffus;
                obj.Color = new ColorFromProcedure() { ColorProceduralFunction = ColorProceduralFunction.Wood };
            }
        }

        //ProceduralMirror
        public static void SetProceduralMirror(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.Mirror;
                obj.NormalSource = new NormalFromProcedure() { Function = new NormalProceduralFunctionTent() };
                obj.SpecularAlbedo = 0.8f;
                obj.TextureFile = PixelHelper.VectorToHexColor(ColorSourceToVector(obj.Color) / 2 + new Vector3D(1, 1, 1) / 2);
            }
        }

        //Unebenes Kupfer
        public static void SetCopper(this IEnumerable<ObjectPropertys> objects, string normalMap)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.Mirror;
                obj.SpecularAlbedo = 1.0f;
                obj.NormalSource = new NormalFromMap() { NormalMap = normalMap };
                if (obj.Color.Type == ColorSource.ColorString)
                    obj.TextureFile = PixelHelper.VectorToHexColor(ColorSourceToVector(obj.Color) / 2 + new Vector3D(1, 1, 1) / 2);
            }
        }

        //BlackIsTransparent
        public static void SetBlackIsTransparent(this IEnumerable<ObjectPropertys> objects, string blackMap)
        {
            foreach (var obj in objects)
            {
                obj.TextureFile = blackMap;
                obj.BlackIsTransparent = true;

            }
        }

        //Glas ohne Media
        public static void SetNoMediaGlas(this IEnumerable<ObjectPropertys> objects)
        {
            //So erstellt man eine helle Textur
            //Bitmap scaledImage = BitmapHelp.AddToColor(BitmapHelp.ScaleColor(new Bitmap(id.TextureFile), new Vector3D(1, 1, 1) * mf), new Vector3D(1, 1, 1) * (1 - mf));
            //scaledImage.Save(DataDirectory + "ScaledImage.bmp");
            //scaledImage.Dispose();

            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.TextureGlass;
                obj.NormalInterpolation = InterpolationMode.Flat;
                obj.SpecularAlbedo = 1.0f;
                obj.RefractionIndex = 1.45f;
                float mf = 0.1f;
                if (obj.Color.Type == ColorSource.ColorString)
                {
                    obj.TextureFile = PixelHelper.VectorToHexColor(ColorSourceToVector(obj.Color) * mf + new Vector3D(1, 1, 1) * (1 - mf));
                }

            }
        }

        //Glas ohne Media mit Normalmmap
        public static void SetNormalmappedGlas(this IEnumerable<ObjectPropertys> objects, string normalMap)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.TextureGlass;
                obj.GlasIsSingleLayer = true;
                obj.SpecularAlbedo = 0.9f;
                obj.NormalSource = new NormalFromMap() { NormalMap = normalMap, TextureMatrix = Matrix3x3.Scale(0.5f, 0.5f) };
                obj.RefractionIndex = 1.45f;
                float mf = 0.2f;
                if (obj.Color.Type == ColorSource.ColorString)
                    obj.TextureFile = PixelHelper.VectorToHexColor(ColorSourceToVector(obj.Color) * mf + new Vector3D(1, 1, 1) * (1 - mf));

            }
        }

        //Microfacet Glas
        public static void SetMicrofacetGlas(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.HeizGlass;
                obj.NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.03f, 0.03f) };
                obj.NormalInterpolation = InterpolationMode.Flat;
                obj.RefractionIndex = 1.45f;
                obj.SpecularAlbedo = 1.0f;
                float mf = 0.1f;
                if (obj.Color.Type == ColorSource.ColorString)
                    obj.TextureFile = PixelHelper.VectorToHexColor(ColorSourceToVector(obj.Color) * mf + new Vector3D(1, 1, 1) * (1 - mf));

            }
        }

        //Microfacet Glas mit Roughnesmap
        public static void SetMicrofacetGlas(this IEnumerable<ObjectPropertys> objects, string roughnesmap)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.HeizGlass;
                obj.NormalInterpolation = InterpolationMode.Flat;
                obj.RefractionIndex = 1.45f;
                obj.SpecularAlbedo = 1.0f;
                obj.NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.03f, 0.03f), RoughnessMap = roughnesmap, TextureMatrix = Matrix3x3.Scale(2, 2) };
                float mf = 0.1f;
                obj.TextureFile = PixelHelper.VectorToHexColor(ColorSourceToVector(obj.Color) * mf + new Vector3D(1, 1, 1) * (1 - mf));

            }
        }

        //Raues Metall
        public static void SetRoughMetall(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.HeizMetal;
                obj.NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.01f, 0.01f) };
                obj.SpecularAlbedo = 1.0f;

                float mf = 0.4f;
                if (obj.Color.Type == ColorSource.ColorString)
                {
                    obj.TextureFile = PixelHelper.VectorToHexColor(ColorSourceToVector(obj.Color) * mf + new Vector3D(1, 1, 1) * (1 - mf));
                }                
            }
        }

        //Anisotrophes Metal
        public static void SetAnisotrophicMetall(this GraphicPanel3D graphic, IEnumerable<ObjectPropertys> objects, string roughnessMap)
        {
            var box1 = graphic.GetBoundingBoxFromObjects(objects.Select(x => x.Id));

            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.HeizMetal;
                obj.NormalSource = new NormalFromMicrofacet() { MicrofacetRoughness = new Vector2D(0.03f, 0.01f), RoughnessMap = roughnessMap };
                obj.TextureCoordSource = new ProceduralTextureCoordSource() { TextureCoordsProceduralFunction = new CylinderMapping(new Ray(box1.Center, new Vector3D(0, 0, -1)), box1.YSize / 2) };

                float mf = 0.4f;
                obj.TextureFile = PixelHelper.VectorToHexColor(ColorSourceToVector(obj.Color) * mf + new Vector3D(1, 1, 1) * (1 - mf));
            }
        }

        public static void CreateRoughnessMap(this GraphicPanel3D graphic, string saveFileName)
        {
            Bitmap roughnessMap = new Bitmap(500, 1);
            for (int i = 0; i < roughnessMap.Width; i++)
            {
                float frequence = 10;
                double win = i / (float)roughnessMap.Width * 2 * Math.PI * frequence;
                double f = (Math.Sin(win) + 1) / 2;
                double value = f * 0.01 + (1 - f) * 0.1;
                int c = (int)(value * 10 * 255);
                roughnessMap.SetPixel(i, 0, Color.FromArgb(c, c, c));
            }
            roughnessMap.Save(saveFileName);
            roughnessMap.Dispose();
        }

        //Parallax-Mapping
        public static void SetParallaxMapping(this IEnumerable<ObjectPropertys> objects, string parallaxMap)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.PlasticMetal;
                obj.SpecularAlbedo = 0.8f;
                obj.SpecularHighlightPowExponent = 3;
                obj.SpecularHighlightCutoff1 = 5;
                obj.SpecularHighlightCutoff2 = 5;
                obj.MirrorColor = new Vector3D(1,1,1) * 0.1f;
                obj.NormalSource = new NormalFromParallax() { ParallaxMap = parallaxMap, TextureMatrix = Matrix3x3.Scale(10, 10), IsParallaxEdgeCutoffEnabled = obj.Color.Type == ColorSource.ColorString, TexturHeightFactor = 0.4f };
            }
        }

        //Glas mit Media
        public static void SetGlasWithMedia(this IEnumerable<ObjectPropertys> objects)
        {
            foreach (var obj in objects)
            {
                obj.BrdfModel = BrdfModel.MirrorGlass;
                obj.RefractionIndex = 1.45f;
                obj.MirrorColor = ColorSourceToVector(obj.Color);
                Vector3D absorbationCoeffizient = (new Vector3D(1, 1, 1) - ColorSourceToVector(obj.Color));
                obj.MediaDescription = new DescriptionForHomogeneousMedia()
                {
                    ScatteringCoeffizent = new Vector3D(1.5f, 1.5f, 1.5f) * 5,
                    AbsorbationCoeffizent = absorbationCoeffizient * 5,
                    AnisotropyCoeffizient = 0.8f,
                };
            }
        }

        //Wachs
        //float mediaFactor = 1; //0..2 = Wie dicht ist das Medium
        //float glasFactor = 0.1f; //0..1 = Wie diffuse ist der Rand. 0 = Medium mehr wie diffuse Wolke; 1 = Medium wie Glas
        public static void SetWax(this GraphicPanel3D graphic, IEnumerable<ObjectPropertys> objects, float mediaFactor, float glasFactor)
        {
            var legoBox = graphic.GetBoundingBoxFromObjects(objects.Select(x => x.Id));
            foreach (var obj in objects)
            {
                //Stillife-Kerze:
                //RadiusInTheBox: 9.79995, RadiusOutTheBox: 17.7948952, XSize: 20.3517017, YSize: 21.6399, ZSize: 19.5999
                //BrdfModel: MirrorGlas, MirrorColor: { new Vector3D(0.600000024f, 0.600000024f, 0.600000024f)}, RefractionIndex: 1.446, TextureFile: "#999999", 
                //AbsorbationCoeffizent: { new Vector3D(0.0299999993f, 0.100000001f, 0.200000003f)}, AnisotropyCoeffizient: 0.8, ScatteringCoeffizent: { new Vector3D(1.5f, 1.5f, 1.5f)}

                float sf = 21.6399f / legoBox.MaxEdge;
                obj.BrdfModel = BrdfModel.MirrorGlass;
                obj.RefractionIndex = 1 + 0.446f * glasFactor;
                Vector3D absorbationCoeffizient = (new Vector3D(1, 1, 1) - ColorSourceToVector(obj.Color));
                obj.MirrorColor = new Vector3D(0.600000024f, 0.600000024f, 0.600000024f) * glasFactor;
                obj.TextureFile = "#999999";
                obj.MediaDescription = new DescriptionForHomogeneousMedia()
                {
                    ScatteringCoeffizent = new Vector3D(1.5f, 1.5f, 1.5f) * sf * mediaFactor,
                    AbsorbationCoeffizent = absorbationCoeffizient * sf * mediaFactor,
                    AnisotropyCoeffizient = 0.8f * glasFactor,
                };
            }
        }

        private static Vector3D ColorSourceToVector(IColorSource color)
        {
            if (color.Type == ColorSource.Texture ) return PixelHelper.ColorToVector(new Bitmap(color.As<ColorFromTexture>().TextureFile).GetPixel(0, 0));
            return color.As<ColorFromRgb>().Rgb;            
        }
    }
}
