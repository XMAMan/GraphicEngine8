using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;

namespace TriangleObjectGeneration
{
    //Lädt obj + mtl + aux-Datei und erzeugt damit Liste von DrawingObjects
    public static class WaveFrontLoader
    {
        //Hier werden die 3 Wavefrontloader(Obj,mtl,aux) benutzt, um daraus dann als Vereinigung DrawingObjekte zu erzeugen
        public static List<DrawingObject> LoadObjectsFromFile(string file, bool takeNormalsFromFile, ObjectPropertys objektPropertys = null)
        {
            if (objektPropertys == null) objektPropertys = new ObjectPropertys();
            var subObjekte = WaveFrontObjLoader.LoadWaveFrontFile(file, true, takeNormalsFromFile, out string materialFileName);             //Obj-Datei

            WaveFrontMaterialFile materialFile = null;
            if (File.Exists(materialFileName))
            {
                materialFile = new WaveFrontMaterialFile(materialFileName);                                     //Mtl-Datei
            }

            WaveFrontAuxiliaryFile auxiliaryFile = null;
            string auxiliaryFileName = materialFileName.Replace(".mtl",".obj") + ".aux";
            if (File.Exists(auxiliaryFileName))
            {
                auxiliaryFile = WaveFrontAuxiliaryLoader.ReadFile(auxiliaryFileName);                           //Aux-Datei
            }

            List<DrawingObject> list = new List<DrawingObject>();

            string[] colors = new string[] { "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF", "#F00000", "#00F000", "#0000F0", "#F0F000", "#F000F0", "#00F0F0" };
            for (int i = 0; i < subObjekte.Count; i++)
            {
                WaveFrontMaterial objMaterial = null;
                if (materialFile != null && materialFile.ContainsMaterial(subObjekte[i].MaterialName))
                {
                    objMaterial = materialFile.GetMaterialByName(subObjekte[i].MaterialName);
                }

                WaveFrontAuxiliaryMaterial auxMaterial = null;
                if (auxiliaryFile != null && auxiliaryFile.ContainsMaterial(subObjekte[i].MaterialName))
                {
                    auxMaterial = auxiliaryFile.GetMaterialByName(subObjekte[i].MaterialName);
                }

                var props = new ObjectPropertys(objektPropertys)
                {
                    TextureFile = colors[i % colors.Length] //Default-Color
                };
                if (objMaterial != null)       //Gibt es mtl-Datei? Wenn ja, nimm Farbe/Textur davon
                {
                    if (objMaterial.TextureFile == null)
                    {
                        props.TextureFile = PixelHelper.VectorToHexColor(objMaterial.DiffuseColor);                       
                    }
                    else
                    {
                        props.TextureFile = objMaterial.TextureFile;
                    }

                    props.GlossyColor = objMaterial.PhongColor;
                    props.GlossyPowExponent = objMaterial.PhongExponent;
                }


                if (auxMaterial != null)
                {
                    if (auxMaterial.Medium != null) //Hat es Media?
                    {
                        props.MediaDescription = new DescriptionForHomogeneousMedia()
                        {
                            AbsorbationCoeffizent = auxMaterial.Medium.Absorption,
                            ScatteringCoeffizent = auxMaterial.Medium.Scattering,
                            EmissionCoeffizient = auxMaterial.Medium.Emission,
                            AnisotropyCoeffizient = auxMaterial.Medium.G,
                            Priority = auxMaterial.Priority + 1
                        };                                            
                    }

                    props.MirrorColor = auxMaterial.Mirror;
                    props.RefractionIndex = auxMaterial.Ior;
                    if (auxMaterial.GeometryType == GeometryType.Imaginary) props.RefractionIndex = 1; //Luftwürfel

                    bool hasDiffuse = objMaterial.DiffuseColor.Max() > 0;
                    bool hasMirror = auxMaterial.Mirror.Max() > 0;
                    bool hasPhong = objMaterial.PhongColor.Max() > 0;
                    bool hasRefractionIndex = float.IsNaN(props.RefractionIndex) == false && props.RefractionIndex > 0 && props.RefractionIndex != 1;

                    if (auxMaterial.GeometryType == GeometryType.Imaginary)
                    {
                        props.BrdfModel = BrdfModel.Diffus; //Luftwürfel
                    }
                    else if (hasDiffuse && !hasMirror && !hasPhong)  //Diffuse Only
                    {
                        props.BrdfModel = BrdfModel.Diffus;
                    }else if (!hasDiffuse && hasMirror && !hasPhong && hasRefractionIndex)//Glas
                    {
                        props.BrdfModel = BrdfModel.MirrorGlass;
                    }
                    else if (!hasDiffuse && hasMirror && !hasPhong && !hasRefractionIndex) //Spiegel
                    {
                        props.TextureFile = PixelHelper.VectorToHexColor(auxMaterial.Mirror);
                        props.BrdfModel = BrdfModel.Mirror;
                    }
                    else if (hasDiffuse && hasMirror && !hasPhong && !hasRefractionIndex) //Diffuse+Spiegel
                    {
                        props.BrdfModel = BrdfModel.DiffuseAndMirror; //SmallUPBP verwendet eine Mischung aus Diffuse+Mirror wo die Brdf nicht mit der SelectionPdf im Zähler gewichtet wird (Die Selection-Pdf taucht nur in der Pdf auf. D.h. Brdf wird durch die SelectionPdf dividiert)
                    }
                    else
                    {
                        props.BrdfModel = BrdfModel.DiffusePhongGlassOrMirrorSum; //Die ultimative 4-Komponenten-Bsdf von SmallUPBP
                    }

                    if (auxMaterial.Emission != null)
                    {
                        float emission = (float)Math.Pow(10, Math.Ceiling(Math.Log10(auxMaterial.Emission.Max())));
                        props.TextureFile = PixelHelper.VectorToHexColor(auxMaterial.Emission / emission);
                        float surfaceArea = subObjekte[i].Triangles.Sum(x => x.SurfaceArea);                        
                        props.RaytracingLightSource = new DiffuseSurfaceLightDescription() { Emission = emission * surfaceArea  };
                        props.BrdfModel = BrdfModel.Diffus;
                    }
                }

                list.Add(new DrawingObject(subObjekte[i], props));
            }

            if (auxiliaryFile != null && auxiliaryFile.BackgroundLight != null)
            {
                list.Add(new DrawingObject(TriangleObjectGenerator.CreateSphere(1, 10, 10), new ObjectPropertys()
                {
                    TextureFile = auxiliaryFile.BackgroundLight.ImageFileName,
                    RaytracingLightSource = new EnvironmentLightDescription() 
                    { 
                        Emission = auxiliaryFile.BackgroundLight.Emission, 
                        Rotate = auxiliaryFile.BackgroundLight.Rotate
                    }
                }));
            }

            return list;
        }

        #region MaterialOverview
        public static string GetMaterialOverview(string objFile)
        {
            var materials = GetAllMaterials(objFile);

            return string.Join(Environment.NewLine + Environment.NewLine, materials);
        }

        class ObjMaterial
        {
            public WaveFrontMaterial MtlMaterial { get; set; }
            public WaveFrontAuxiliaryMaterial AuxMaterial { get; set; }

            public override string ToString()
            {
                StringBuilder str = new StringBuilder();
                str.AppendLine(this.MtlMaterial.Name);
                if (this.MtlMaterial.DiffuseColor != null) str.AppendLine("Diffuse-Color = " + this.MtlMaterial.DiffuseColor.ToShortString());
                if (this.MtlMaterial.TextureFile != null) str.AppendLine("TextureFile = " + this.MtlMaterial.TextureFile);
                if (this.AuxMaterial?.Mirror != null) str.AppendLine("Mirror-Color = " + this.AuxMaterial?.Mirror.ToShortString());
                if (this.MtlMaterial.PhongColor != null) str.AppendLine("PhongColor = " + this.MtlMaterial.PhongColor.ToShortString());
                if (float.IsNaN(this.MtlMaterial.PhongExponent) == false) str.AppendLine("PhongExponent = " + this.MtlMaterial.PhongExponent);
                if (this.AuxMaterial != null && float.IsNaN(this.AuxMaterial.Ior) == false) str.AppendLine("Brechungsindex = " + this.AuxMaterial.Ior);
                if (this.AuxMaterial?.Emission != null) str.AppendLine("Emission-Color = " + this.AuxMaterial?.Emission.ToShortString());
                if (this.AuxMaterial?.Medium != null)
                {
                    str.AppendLine("Media-Name = " + this.AuxMaterial.Medium.Name);
                    str.AppendLine("Media-Priority = " + this.AuxMaterial.Priority);
                    str.AppendLine("Media-Absorption = " + this.AuxMaterial.Medium.Absorption);
                    str.AppendLine("Media-Scattering = " + this.AuxMaterial.Medium.Scattering);
                    str.AppendLine("Media-Emission = " + this.AuxMaterial.Medium.Emission);
                    str.AppendLine("Media-G = " + this.AuxMaterial.Medium.G);
                    if (this.AuxMaterial.GeometryType != GeometryType.NotSet) str.AppendLine("Media-GeometryType = " + this.AuxMaterial.GeometryType);
                }

                return str.ToString();
            }
        }

        static List<ObjMaterial> GetAllMaterials(string file)
        {
            var lines = File.ReadAllLines(file);
            var materialNames = lines.Where(x => x.StartsWith("usemtl")).Select(x => x.Replace("usemtl ", "")).ToList();
            string materialFileName = file.Replace(".obj", ".mtl");

            WaveFrontMaterialFile materialFile = null;
            if (File.Exists(materialFileName))
            {
                materialFile = new WaveFrontMaterialFile(materialFileName);                                     //Mtl-Datei
            }

            WaveFrontAuxiliaryFile auxiliaryFile = null;
            string auxiliaryFileName = materialFileName.Replace(".mtl", ".obj") + ".aux";
            if (File.Exists(auxiliaryFileName))
            {
                auxiliaryFile = WaveFrontAuxiliaryLoader.ReadFile(auxiliaryFileName);                           //Aux-Datei
            }

            List<ObjMaterial> resultList = new List<ObjMaterial>();

            for (int i = 0; i < materialNames.Count; i++)
            {
                WaveFrontMaterial objMaterial = null;
                if (materialFile != null && materialFile.ContainsMaterial(materialNames[i]))
                {
                    objMaterial = materialFile.GetMaterialByName(materialNames[i]);
                }

                WaveFrontAuxiliaryMaterial auxMaterial = null;
                if (auxiliaryFile != null && auxiliaryFile.ContainsMaterial(materialNames[i]))
                {
                    auxMaterial = auxiliaryFile.GetMaterialByName(materialNames[i]);
                }

                if (resultList.Any(x => x.MtlMaterial.Name == objMaterial.Name) == false)
                {
                    resultList.Add(new ObjMaterial() { MtlMaterial = objMaterial, AuxMaterial = auxMaterial });
                }
            }

            return resultList;
        }
        #endregion
    }
}
