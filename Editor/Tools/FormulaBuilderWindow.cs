using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FormulaKit.Editor.Tools.Utils;
using FormulaKit.Runtime;
using UnityEditor;
using UnityEngine;

namespace FormulaKit.Editor.Tools
{
    public partial class FormulaBuilderWindow : EditorWindow
    {
        private readonly Dictionary<string, float> _testInputs = new Dictionary<string, float>();
        private readonly List<FormulaExample> _examples = new List<FormulaExample>();

        private FormulaLoader _tempLoader;
        private FormulaRunner _tempRunner;

        private string _formulaId = string.Empty;
        private string _formulaExpression = string.Empty;
        private bool _advancedMode = true;

        private Vector2 _inputsScroll;
        private float _evaluationResult;
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.None;
        private string _examplesError;

        private EditorView _editorView;
        private EditorViewOptions _editorOptions;
        private bool _editorViewDirty = true;

        private static readonly Regex NumberRegex = new Regex(@"\b\d+(\.\d+)?\b", RegexOptions.Compiled);
        private static readonly Regex KeywordRegex = new Regex(@"\b(let|if|else|elseif|return|true|false)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex FunctionRegex = new Regex(@"\b(abs|acos|asin|atan|ceil|clamp|clamp01|cos|exp|floor|lerp|log|max|min|negative|pow|rand|randf|random|round|sign|sin|sqrt|tan)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex OperatorRegex = new Regex(@"[\+\-\*/=<>!&\|\^]+", RegexOptions.Compiled);

        private static readonly string[] FormulaKeywords =
        {
            "let", "if", "elseif", "else", "return", "true", "false"
        };

        private static readonly FunctionSnippet[] FunctionSnippets =
        {
            new FunctionSnippet("abs", "abs(value)", "Returns the absolute value."),
            new FunctionSnippet("acos", "acos(value)", "Arc cosine in radians."),
            new FunctionSnippet("asin", "asin(value)", "Arc sine in radians."),
            new FunctionSnippet("atan", "atan(value)", "Arc tangent in radians."),
            new FunctionSnippet("ceil", "ceil(value)", "Rounds value up."),
            new FunctionSnippet("clamp", "clamp(value, min, max)", "Clamp between min and max."),
            new FunctionSnippet("clamp01", "clamp01(value)", "Clamp between 0 and 1."),
            new FunctionSnippet("cos", "cos(radians)", "Cosine of the angle."),
            new FunctionSnippet("exp", "exp(power)", "Euler's number raised to power."),
            new FunctionSnippet("floor", "floor(value)", "Rounds value down."),
            new FunctionSnippet("lerp", "lerp(a, b, t)", "Linearly interpolates."),
            new FunctionSnippet("log", "log(value)", "Natural logarithm."),
            new FunctionSnippet("max", "max(a, b)", "Maximum of two values."),
            new FunctionSnippet("min", "min(a, b)", "Minimum of two values."),
            new FunctionSnippet("negative", "negative(value)", "Negates the value."),
            new FunctionSnippet("pow", "pow(value, power)", "Raises value to a power."),
            new FunctionSnippet("rand", "rand(maxExclusive)", "Random integer."),
            new FunctionSnippet("randf", "randf(maxExclusive)", "Random float below max."),
            new FunctionSnippet("random", "random()", "Random float between 0 and 1."),
            new FunctionSnippet("round", "round(value)", "Rounds to nearest integer."),
            new FunctionSnippet("sign", "sign(value)", "Returns the sign."),
            new FunctionSnippet("sin", "sin(radians)", "Sine of the angle."),
            new FunctionSnippet("sqrt", "sqrt(value)", "Square root of value."),
            new FunctionSnippet("tan", "tan(radians)", "Tangent of the angle."),
        };

        [MenuItem("Tools/Formula Framework/Formula Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormulaBuilderWindow>("Formula Builder");
            window.minSize = new Vector2(520f, 640f);
            window.Show();
        }

        private void OnEnable()
        {
            _tempLoader = new FormulaLoader();
            _tempRunner = new FormulaRunner(_tempLoader);
            InitializeEditorView();
            LoadExamples();
        }

        private EditorViewOptions CreateEditorOptions()
        {
            var options = new EditorViewOptions
            {
                Keywords = FormulaKeywords,
                FunctionNames = FunctionSnippets.Select(snippet => snippet.Name).ToArray(),
                ShowLineNumbers = true
            };

            options.Palette.Keyword = new Color32(86, 156, 214, 255);
            options.Palette.Parameter = options.Palette.Keyword;
            options.Palette.Function = new Color32(220, 220, 170, 255);
            options.Palette.Operator = new Color32(212, 212, 212, 255);
            options.Palette.Flag = options.Palette.Operator;

            return options;
        }

        private void InitializeEditorView()
        {
            if (_editorView != null)
            {
                _editorView.RepaintAction -= Repaint;
            }

            _editorOptions ??= CreateEditorOptions();
            _editorView = new EditorView(_editorOptions);
            _editorView.RepaintAction += Repaint;
            _editorView.OnEnable(_formulaExpression ?? string.Empty);
            _editorViewDirty = false;
        }

        private void EnsureEditorViewInitialized()
        {
            if (_editorView == null)
            {
                InitializeEditorView();
                return;
            }

            if (_editorViewDirty)
            {
                _editorView.OnEnable(_formulaExpression ?? string.Empty);
                _editorViewDirty = false;
            }
        }

        private void OnDisable()
        {
            if (_editorView != null)
            {
                _editorView.RepaintAction -= Repaint;
                _editorView = null;
            }

            _tempLoader = null;
            _tempRunner = null;
        }

        private void OnGUI()
        {
            GUILayout.Space(8f);
            DrawHeader();
            GUILayout.Space(8f);

            DrawFormulaEditor();
            GUILayout.Space(12f);

            DrawInputsSection();
            GUILayout.Space(12f);

            DrawEvaluationSection();
            GUILayout.Space(12f);

            DrawStatus();
        }

        private void DrawHeader()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.Label("Formula Builder", titleStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.HelpBox("Create and test formulas with advanced editing.", MessageType.None);
        }

        private void DrawFormulaEditor()
        {
            EditorGUILayout.LabelField("Formula", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUI.BeginChangeCheck();
                string newId = EditorGUILayout.TextField("Formula ID", _formulaId);
                if (EditorGUI.EndChangeCheck())
                {
                    _formulaId = newId.Trim();
                }

                GUILayout.Space(4f);

                bool newAdvanced = EditorGUILayout.Toggle("Advanced Editor", _advancedMode);
                if (newAdvanced != _advancedMode)
                {
                    _advancedMode = newAdvanced;
                    if (_advancedMode)
                    {
                        _editorViewDirty = true;
                        EnsureEditorViewInitialized();
                        _editorView.RequestFocus();
                    }
                }

                GUILayout.Space(4f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Expression", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(_examples.Count == 0))
                    {
                        if (GUILayout.Button("Examples", EditorStyles.miniButton))
                        {
                            ShowExamplesMenu();
                        }
                    }

                    if (GUILayout.Button("Functions", EditorStyles.miniButton))
                    {
                        ShowFunctionsMenu();
                    }

                    if (GUILayout.Button("Help", EditorStyles.miniButton))
                    {
                        ShowSyntaxHelp();
                    }
                }

                GUILayout.Space(4f);

                if (_advancedMode)
                {
                    EnsureEditorViewInitialized();
                    _editorView.EditorViewGUI();
                    _formulaExpression = _editorView.GetText();
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    string updated = EditorGUILayout.TextArea(_formulaExpression, EditorStyles.textArea, GUILayout.Height(160f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        _formulaExpression = updated;
                        _editorViewDirty = true;
                    }
                }

                if (!string.IsNullOrEmpty(_examplesError))
                {
                    EditorGUILayout.HelpBox(_examplesError, MessageType.Warning);
                }
            }
        }

        private void DrawInputsSection()
        {
            EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Auto-Detect Inputs"))
                    {
                        AutoDetectInputs();
                    }

                    if (GUILayout.Button("Add Input"))
                    {
                        string name = GenerateUniqueInputName();
                        _testInputs[name] = 0f;
                    }

                    using (new EditorGUI.DisabledScope(_testInputs.Count == 0))
                    {
                        if (GUILayout.Button("Clear"))
                        {
                            _testInputs.Clear();
                        }

                        if (GUILayout.Button("Random Values"))
                        {
                            ApplyRandomValues();
                        }
                    }
                }

                GUILayout.Space(4f);

                if (_testInputs.Count == 0)
                {
                    EditorGUILayout.HelpBox("No inputs. Detect or add manually.", MessageType.Info);
                }
                else
                {
                    _inputsScroll = EditorGUILayout.BeginScrollView(_inputsScroll, GUILayout.Height(160f));
                    string[] keys = _testInputs.Keys.ToArray();
                    foreach (string key in keys)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(key, GUILayout.Width(160f));
                            _testInputs[key] = EditorGUILayout.FloatField(_testInputs[key]);
                            if (GUILayout.Button("✕", GUILayout.Width(24f)))
                            {
                                _testInputs.Remove(key);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawEvaluationSection()
        {
            EditorGUILayout.LabelField("Evaluation", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(2f);
                if (GUILayout.Button("Evaluate", GUILayout.Height(32f)))
                {
                    EvaluateFormula();
                }

                GUILayout.Space(6f);
                GUILayout.Label("Result", EditorStyles.miniBoldLabel);
                var resultStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 18,
                    fontStyle = FontStyle.Bold
                };
                GUILayout.Box(_evaluationResult.ToString("F4"), resultStyle, GUILayout.Height(40f));
            }
        }

        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void ApplyRandomValues()
        {
            string[] keys = _testInputs.Keys.ToArray();
            foreach (string key in keys)
            {
                _testInputs[key] = UnityEngine.Random.Range(0f, 100f);
            }
        }

        private void AutoDetectInputs()
        {
            _testInputs.Clear();
            if (string.IsNullOrEmpty(_formulaId) || string.IsNullOrEmpty(_formulaExpression))
            {
                ShowStatus("Enter formula ID and expression first.", MessageType.Warning);
                return;
            }

            bool success = _tempLoader.RegisterFormula(_formulaId, _formulaExpression);
            if (!success)
            {
                ShowStatus("Formula contains errors.", MessageType.Error);
                return;
            }

            var inputs = _tempLoader.GetRequiredInputs(_formulaId);
            foreach (string input in inputs)
            {
                if (!_testInputs.ContainsKey(input))
                {
                    _testInputs[input] = 0f;
                }
            }

            ShowStatus(inputs.Count > 0
                ? $"Detected inputs: {string.Join(", ", inputs)}"
                : "No inputs detected.", MessageType.Info);
        }

        private void EvaluateFormula()
        {
            if (string.IsNullOrEmpty(_formulaId) || string.IsNullOrEmpty(_formulaExpression))
            {
                ShowStatus("Enter formula ID and expression first.", MessageType.Warning);
                return;
            }

            bool success = _tempLoader.RegisterFormula(_formulaId, _formulaExpression);
            if (!success)
            {
                ShowStatus("Formula contains syntax errors.", MessageType.Error);
                return;
            }

            try
            {
                _evaluationResult = _tempRunner.Evaluate(_formulaId, _testInputs);
                ShowStatus("Evaluation successful.", MessageType.Info);
            }
            catch (Exception ex)
            {
                ShowStatus($"Evaluation failed: {ex.Message}", MessageType.Error);
                _evaluationResult = 0f;
            }
        }

        private void ShowStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }

        private void ShowExamplesMenu()
        {
            if (_examples.Count == 0)
            {
                return;
            }

            GenericMenu menu = new GenericMenu();
            foreach (var group in _examples.GroupBy(e => e.Category))
            {
                foreach (var example in group)
                {
                    string label = $"{group.Key}/{example.Name}";
                    menu.AddItem(new GUIContent(label), false, () => ApplyExample(example));
                }
            }

            menu.ShowAsContext();
        }

        private void ApplyExample(FormulaExample example)
        {
            _formulaId = example.Id;
            _formulaExpression = example.Expression;
            _editorViewDirty = true;
            if (_advancedMode)
            {
                EnsureEditorViewInitialized();
                _editorView.RequestFocus();
            }
            AutoDetectInputs();
        }

        private void ShowFunctionsMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (var snippet in FunctionSnippets)
            {
                menu.AddItem(new GUIContent(snippet.Name), false, () => InsertFunctionSnippet(snippet));
            }

            menu.ShowAsContext();
        }

        private void InsertFunctionSnippet(FunctionSnippet snippet)
        {
            if (_advancedMode && _editorView != null)
            {
                EnsureEditorViewInitialized();
                _editorView.InsertText(snippet.Snippet);
                _formulaExpression = _editorView.GetText();
            }
            else
            {
                string current = _formulaExpression ?? string.Empty;
                current += (current.Length > 0 ? "\n" : string.Empty) + snippet.Snippet;
                _formulaExpression = current;
                _editorViewDirty = true;
            }
        }

        private void ShowSyntaxHelp()
        {
            EditorUtility.DisplayDialog(
                "Formula Syntax",
                "Keywords: let, if, elseif, else, return, true, false\n" +
                "Functions: " + string.Join(", ", FunctionSnippets.Select(f => f.Name)) + "\n" +
                "Operators: +, -, *, /, %, ==, !=, <, <=, >, >=, &&, ||, ^",
                "Close");
        }

        private void LoadExamples()
        {
            _examples.Clear();
            _examplesError = string.Empty;

            foreach (var example in GetCompiledExamples())
            {
                _examples.Add(example);
            }

            if (_examples.Count == 0)
            {
                _examplesError = "No compiled examples available.";
            }
        }

        private string GenerateUniqueInputName()
        {
            int index = 1;
            string candidate;
            do
            {
                candidate = $"input{index}";
                index++;
            }
            while (_testInputs.ContainsKey(candidate));

            return candidate;
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
            var builder = new StringBuilder(text.Length);
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

        private readonly struct FormulaExample
        {
            public string Category { get; }
            public string Name { get; }
            public string Id { get; }
            public string Expression { get; }

            public FormulaExample(string category, string name, string id, string expression)
            {
                Category = category;
                Name = name;
                Id = id;
                Expression = expression;
            }
        }

        private readonly struct FunctionSnippet
        {
            public string Name { get; }
            public string Snippet { get; }
            public string Description { get; }

            public FunctionSnippet(string name, string snippet, string description)
            {
                Name = name;
                Snippet = snippet;
                Description = description;
            }
        }
    }
}
