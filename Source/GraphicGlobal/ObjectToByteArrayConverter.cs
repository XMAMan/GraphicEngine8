using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GraphicGlobal
{
    public static class ObjectToByteArrayConverter
    {
        public static byte[] ConvertObjectToByteArray(object data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, data);
                return ms.ToArray();
            }
        }

        public static T ConvertByteArrayToObject<T>(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length))
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                return (T)new BinaryFormatter().Deserialize(ms);
            }
        }
    }
}
