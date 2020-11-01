using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Creates a table where techniques are randomly assigned to a given number of users.
/// Tries to evenly distribute the techniques.
/// </summary>
public class TechniqueUserAssignmentWindow : EditorWindow
{
    
    private string targetFile = Path.Combine(Application.streamingAssetsPath, "techniqueUserAssignment.csv");
    private int numberOfUsers = 20;
    private int techniquesPerUser = 3;
    
    [MenuItem("Window/Technique User Assignment")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TechniqueUserAssignmentWindow));
    }

    private void OnGUI()
    {
        targetFile = EditorGUILayout.TextField("Target File", targetFile);
        numberOfUsers = EditorGUILayout.IntField("Number of Users", numberOfUsers);
        techniquesPerUser = EditorGUILayout.IntField("Techniques per User", techniquesPerUser);
        if (GUILayout.Button("Generate"))
            GenerateTechniqueUserAssignment();
    }

    private void GenerateTechniqueUserAssignment()
    {
        List<string> techniques = File.ReadLines(targetFile).First().Split(';').ToList();
        techniques.RemoveAt(0);
        techniques.RemoveAt(techniques.Count - 1);
        Dictionary<string, int> techniqueUsages = techniques.ToDictionary(technique => technique, technique => 0);

        List<string> lines = File.ReadLines(targetFile).ToList();
        for (int i = 1; i < lines.Count; i++)
        {
            List<string> cells = lines[i].Split(';').ToList();
            for (int j = 1; j < cells.Count - 1; j++)
                if (cells[j].Equals("x"))
                    techniqueUsages[techniques[j - 1]]++;
        }

        int lastUserId = lines.Count > 1 ? int.Parse(lines[lines.Count - 1].Split(';')[0]) : 0;

        for (int i = 1 + lastUserId; i <= numberOfUsers + lastUserId; i++)
        {
            List<string> chosenTechniques = new List<string>();
            for (int j = 0; j < techniquesPerUser; j++)
                chosenTechniques.Add(GetTechnique(techniqueUsages, chosenTechniques));
            string line = i + techniques.Aggregate("",
                              (current, technique) =>
                                  current + (";" + (chosenTechniques.Contains(technique) ? "x" : ""))) + ";" +
                          string.Join(",", chosenTechniques.OrderBy(a => Random.value).ToList());
            File.AppendAllText(targetFile, "\n" + line);
        }
    }

    private string GetTechnique(Dictionary<string,int> techniqueUsages, List<string> chosenTechniques)
    {
        int min = techniqueUsages.Values.Min();
        List<string> possibleTechniques = techniqueUsages.Where(pair => pair.Value == min).Select(pair => pair.Key).ToList();
        possibleTechniques.RemoveAll(chosenTechniques.Contains);
        int randomInt = Random.Range(0, possibleTechniques.Count);
        techniqueUsages[possibleTechniques[randomInt]]++;
        return possibleTechniques[randomInt];
    }
}
