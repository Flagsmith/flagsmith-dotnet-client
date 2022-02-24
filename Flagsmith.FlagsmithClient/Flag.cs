using Newtonsoft.Json;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Identity.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System;

namespace Flagsmith
{
    [JsonConverter(typeof(FlagJsonConverter))]
    public class Flag
    {
        public Flag() { }
        public Flag(string name, bool enabled, string value, int featureId = default)
        {
            this.Enabled = enabled;
            this.Value = value;
            this.Name = name;
            this.FeatureId = featureId;

        }
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("feature.id")]
        public int FeatureId { get; private set; }

        [JsonProperty("feature.name")]
        public string Name { get; private set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; private set; }

        [JsonProperty("feature_state_value")]
        public string Value { get; private set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    internal class FlagJsonConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            object targetObj = Activator.CreateInstance(objectType);
            foreach (PropertyInfo prop in objectType.GetProperties().Where(p => p.CanRead && p.CanWrite))
            {
                JsonPropertyAttribute att = prop.GetCustomAttributes(true)
                                                .OfType<JsonPropertyAttribute>()
                                                .FirstOrDefault();

                string jsonPath = (att != null ? att.PropertyName : prop.Name);
                JToken token = jo.SelectToken(jsonPath);

                if (token != null && token.Type != JTokenType.Null)
                {
                    object value = token.ToObject(prop.PropertyType, serializer);
                    prop.SetValue(targetObj, value, null);
                }
            }

            return targetObj;
        }

        public override bool CanConvert(Type objectType)
        {
            // CanConvert is not called when [JsonConverter] attribute is used
            return false;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
