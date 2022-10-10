using System.Linq;

namespace GraphicMinimal
{
    public class BlobPropertys
    {
        public Vector3D[] CenterList { get; set; }
        public float SphereRadius { get; set; }

        public override string ToString()
        {
            return string.Join("|", CenterList.Select(x => x)) + "#" + SphereRadius;
        }

        public static BlobPropertys Parse(string value)
        {
            var split1 = value.Split('#');

            BlobPropertys obj = new BlobPropertys();

            obj.CenterList = split1[0].Split('|').Select(x => Vector3D.Parse(x)).ToArray();
            obj.SphereRadius = float.Parse(split1[1]);

            return obj;
        }
    }
}
