using System.Linq;
using FormulaFramework;
using UnityEditor;
using UnityEngine;

namespace FormulaKit.Editor
{
    /// <summary>
    /// Custom inspector for FormulaManager with quick actions
    /// </summary>
    [CustomEditor(typeof(FormulaManager))]
    public class FormulaManagerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Space(15);
            
            DrawQuickActions();
            
            GUILayout.Space(10);
            
            DrawFormulaInfo();
        }
        
        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            if (GUILayout.Button("Open Formula Builder", GUILayout.Height(35)))
            {
                FormulaBuilderWindow.ShowWindow();
            }
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("View Library", GUILayout.Height(30)))
            {
                FormulaLibraryWindow.ShowWindow();
            }
            
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All Formulas", 
                    "Are you sure?", "Yes", "No"))
                {
                    FormulaManager.Instance.ClearAll();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFormulaInfo()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see loaded formulas", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("Loaded Formulas", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            var manager = (FormulaManager)target;
            var stats = manager.GetStats();
            
            EditorGUILayout.LabelField(stats, EditorStyles.wordWrappedLabel);
            
            GUILayout.Space(5);
            
            var allIds = manager.GetAllFormulaIds().ToList();
            
            if (allIds.Count > 0)
            {
                EditorGUILayout.LabelField($"Formulas ({allIds.Count}):", EditorStyles.miniBoldLabel);
                
                foreach (var id in allIds.Take(10))
                {
                    EditorGUILayout.LabelField($"• {id}", EditorStyles.miniLabel);
                }
                
                if (allIds.Count > 10)
                {
                    EditorGUILayout.LabelField($"... and {allIds.Count - 10} more", EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No formulas loaded", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}