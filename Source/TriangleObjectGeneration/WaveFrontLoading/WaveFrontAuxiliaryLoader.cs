using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleObjectGeneration
{
    //SmallUPBP hat die Aux-Dateien 'erfunden', um Kamera-Position + Media-Eigenschaften (und alles, was es in wavefront-mtl nicht gibt) beschreiben zu können
    public static class WaveFrontAuxiliaryLoader
    {
        public static WaveFrontAuxiliaryFile ReadFile(string fileName)
        {
            string line;

            CameraData cameraData = new CameraData();
            WaveFrontAuxiliaryBackgroundLight backgroundLight = null;
            WaveFrontAuxiliaryMedium medium = null;
            WaveFrontAuxiliaryMaterial material = null;

            List<WaveFrontAuxiliaryMedium> medias = new List<WaveFrontAuxiliaryMedium>();
            List<WaveFrontAuxiliaryMaterial> materials = new List<WaveFrontAuxiliaryMaterial>();

            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            while ((line = file.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    if (medium != null)
                    {
                        medias.Add(medium);
                        medium = null;
                    }
                    if (material != null)
                    {
                        materials.Add(material);
                        material = null;
                    }

                    continue;
                }

                int space = line.IndexOf(' ');
                if (space != -1)
                {
                    string command = line.Substring(0, space);
                    string arguments = line.Substring(space + 1).Replace('\t', ' ');

                    switch (command)
                    {
                        //Camera
                        case "TM_ROW0":
                            cameraData.TM_ROW0 = ParseVector(arguments);
                            break;
                        case "TM_ROW1":
                            cameraData.TM_ROW1 = ParseVector(arguments);
                            break;
                        case "TM_ROW2":
                            cameraData.TM_ROW2 = ParseVector(arguments);
                            break;
                        case "TM_ROW3":
                            cameraData.TM_ROW3 = ParseVector(arguments);
                            break;
                        case "CAMERA_FOV":
                            cameraData.CAMERA_FOV = float.Parse(arguments.Replace('.', ','));
                            break;
                        case "CAMERA_TDIST":
                            cameraData.CAMERA_TDIST = float.Parse(arguments.Replace('.', ','));
                            break;

                        //Medium
                        case "medium":
                            if (medium != null)
                            {
                                medias.Add(medium);
                                medium = null;
                            }
                            if (medium == null) medium = new WaveFrontAuxiliaryMedium();
                            medium.Name = arguments;
                            break;
                        case "absorption":
                            medium.Absorption = ParseVector(arguments);
                            break;
                        case "emission":
                            medium.Emission = ParseVector(arguments);
                            break;
                        case "scattering":
                            medium.Scattering = ParseVector(arguments);
                            break;
                        case "g":
                            medium.G = float.Parse(arguments.Replace('.', ','));
                            break;

                        //Material
                        case "material":
                            if (material != null)
                            {
                                materials.Add(material);
                                material = null;
                            }
                            if (material == null) material = new WaveFrontAuxiliaryMaterial();
                            material.Name = arguments;
                            break;
                        case "mirror":
                            material.Mirror = ParseVector(arguments);
                            break;
                        case "ior":
                            material.Ior = float.Parse(arguments.Replace('.', ','));
                            break;
                        case "Ke":
                            material.Emission = ParseVector(arguments);
                            break;
                        case "mediumId":
                            material.Medium = medias.First(x => x.Name == arguments);
                            break;
                        case "priority":
                            material.Priority = int.Parse(arguments);
                            break;
                        case "geometryType":
                            switch (arguments)
                            {
                                case "imaginary":
                                    material.GeometryType = GeometryType.Imaginary;
                                    break;
                                case "real":
                                    material.GeometryType = GeometryType.Real;
                                    break;
                                default:
                                    throw new Exception($"Unknown GeometryType: '{arguments}'");
                            }
                            break;

                        //Backgroundlight
                        case "light_background_em":
                            {
                                var fields = arguments.Split(' ');
                                backgroundLight = new WaveFrontAuxiliaryBackgroundLight()
                                {
                                    Emission = float.Parse(fields[0].Replace('.', ',')),
                                    Rotate = float.Parse(fields[1].Replace('.', ',')),
                                    ImageFileName = fields[2]
                                };
                            }                           
                            break;
                    }
                }
            }

            file.Close();

            if (medium != null) medias.Add(medium);
            if (material != null) materials.Add(material);

            return new WaveFrontAuxiliaryFile()
            {
                Camera = new Camera(cameraData.TM_ROW3, Vector3D.Normalize(-cameraData.TM_ROW2), Vector3D.Normalize(cameraData.TM_ROW1), cameraData.CAMERA_FOV * 180 / (float)Math.PI),
                Materials = materials.ToArray(),
                BackgroundLight = backgroundLight
            };
        }

        private static Vector3D ParseVector(string text)
        {
            var fields = text.Split(' ');
            return new Vector3D(float.Parse(fields[0].Replace('.', ',')),
                                   float.Parse(fields[1].Replace('.', ',')),
                                   float.Parse(fields[2].Replace('.', ',')));
        }

        class CameraData
        {
            public Vector3D TM_ROW0 { get; set; }
            public Vector3D TM_ROW1 { get; set; }
            public Vector3D TM_ROW2 { get; set; }
            public Vector3D TM_ROW3 { get; set; }
            public float CAMERA_FOV { get; set; }
            public float CAMERA_TDIST { get; set; }

        }
    }

    public class WaveFrontAuxiliaryFile
    {
        public Camera Camera;
        public WaveFrontAuxiliaryMaterial[] Materials;
        public WaveFrontAuxiliaryBackgroundLight BackgroundLight;

        public bool ContainsMaterial(string materialName)
        {
            return this.Materials.Any(x => x.Name == materialName);
        }

        public WaveFrontAuxiliaryMaterial GetMaterialByName(string materialName)
        {
            return this.Materials.First(x => x.Name == materialName);
        }
    }

    public class WaveFrontAuxiliaryMaterial
    {
        public string Name { get; set; }
        public Vector3D Mirror { get; set; } = new Vector3D(0, 0, 0); //Spiegelfarbe
        public Vector3D Emission { get; set; } //Diffuse Lichtquelle
        public float Ior { get; set; } = float.NaN; //Brechungsindex
        public WaveFrontAuxiliaryMedium Medium { get; set; }
        public int Priority { get; set; }
        public GeometryType GeometryType { get; set; } = GeometryType.NotSet;
}

    public enum GeometryType
    {
        NotSet,
        Real,           //Der Rand vom ParticipatingMedia hat ein Brechungsindex
        Imaginary       //Der Rand bricht nicht den Strahl (Luftwürfel)
    }

    public class WaveFrontAuxiliaryMedium
    {
        public string Name { get; set; }
        public Vector3D Absorption { get; set; }
        public Vector3D Emission { get; set; }
        public Vector3D Scattering { get; set; }
        public float G { get; set; } //AnisotropyCoeffizient von der Phasenfunktion
    }

    public class WaveFrontAuxiliaryBackgroundLight
    {
        public string ImageFileName { get; set; }
        public float Emission { get; set; }
        public float Rotate { get; set; }
    }
}
