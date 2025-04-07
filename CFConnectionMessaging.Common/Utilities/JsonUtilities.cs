using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace CFConnectionMessaging.Utilities
{
    internal static class JsonUtilities
    {  
        public static JsonSerializerOptions DefaultJsonSerializerOptions
        {
            get
            {
                var jsonSerializerOptions = new JsonSerializerOptions();
                //jsonSerializerOptions.Converters.Add(new TimeSpanToStringConverter());
                jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                jsonSerializerOptions.WriteIndented = true;
                jsonSerializerOptions.PropertyNameCaseInsensitive = true;
                return jsonSerializerOptions;
            }
        }

        public static string SerializeToString<T>(T item, JsonSerializerOptions options)
        {
            return JsonSerializer.Serialize(item, options);
        }

        public static T DeserializeFromString<T>(string json, JsonSerializerOptions options)
        {
            return (T)JsonSerializer.Deserialize(json, typeof(T), options)!;
        }
    }
}
