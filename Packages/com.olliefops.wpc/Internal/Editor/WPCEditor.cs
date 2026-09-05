using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

namespace WPC.Editor
{
    [CustomEditor(typeof(WPCSetup))]
    public class WPCEditor : UnityEditor.Editor
    {
        private WPCSetup wpcSetup;

        public string[] types = { "Receiver", "Controller" };
        
        private void OnEnable()
        {
            wpcSetup = (WPCSetup)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import"))
            {
                string path = EditorUtility.OpenFilePanel("Import JSON", "", "json");
                if (string.IsNullOrEmpty(path)) return;
                
                string json = File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, wpcSetup);
                
                EditorUtility.SetDirty(wpcSetup);
            }
            if (GUILayout.Button("Export"))
            {
                string json = JsonUtility.ToJson(wpcSetup, true);
                string path = EditorUtility.SaveFilePanel("Export JSON", "", "", "json");

                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllText(path, json);
                    Debug.Log($"Exported JSON to: {path}");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            int newType = EditorGUILayout.Popup(
                "Type",
                serializedObject.FindProperty("setupType").intValue,
                types
            );
            EditorGUILayout.PropertyField(serializedObject.FindProperty("menuPath"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("secretKey"));
            if (GUILayout.Button("Generate Secret Key"))
            {
                System.Random random = new System.Random();
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

                serializedObject.FindProperty("secretKey").stringValue = new string(Enumerable.Repeat(chars, 24)
                    .Select(s => s[random.Next(s.Length)])
                    .ToArray());
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("receivers"));

            if (newType != serializedObject.FindProperty("setupType").intValue)
            {
                serializedObject.FindProperty("setupType").intValue = newType;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}