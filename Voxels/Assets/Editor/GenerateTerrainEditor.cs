using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class GenerateTerrainEditor : Editor
{
    SerializedProperty isolevel;
    SerializedProperty noiseScale;
    SerializedProperty octaves;
    SerializedProperty lacunarity;
    SerializedProperty persistence;
    SerializedProperty seed;
    SerializedProperty offset;
    SerializedProperty regions;
    SerializedProperty dimensions;

    private void OnEnable()
    {
        isolevel = serializedObject.FindProperty("isolevel");
        noiseScale = serializedObject.FindProperty("noiseScale");
        octaves = serializedObject.FindProperty("octaves");
        lacunarity = serializedObject.FindProperty("lacunarity");
        persistence = serializedObject.FindProperty("persistence");
        seed = serializedObject.FindProperty("seed");
        offset = serializedObject.FindProperty("offset");
        regions = serializedObject.FindProperty("regions");
        dimensions = serializedObject.FindProperty("dimensions");
    }

    public override void OnInspectorGUI()
    {
        TerrainGenerator tgScript = (TerrainGenerator) target;  

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(tgScript), typeof(TerrainGenerator), false);
        GUI.enabled = true;

        if (GUILayout.Button("Generate"))
        {
            tgScript.Generate();
        }
        
        EditorGUILayout.PropertyField(isolevel);
        EditorGUILayout.PropertyField(noiseScale);
        EditorGUILayout.PropertyField(octaves);
        EditorGUILayout.PropertyField(lacunarity);
        EditorGUILayout.PropertyField(persistence);
        EditorGUILayout.PropertyField(seed);
        EditorGUILayout.PropertyField(offset);
        EditorGUILayout.PropertyField(dimensions, true);
        EditorGUILayout.PropertyField(regions, true);
    }
}
