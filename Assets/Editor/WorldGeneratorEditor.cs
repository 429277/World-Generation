using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WorldGenerator worldGenerator = (WorldGenerator)target;
        if (DrawDefaultInspector())
        {
            if (worldGenerator._autoUpdate)
            {
                worldGenerator.DrawMapInEditor();
            }
        };

        if (GUILayout.Button("Preview map"))
        {
            worldGenerator.DrawMapInEditor();
        }
    }
}
