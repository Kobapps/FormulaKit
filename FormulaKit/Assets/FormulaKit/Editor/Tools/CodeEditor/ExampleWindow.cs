
using System;
using UnityEditor;

namespace Joshcamas.CodeEditor
{
    public class ExampleWindow : EditorWindow
    {
        private EditorView view = new EditorView();

        [MenuItem("Joshcamas/Code Editor Example")]
        public static void OpenWindow()
        {
            CreateWindow<ExampleWindow>("Example Code Editor");
        }
        
        private void OnEnable()
        {
            view.OnEnable(string.Empty);
        }

        private void OnGUI()
        {
            view.EditorViewGUI();
        }
    }
}