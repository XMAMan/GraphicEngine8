using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphicGlobal
{
    //Unter WinForm konnte man noch mit den BinaryFormatter arbeiten, aber unter WPF und UWP geht das nicht mehr. -> Siehe hier: https://learn.microsoft.com/de-de/dotnet/standard/serialization/binaryformatter-security-guide
    //Damit kann unter WPF auch die Random-Klasse serialisiert werden: https://github.com/microsoft/referencesource/blob/master/mscorlib/system/random.cs
    //Quelle: https://discussions.unity.com/t/serialization-of-random-number-generator/909482/6
    public static class JsonConverterNoPublicFields
    {
        static JsonSerializerSettings SerializerSettings;

        static JsonConverterNoPublicFields()
        {
            JsonConverterNoPublicFields.SerializerSettings = new JsonSerializerSettings { ContractResolver = new ContractResolver(), TypeNameHandling = TypeNameHandling.All };
        }

        /// <summary>
        /// Create a new random instance which is a deep copy of the instance provided.
        /// </summary>
        /// <param name="random">Object to clone.</param>
        /// <returns>New random instance.</returns>
        public static T Clone<T>(Random random)
        {
            return JsonConverterNoPublicFields.Deserialize<T>(Serialize(random));
        }

        /// <summary>
        /// Deserializes a string into a <see cref="Random" /> object.
        /// </summary>
        /// <param name="state">State to deserialize.</param>
        /// <returns>Random object.</returns>
        public static T Deserialize<T>(string state)
        {
            return JsonConvert.DeserializeObject<T>(state, JsonConverterNoPublicFields.SerializerSettings);
        }

        /// <summary>
        /// Serializes a <see cref="Object" /> object.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <returns>String representing the state of the Random object.</returns>
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, JsonConverterNoPublicFields.SerializerSettings);
        }

        class ContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                List<JsonProperty> properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(p => base.CreateProperty(p, memberSerialization))
                    .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Select(f => base.CreateProperty(f, memberSerialization)))
                    .ToList();

                properties.ForEach(p => { p.Writable = true; p.Readable = true; });

                return properties;
            }
        }
    }
}
