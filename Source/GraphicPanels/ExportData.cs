using GraphicMinimal;
using GraphicGlobal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace GraphicPanels
{
    //Alle Daten einer Scene einschließlich der Dreiecksdaten
    [Serializable()]
    internal class BigExportData
    {
        public IGeometryCommand[] Commands { get; set; }
        //public DrawingObject[] AllObjects { get; set; } //OutOfMemory bei ChinaRoom, Chessboard, Snowman, Stilllife, Mirrorballs, Candle, MirrorsEdge
        public ObjectPropertys[] AllObjects { get; set; }
        public GlobalObjectPropertys GlobalSettings { get; set; }
        public Mode3D Modus { get; set; }
    }

    //Alle Daten einer Scene außer die Dreieckspositionen (Diese Information befindet sich in den referenztierten Obj-Dateien)
    internal class SmallExportData
    {
        public IGeometryCommand[] Commands { get; set; }
        public ObjectPropertys[] AllObjectPropertys { get; set; }
        public GlobalObjectPropertys GlobalSettings { get; set; }
        public Mode3D Modus { get; set; }
    }

    //Konvertiert ein ExportData-Objekt in ein kleines Json
    class ExportDataJsonConverter
    {
        private readonly SmallExportData data;

        private ExportDataJsonConverter(SmallExportData data)
        {
            this.data = data;
        }

        #region ToJson
        public static string ToJson(SmallExportData data)
        {
            string smallJson = new ExportDataJsonConverter(data).MakeSmallJson(JsonHelper.ToJson(data));
            return smallJson;
        }

        private string MakeSmallJson(string exportDataJson)
        {
            var jObj = JObject.Parse(exportDataJson);

            RemoveDefaultPropertys(jObj);
            JsonHelper.RemoveItemsWithSpecificName(jObj, new[] { "Float3f", "XY", "Xi", "Yi", "Values" });//entferne all die Propertys welche redundant zur Objekterzeugung sind

            string result = jObj.ToString();

            result = MakeSmallWithRegExReplace(result);            

            return result;
        }

        private void RemoveDefaultPropertys(JObject jObj)
        {
            ObjectPropertysAfterAdd defaults = new ObjectPropertysAfterAdd(this.data.Commands);

            //Weg 1: Commands enthält die Propertys, die sich gegenüber Default geändert haben;
            //AllObjectPropertys enthält nur die Propertys, welche sich gegenüber den Command-Propertys geändert haben
            //Problem: Ich kann bei den AllObjectPropertys keine Default-Werte explizit nutzen (Kein weißes Licht möglich)
            //RemovePropertysWhichHasDefaultValues(jObj, new ObjektPropertys(), item => item?.Parent?.Children<JProperty>().First().Value.ToString() == "GraphicMinimal.ObjektPropertys, GraphicMinimal" && (item?.Parent?.Parent?.Parent?.Parent?.Parent?.Parent?.Parent as JProperty)?.Name == "Commands" && (item as JProperty).Name != "Id" && (item as JProperty).Name != "Name");
            //RemovePropertysWhichHasNoModificationAfterAdd(jObj, defaults, item => item?.Parent?.Children<JProperty>().First().Value.ToString() == "GraphicMinimal.ObjektPropertys, GraphicMinimal" && (item?.Parent?.Parent?.Parent?.Parent?.Parent as JProperty)?.Name == "AllObjectPropertys" && (item as JProperty).Name != "Id" && (item as JProperty).Name != "Name");

            //Weg 2: Bei den AllObjectPropertys stehen alle Propertys, welche entweder in den Commands oder über Property-Setter geändert wurden
            RemovePropertysWhichHasDefaultValues(jObj, new ObjectPropertys(), item => item?.Parent?.Children<JProperty>().First().Value.ToString() == "GraphicMinimal.ObjektPropertys, GraphicMinimal" && (item as JProperty).Name != "Id" && (item as JProperty).Name != "Name");

            RemovePropertysWhichHasDefaultValues(jObj, new GlobalObjectPropertys(), item => item?.Parent?.Children<JProperty>().First().Value.ToString() == "GraphicMinimal.GlobalObjektPropertys, GraphicMinimal");
        }

        private static void RemovePropertysWhichHasDefaultValues(JObject jObj, object defaultObject, Func<JToken, bool> takeThis)
        {
            var defaultObjPropertys = JObject.Parse(JsonConvert.SerializeObject(defaultObject, Formatting.Indented, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All }));

            List<JProperty> removeItems = new List<JProperty>();
            foreach (var item in JsonHelper.GetAllItems(jObj))
            {
                if (item is JProperty property && (item as JProperty).Name != "$type" && takeThis(item))
                {
                    JProperty prop = property;

                    string defaultValue = defaultObjPropertys.Children<JProperty>().Where(x => x.Name == prop.Name).First().Value.ToString();
                    string value = prop.Value.ToString();

                    if (value == defaultValue) removeItems.Add(prop);
                }
                var s = item.ToString();
            }
            foreach (var rem in removeItems)
            {
                rem.Parent[rem.Name].Parent.Remove();
            }
        }

        private static string MakeSmallWithRegExReplace(string json)
        {
            //var matches = new Regex(@"{\s+""\$type"": ""GraphicMinimal.Vector3D,[^X]+X.: (?<X>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^Y]+Y.: (?<Y>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^Z]+Z.: (?<Z>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^}]+}").Matches(json)
            //    .Cast<Match>().Select(x => x.Groups["X"] + " " + x.Groups["Y"] + " " + x.Groups["Z"]).ToArray();

            string small = json;
            
            small = Regex.Replace(small, @"{\s+""\$type"": ""GraphicMinimal.Vector3D,[^X]+X.: (?<X>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^Y]+Y.: (?<Y>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^Z]+Z.: (?<Z>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^}]+}", "[${X}, ${Y}, ${Z}]");
            small = Regex.Replace(small, @"{[^G]+GraphicMinimal.Vector2D,[^X]+X.: (?<X>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^Y]+Y.: (?<Y>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^}]+}", "[${X}, ${Y}]");
            small = Regex.Replace(small, @".TextureMatrix[^D]+Definition.: ""([^""]+)""[^}]+}", @"""TextureMatrix"": ""$1""");
            small = Regex.Replace(small, @"""\$type"": ""GraphicMinimal.ObjectPropertys, GraphicMinimal"",[^""]+""Name", "\"Name");
            small = Regex.Replace(small, @"{\s+""Name"": """",", "{");

            return small;
        }
        #endregion

        #region FromJson
        public static SmallExportData CreateFromJson(string json)
        {
            string big = MakeBigWithRegExReplace(json);
            return JsonHelper.CreateFromJson<SmallExportData>(big);
        }

        private static string MakeBigWithRegExReplace(string json)
        {
            //var match = new Regex(@"{\s+""\$type"": ""GraphicMinimal.Vector3D,[^X]+X.: (?<X>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^Y]+Y.: (?<Y>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^Z]+Z.: (?<Z>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)[^}]+}").Matches(json);

            string big = json;

            big = Regex.Replace(big, @"\[(?<X>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?), (?<Y>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?), (?<Z>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)\]", "{        \"$type\": \"GraphicMinimal.Vector3D, GraphicMinimal\",        \"X\": ${X},        \"Y\": ${Y},        \"Z\": ${Z}      }");
            big = Regex.Replace(big, @"\[(?<X>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?), (?<Y>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?)\]", "{          \"$type\": \"GraphicMinimal.Vector2D, GraphicMinimal\",          \"X\": ${X},          \"Y\": ${Y}        }");
            big = Regex.Replace(big, @"""TextureMatrix"": ""([^""]+)""", "\"TextureMatrix\": {          \"$type\": \"GraphicMinimal.Matrix3x3, GraphicMinimal\",          \"Definition\": \"$1\"        }");
            big = Regex.Replace(big, @"""Name""", "\"$type\": \"GraphicMinimal.ObjectPropertys, GraphicMinimal\",\r\n        \"Name\"");
            //big = Regex.Replace(big, @"{\s+""Name"": """",", "{");

            return big;
        }
        #endregion
    }

    class ObjectPropertysAfterAdd
    {
        private readonly List<Item> items;

        public ObjectPropertysAfterAdd(IGeometryCommand[] commands)
        {
            GraphicPanel3D grafik = new GraphicPanel3D();
            foreach (var command in commands)
            {
                command.Execute(grafik);
            }
            this.items = grafik.GetAllObjects().Select(x => new Item(x)).ToList();
            this.items.Add(new Item(new ObjectPropertys()));
        }

        public string GetPropertyValueFromObject(int objId, string propertyName)
        {
            var obj = this.items.Where(x => x.Id == objId).First();
            string value = obj.Obj.Children<JProperty>().Where(x => x.Name == propertyName).First().Value.ToString();
            return value;
        }

        class Item
        {
            private readonly ObjectPropertys prop;

            public int Id { get => prop.Id; }
            public JObject Obj { get; private set; }

            public Item(ObjectPropertys prop)
            {
                this.prop = prop;
                this.Obj = JObject.Parse(JsonConvert.SerializeObject(prop, Formatting.Indented, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All }));
            }
        }
    }

    class JsonHelper
    {
        //https://www.codeproject.com/Questions/1105164/Serialise-interfaces-in-Csharp
        public static string ToJson(object o)
        {
            var indented = Formatting.Indented;
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };

            string json = JsonConvert.SerializeObject(o, indented, settings);
            return json;
        }

        public static T CreateFromJson<T>(string json)
        {
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
            T obj = JsonConvert.DeserializeObject<T>(json, settings);
            return obj;
        }


        //Traversiert durch den Json-Baum
        public static IEnumerable<JToken> GetAllItems(JToken token)
        {
            yield return token;

            foreach (var child in token.Children<JToken>())
            {
                foreach (var child1 in GetAllItems(child))
                {
                    yield return child1;
                }
            }
        }

        //Entfernt all die Propertys aus dem Json-Baum, die ein bestimmten Name haben
        public static void RemoveItemsWithSpecificName(JToken jObj, string[] namesToRemove)
        {
            List<string> names = new List<string>();
            foreach (var child in jObj.Children<JProperty>())
            {
                if (namesToRemove.Contains(child.Name))
                    names.Add(child.Name);
            }
            foreach (var name in names)
            {
                jObj[name].Parent.Remove(); //https://stackoverflow.com/questions/21898727/getting-the-error-cannot-add-or-remove-items-from-newtonsoft-json-linq-jpropert
            }

            foreach (var child in jObj.Children<JToken>())
            {
                RemoveItemsWithSpecificName(child, namesToRemove);
            }
        }
    }
}
