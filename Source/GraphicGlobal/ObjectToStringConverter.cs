namespace GraphicGlobal
{
    public static class ObjectToStringConverter
    {
        //String mit komischen Zeichen drin
        /*public static string ConvertObjectToString(object data)
        {
            MemoryStream str = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(str, data);
            str.Close();
            return Encoding.Unicode.GetString(str.GetBuffer());
        }

        public static T ConvertStringToObject<T>(string data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream(Encoding.Unicode.GetBytes(data));
            return (T)formatter.Deserialize(stream);
        }*/

        //Schöner String
        public static string ConvertObjectToString(object data)
        {
            //BinaryFormatter funktioniert nur unter WinForm aber nicht unter WPF
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    new BinaryFormatter().Serialize(ms, data);
            //    return Convert.ToBase64String(ms.ToArray());
            //}

            return JsonConverterNoPublicFields.Serialize(data);
        }

        public static T ConvertStringToObject<T>(string base64String)
        {
            //byte[] bytes = Convert.FromBase64String(base64String);
            //using (MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length))
            //{
            //    ms.Write(bytes, 0, bytes.Length);
            //    ms.Position = 0;
            //    return (T)new BinaryFormatter().Deserialize(ms);
            //}

            return JsonConverterNoPublicFields.Deserialize<T>(base64String);
        }
    }
}
