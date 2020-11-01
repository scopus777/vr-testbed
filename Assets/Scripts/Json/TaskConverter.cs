using System;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

public class TaskConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jObject = JObject.Load(reader);
        TaskType taskType = (TaskType) jObject.GetValue("taskTypes").Value<Int64>();
        
        Task task;
        if (taskType.HasFlag(TaskType.Selection))
            task = new SelectionTask();
        else
            task = new ManipulationTask();
        
        serializer.Populate(jObject.CreateReader(), task);
        
        return task;
    }

    public override bool CanWrite
    {
        get { return false; }
    }
    public override bool CanConvert(Type objectType)
    {
        return typeof(Task).IsAssignableFrom(objectType);
    }

}
