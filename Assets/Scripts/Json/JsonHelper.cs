using System.IO;
using Valve.Newtonsoft.Json;

public class JsonHelper
{
    public static T DeserializeFromFile<T>(string path)
    {
        T result = JsonConvert.DeserializeObject<T>(File.ReadAllText(path), GetSettings());
        return result;
    }

    public static void SerializeToFile(string path, object obj)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path,JsonConvert.SerializeObject(obj, GetSettings()));
    }

    public static JsonSerializerSettings GetSettings()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new TaskConverter());
        settings.Converters.Add(new QuaternionConverter());
        settings.Formatting = Formatting.Indented;
        return settings;
    }
}
