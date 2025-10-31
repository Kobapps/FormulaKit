using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FormulaKit.Runtime;
using UnityEditor;
using UnityEngine;

namespace FormulaKit.Editor.Tools.Tools
{
    /// <summary>
    /// Simple and clean formula editor
    /// Tools -> Formula Framework -> Formula Builder
    /// </summary>
    public class FormulaBuilderWindow : EditorWindow
    {
        // Formula data
        private string _formulaId = "";
        private string _formulaExpression = "";
        private bool _advancedMode = true;

        // Test data
        private readonly Dictionary<string, float> _testInputs = new Dictionary<string, float>();
        private float _testResult = 0f;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.None;

        // Scroll positions
        private Vector2 _expressionScroll;
        private Vector2 _inputsScroll;

        // Editor helpers
        private GUIStyle _advancedInputStyle;
        private GUIStyle _syntaxHighlightStyle;
        private bool _stylesInitialized;
        private Texture2D _transparentBackground;

        private readonly List<FormulaExample> _examples = new List<FormulaExample>();
        private string _examplesError;

        // Test components
        private FormulaLoader _tempLoader;
        private FormulaRunner _tempRunner;

        private static readonly Regex NumberRegex = new Regex(@"\b\d+(\.\d+)?\b", RegexOptions.Compiled);
        private static readonly Regex KeywordRegex = new Regex(@"\b(let|if|else|elseif|return|true|false)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex FunctionRegex = new Regex(@"\b(abs|acos|asin|atan|ceil|clamp|clamp01|cos|exp|floor|lerp|log|max|min|negative|pow|rand|randf|random|round|sign|sin|sqrt|tan)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex OperatorRegex = new Regex(@"[\+\-\*/=<>!&\|\^]+", RegexOptions.Compiled);

        private static readonly FunctionSnippet[] FunctionSnippets =
        {
            new FunctionSnippet("abs", "abs(value)", "Returns the absolute value."),
            new FunctionSnippet("acos", "acos(value)", "Arc cosine in radians."),
            new FunctionSnippet("asin", "asin(value)", "Arc sine in radians."),
            new FunctionSnippet("atan", "atan(value)", "Arc tangent in radians."),
            new FunctionSnippet("ceil", "ceil(value)", "Rounds value up to the nearest integer."),
            new FunctionSnippet("clamp", "clamp(value, min, max)", "Clamps value between min and max."),
            new FunctionSnippet("clamp01", "clamp01(value)", "Clamps value between 0 and 1."),
            new FunctionSnippet("cos", "cos(radians)", "Cosine of the angle in radians."),
            new FunctionSnippet("exp", "exp(power)", "Euler's number raised to power."),
            new FunctionSnippet("floor", "floor(value)", "Rounds value down to the nearest integer."),
            new FunctionSnippet("lerp", "lerp(a, b, t)", "Linearly interpolates between a and b."),
            new FunctionSnippet("log", "log(value)", "Natural logarithm."),
            new FunctionSnippet("max", "max(a, b)", "Maximum of a and b."),
            new FunctionSnippet("min", "min(a, b)", "Minimum of a and b."),
            new FunctionSnippet("negative", "negative(value)", "Negates the value."),
            new FunctionSnippet("pow", "pow(value, power)", "Raises value to power."),
            new FunctionSnippet("rand", "rand(maxExclusive)", "Random integer below max."),
            new FunctionSnippet("randf", "randf(maxExclusive)", "Random float below max."),
            new FunctionSnippet("random", "random()", "Random float between 0 and 1."),
            new FunctionSnippet("round", "round(value)", "Rounds to the nearest integer."),
            new FunctionSnippet("sign", "sign(value)", "Returns the sign of the value."),
            new FunctionSnippet("sin", "sin(radians)", "Sine of the angle in radians."),
            new FunctionSnippet("sqrt", "sqrt(value)", "Square root of value."),
            new FunctionSnippet("tan", "tan(radians)", "Tangent of the angle in radians."),
        };

        private const string ExpressionControlName = "FormulaBuilder.Expression";
        private const string ExamplesAssetPath = "Assets/FormulaKit/Editor/Tools/FormulaExamples.txt";
        
        [MenuItem("Tools/Formula Framework/Formula Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormulaBuilderWindow>("Formula Builder");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            _tempLoader = new FormulaLoader();
            _tempRunner = new FormulaRunner(_tempLoader);
            _stylesInitialized = false;
            LoadExamples();
        }

        private void OnDisable()
        {
            if (_transparentBackground != null)
            {
                DestroyImmediate(_transparentBackground);
                _transparentBackground = null;
            }
        }
        
        private void OnGUI()
        {
            GUILayout.Space(10);
            
            DrawHeader();
            GUILayout.Space(15);
            
            DrawFormulaEditor();
            GUILayout.Space(15);
            
            DrawTestSection();
            GUILayout.Space(15);
            
           // DrawActions();
           // GUILayout.Space(10);
            
            DrawStatus();
        }
        
        // ============== HEADER ==============
        
        private void DrawHeader()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            
            GUILayout.Label("Formula Builder", style);
            
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Create and test formulas with live feedback",
                MessageType.None);
        }
        
        // ============== FORMULA EDITOR ==============
        
        private void DrawFormulaEditor()
        {
            EditorGUILayout.LabelField("Create Formula", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // Formula ID
            EditorGUILayout.LabelField("ID:", EditorStyles.miniBoldLabel);
            _formulaId = EditorGUILayout.TextField(_formulaId);
            
            GUILayout.Space(10);
            
            // Formula Expression
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Expression:", EditorStyles.miniBoldLabel);

            GUILayout.FlexibleSpace();
            _advancedMode = EditorGUILayout.ToggleLeft("Advanced", _advancedMode, GUILayout.Width(90));

            if (GUILayout.Button("Examples ▼", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                ShowExamplesMenu();
            }

            if (GUILayout.Button("Functions +", EditorStyles.miniButton, GUILayout.Width(90)))
            {
                ShowFunctionMenu();
            }

            if (GUILayout.Button("Help", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                ShowSyntaxHelp();
            }

            EditorGUILayout.EndHorizontal();

            if (_advancedMode)
            {
                DrawAdvancedExpressionField();
            }
            else
            {
                DrawBasicExpressionField();
            }

            EditorGUILayout.EndVertical();
        }
        
        // ============== TEST SECTION ==============
        
        private void DrawTestSection()
        {
            EditorGUILayout.LabelField("Test Formula", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // Auto-detect inputs button
            if (GUILayout.Button("🔄 Auto-Detect Inputs", GUILayout.Height(25)))
            {
                AutoDetectInputs();
            }
            
            GUILayout.Space(10);
            
            // Input fields
            if (_testInputs.Count == 0)
            {
                EditorGUILayout.HelpBox("No inputs. Click 'Auto-Detect' or add manually.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Inputs:", EditorStyles.miniBoldLabel);
                
                _inputsScroll = EditorGUILayout.BeginScrollView(_inputsScroll, GUILayout.Height(150));
                
                var keys = _testInputs.Keys.ToList();
                foreach (var key in keys)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.LabelField(key, GUILayout.Width(120));
                    _testInputs[key] = EditorGUILayout.FloatField(_testInputs[key]);
                    
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        _testInputs.Remove(key);
                        GUIUtility.ExitGUI();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            GUILayout.Space(5);
            
            // Add/Clear buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("+ Add Input"))
            {
                _testInputs["newInput"] = 0f;
            }
            
            if (GUILayout.Button("Clear All"))
            {
                _testInputs.Clear();
            }
            
            if (GUILayout.Button("Random Values"))
            {
                foreach (var key in _testInputs.Keys.ToList())
                {
                    _testInputs[key] = Random.Range(0f, 100f);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Evaluate button
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            
            if (GUILayout.Button("▶ EVALUATE", GUILayout.Height(35)))
            {
                EvaluateFormula();
            }
            
            GUI.backgroundColor = oldColor;
            
            GUILayout.Space(10);
            
            // Result display
            EditorGUILayout.LabelField("Result:", EditorStyles.miniBoldLabel);
            
            var resultStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };
            
            GUILayout.Box(_testResult.ToString("F4"), resultStyle, GUILayout.Height(40));
            
            EditorGUILayout.EndVertical();
        }
        
        // ============== ACTIONS ==============
        
        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            // Clear
            if (GUILayout.Button("🗑️ Clear All", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Clear formula and inputs?", "Yes", "No"))
                {
                    ClearAll();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        // ============== STATUS ==============
        
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }
        
        // ============== HELPER METHODS ==============
        
        private bool AutoDetectInputs(bool silent = false)
        {
            _testInputs.Clear();
            if (string.IsNullOrEmpty(_formulaId) || string.IsNullOrEmpty(_formulaExpression))
            {
                if (!silent)
                {
                    ShowStatus("Enter formula ID and expression first", MessageType.Warning);
                }

                return false;
            }

            // Parse formula
            bool success = _tempLoader.RegisterFormula(_formulaId, _formulaExpression);

            if (success)
            {
                var inputs = _tempLoader.GetRequiredInputs(_formulaId);

                // Add new inputs, keep existing values
                foreach (var input in inputs)
                {
                    if (!_testInputs.ContainsKey(input))
                    {
                        _testInputs[input] = 0f;
                    }
                }

                if (!silent)
                {
                    ShowStatus($"✓ Detected {inputs.Count} inputs: {string.Join(", ", inputs)}", MessageType.Info);
                }

                return true;
            }
            else
            {
                if (!silent)
                {
                    ShowStatus("❌ Formula has errors", MessageType.Error);
                }

                return false;
            }
        }
        
        private void EvaluateFormula()
        {
            if (string.IsNullOrEmpty(_formulaId) || string.IsNullOrEmpty(_formulaExpression))
            {
                ShowStatus("Enter formula ID and expression first", MessageType.Warning);
                return;
            }
            
            // Register/update formula
            bool success = _tempLoader.RegisterFormula(_formulaId, _formulaExpression);
            
            if (!success)
            {
                ShowStatus("❌ Formula has syntax errors", MessageType.Error);
                return;
            }
            
            // Evaluate
            try
            {
                _testResult = _tempRunner.Evaluate(_formulaId, _testInputs);
                ShowStatus("✓ Evaluation successful!", MessageType.Info);
            }
            catch (System.Exception e)
            {
                ShowStatus($"❌ Error: {e.Message}", MessageType.Error);
                _testResult = 0f;
            }
        }
        
        
        private void ClearAll()
        {
            _formulaId = "";
            _formulaExpression = "";
            _testInputs.Clear();
            _testResult = 0f;
            _statusMessage = "";
        }
        
        private void ShowStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }
        
        private void ShowExamplesMenu()
        {
            GenericMenu menu = new GenericMenu();

            if (!string.IsNullOrEmpty(_examplesError))
            {
                menu.AddDisabledItem(new GUIContent(_examplesError));
            }
            else if (_examples.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No examples found"));
            }
            else
            {
                foreach (var example in _examples.OrderBy(e => e.Category).ThenBy(e => e.Name))
                {
                    string path = string.IsNullOrEmpty(example.Category)
                        ? example.Name
                        : $"{example.Category}/{example.Name}";

                    menu.AddItem(new GUIContent(path), false, () => ApplyExample(example));
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Reload"), false, LoadExamples);

            menu.ShowAsContext();
        }
        
        private void ShowSyntaxHelp()
        {
            string help = @"FORMULA SYNTAX QUICK REFERENCE

Variables:
  let bonus = damage * 0.5;

Conditionals:
  if (health < 50) { bonus = 10 }

Ternary:
  result = score > 100 ? 10 : 5;

Operators:
  +  -  *  /  ^  (math)
  <  >  <=  >=  ==  !=  (comparison)
  &&  ||  !  (logical)

Functions:
  abs, sqrt, pow, min, max, round
  clamp, clamp01, lerp, floor, ceil
  sin, cos, tan, asin, acos, atan
  log, exp, negative, sign
  rand, randf, random

Examples:
  baseDamage * (1 + strength * 0.1)
  clamp(speed, 0, maxSpeed)
  pow(level, 1.5)";

            EditorUtility.DisplayDialog("Syntax Help", help, "OK");
        }

        private void DrawBasicExpressionField()
        {
            _expressionScroll = EditorGUILayout.BeginScrollView(_expressionScroll, GUILayout.Height(140));

            var textStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 12
            };

            GUI.SetNextControlName(ExpressionControlName);
            _formulaExpression = EditorGUILayout.TextArea(_formulaExpression, textStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void DrawAdvancedExpressionField()
        {
            EnsureStyles();
            _expressionScroll = EditorGUILayout.BeginScrollView(_expressionScroll, GUILayout.Height(160));

            Rect rect = GUILayoutUtility.GetRect(0, 1000, 0, 1000, _syntaxHighlightStyle, GUILayout.ExpandHeight(true));
            rect.height = Mathf.Max(rect.height, 140f);

            GUI.Box(rect, GUIContent.none, EditorStyles.textArea);

            GUI.SetNextControlName(ExpressionControlName);
            EditorGUI.BeginChangeCheck();
            string edited = EditorGUI.TextArea(rect, _formulaExpression, _advancedInputStyle);
            if (EditorGUI.EndChangeCheck())
            {
                _formulaExpression = edited;
            }

            if (Event.current.type == EventType.Repaint)
            {
                Rect textRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
                GUI.Label(textRect, HighlightFormula(_formulaExpression), _syntaxHighlightStyle);
            }

            EditorGUILayout.EndScrollView();
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _stylesInitialized = true;

            _advancedInputStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 12
            };

            if (_transparentBackground == null)
            {
                _transparentBackground = new Texture2D(1, 1)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _transparentBackground.SetPixel(0, 0, new Color(0, 0, 0, 0));
                _transparentBackground.Apply();
            }

            Color transparent = new Color(0, 0, 0, 0);

            GUIStyleState[] states =
            {
                _advancedInputStyle.normal,
                _advancedInputStyle.focused,
                _advancedInputStyle.hover,
                _advancedInputStyle.active,
                _advancedInputStyle.onNormal,
                _advancedInputStyle.onFocused,
                _advancedInputStyle.onHover,
                _advancedInputStyle.onActive
            };

            foreach (var state in states)
            {
                state.background = _transparentBackground;
                state.textColor = transparent;
            }

            _advancedInputStyle.selectionColor = EditorStyles.textArea.selectionColor;
            _advancedInputStyle.cursorColor = EditorStyles.textArea.cursorColor;

            _syntaxHighlightStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 12,
                richText = true,
                normal = { textColor = EditorStyles.label.normal.textColor }
            };
        }

        private static string HighlightFormula(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string escaped = EscapeRichText(text);

            escaped = KeywordRegex.Replace(escaped, m => $"<color=#569CD6>{m.Value}</color>");
            escaped = FunctionRegex.Replace(escaped, m => $"<color=#DCDCAA>{m.Value}</color>");
            escaped = NumberRegex.Replace(escaped, m => $"<color=#B5CEA8>{m.Value}</color>");
            escaped = OperatorRegex.Replace(escaped, m => $"<color=#D4D4D4>{m.Value}</color>");

            return escaped;
        }

        private static string EscapeRichText(string text)
        {
            StringBuilder builder = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                switch (c)
                {
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    case '&':
                        builder.Append("&amp;");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private void ShowFunctionMenu()
        {
            GenericMenu menu = new GenericMenu();

            foreach (var snippet in FunctionSnippets)
            {
                menu.AddItem(new GUIContent($"{snippet.Name}    — {snippet.Description}"), false,
                    () => InsertFunctionSnippet(snippet.Snippet));
            }

            menu.ShowAsContext();
        }

        private void InsertFunctionSnippet(string snippet)
        {
            EditorGUI.FocusTextInControl(ExpressionControlName);
            GUI.FocusControl(ExpressionControlName);

            var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (editor == null)
            {
                _formulaExpression += snippet;
                return;
            }

            int start = Math.Min(editor.cursorIndex, editor.selectIndex);
            int end = Math.Max(editor.cursorIndex, editor.selectIndex);

            string before = _formulaExpression.Substring(0, start);
            string after = _formulaExpression.Substring(end);

            _formulaExpression = before + snippet + after;

            int newIndex = before.Length + snippet.Length;
            editor.cursorIndex = newIndex;
            editor.selectIndex = newIndex;
            Repaint();
        }

        private void LoadExamples()
        {
            _examples.Clear();
            _examplesError = string.Empty;

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ExamplesAssetPath);
            if (asset == null)
            {
                _examplesError = "FormulaExamples.txt not found";
                return;
            }

            try
            {
                using (StringReader reader = new StringReader(asset.text))
                {
                    string line;
                    string currentCategory = null;
                    string currentName = null;
                    string currentId = null;
                    List<string> expressionLines = null;

                    void CommitCurrent()
                    {
                        if (string.IsNullOrEmpty(currentCategory) || string.IsNullOrEmpty(currentName) || string.IsNullOrEmpty(currentId) || expressionLines == null)
                        {
                            return;
                        }

                        string expression = string.Join("\n", expressionLines).TrimEnd();

                        _examples.Add(new FormulaExample
                        {
                            Category = currentCategory,
                            Name = currentName,
                            Id = currentId,
                            Expression = expression
                        });

                        currentCategory = null;
                        currentName = null;
                        currentId = null;
                        expressionLines = null;
                    }

                    while ((line = reader.ReadLine()) != null)
                    {
                        string rawLine = line.TrimEnd('\r');
                        string trimmed = rawLine.Trim();

                        if (string.IsNullOrEmpty(trimmed))
                        {
                            if (expressionLines != null)
                            {
                                expressionLines.Add(string.Empty);
                            }

                            continue;
                        }

                        if (TryParseExampleHeader(trimmed, out string category, out string name, out string id))
                        {
                            CommitCurrent();
                            currentCategory = category;
                            currentName = name;
                            currentId = id;
                            expressionLines = new List<string>();
                            continue;
                        }

                        if (trimmed.StartsWith("#"))
                        {
                            continue;
                        }

                        if (expressionLines == null)
                        {
                            continue;
                        }

                        expressionLines.Add(rawLine);
                    }

                    CommitCurrent();
                }
            }
            catch (Exception e)
            {
                _examplesError = $"Failed to load examples: {e.Message}";
            }
        }

        private static bool TryParseExampleHeader(string line, out string category, out string name, out string id)
        {
            category = string.Empty;
            name = string.Empty;
            id = string.Empty;

            string[] parts = line.Split('|');
            if (parts.Length != 3)
            {
                return false;
            }

            category = parts[0].Trim();
            name = parts[1].Trim();
            id = parts[2].Trim();

            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
            {
                category = string.Empty;
                name = string.Empty;
                id = string.Empty;
                return false;
            }

            return true;
        }

        private void ApplyExample(FormulaExample example)
        {
            _formulaId = example.Id;
            _formulaExpression = example.Expression;

            if (AutoDetectInputs(true))
            {
                ShowStatus($"Loaded '{example.Name}' with inputs: {string.Join(", ", _testInputs.Keys)}", MessageType.Info);
            }
            else
            {
                ShowStatus($"Loaded '{example.Name}' (unable to detect inputs)", MessageType.Warning);
            }
        }

        private struct FormulaExample
        {
            public string Category;
            public string Name;
            public string Id;
            public string Expression;
        }

        private readonly struct FunctionSnippet
        {
            public FunctionSnippet(string name, string snippet, string description)
            {
                Name = name;
                Snippet = snippet;
                Description = description;
            }

            public string Name { get; }
            public string Snippet { get; }
            public string Description { get; }
        }
    }
}
