using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class GenerateTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator myScript = (TerrainGenerator) target;

        if (GUILayout.Button("Generate"))
        {
            myScript.Generate();
        }

        DrawDefaultInspector();
    }
}
