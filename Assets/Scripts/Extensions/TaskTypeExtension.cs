using System;
using System.Collections.Generic;

public static class TaskTypeExtension 
{
   
   /// <summary>
   /// Generates a string out of the current task type.
   /// </summary>
   public static string ToTaskTypeString(this TaskType type)
   {
      List<string> typeList = new List<string>();
      if (type.HasFlag(TaskType.Selection))
         typeList.Add("Selection");
      if (type.HasFlag(TaskType.Positioning))
         typeList.Add("Positioning");
      if (type.HasFlag(TaskType.Rotating))
         typeList.Add("Rotating");
      if (type.HasFlag(TaskType.Scaling))
         typeList.Add("Scaling");
      return String.Join(",", typeList);
   }
}
