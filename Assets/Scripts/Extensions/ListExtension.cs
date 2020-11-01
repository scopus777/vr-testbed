using System.Collections.Generic;

public static class ListExtension 
{
    public static void MoveFirstToEnd<T>(this List<T> list)
    {
        list.Add(list[0]);
        list.RemoveAt(0);
    }
}
