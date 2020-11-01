using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

public class QuaternionConverter : JsonConverter
{
  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    Quaternion quaternion = (Quaternion) value;
    writer.WriteStartObject();
    writer.WritePropertyName("w");
    writer.WriteValue(quaternion.w);
    writer.WritePropertyName("x");
    writer.WriteValue(quaternion.x);
    writer.WritePropertyName("y");
    writer.WriteValue(quaternion.y);
    writer.WritePropertyName("z");
    writer.WriteValue(quaternion.z);
    writer.WriteEndObject();
  }

  public override bool CanConvert(Type objectType) => objectType == typeof(Quaternion);

  public override object ReadJson(
    JsonReader reader,
    Type objectType,
    object existingValue,
    JsonSerializer serializer)
  {
    JObject jObject = JObject.Load(reader);
    List<JProperty> list = jObject.Properties().ToList();
    Quaternion quaternion = new Quaternion();
    if (list.Any(p => p.Name == "w"))
      quaternion.w = (float) jObject["w"];
    if (list.Any(p => p.Name == "x"))
      quaternion.x = (float) jObject["x"];
    if (list.Any(p => p.Name == "y"))
      quaternion.y = (float) jObject["y"];
    if (list.Any(p => p.Name == "z"))
      quaternion.z = (float) jObject["z"];
    

    return quaternion;
  }

  public override bool CanRead => true;
}
