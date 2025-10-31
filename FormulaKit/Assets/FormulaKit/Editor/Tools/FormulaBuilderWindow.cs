using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FormulaKit.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FormulaKit.Editor.Tools.Tools
{
    public class FormulaBuilderWindow : EditorWindow
    {
        private const string LayoutPath = "Assets/FormulaKit/Editor/Tools/FormulaBuilderWindow.uxml";
        private const string StylePath = "Assets/FormulaKit/Editor/Tools/FormulaBuilderWindow.uss";
        private const string ExamplesAssetPath = "Assets/FormulaKit/Editor/Tools/FormulaExamples.txt";

        private readonly Dictionary<string, float> _testInputs = new Dictionary<string, float>();
        private readonly List<FormulaExample> _examples = new List<FormulaExample>();

        private FormulaLoader _tempLoader;
        private FormulaRunner _tempRunner;

        private string _formulaId = string.Empty;
        private string _formulaExpression = string.Empty;
        private bool _advancedMode = true;

        private string _examplesError;

        private TextField _idField;
        private TextField _basicExpression;
        private CodeEditorElement _codeEditor;
        private Toggle _advancedToggle;
        private VisualElement _editorHost;
        private Button _examplesButton;
        private Button _functionsButton;
        private Button _helpButton;
        private Button _autoDetectButton;
        private ScrollView _inputsContainer;
        private Button _addInputButton;
        private Button _clearInputsButton;
        private Button _randomInputsButton;
        private Button _evaluateButton;
        private Label _resultLabel;
        private HelpBox _statusBox;

        private readonly List<InputRow> _inputRows = new List<InputRow>();

        private static readonly Regex NumberRegex = new Regex(@"\\b\\d+(\\.\\d+)?\\b", RegexOptions.Compiled);
        private static readonly Regex KeywordRegex = new Regex(@"\\b(let|if|else|elseif|return|true|false)\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex FunctionRegex = new Regex(@"\\b(abs|acos|asin|atan|ceil|clamp|clamp01|cos|exp|floor|lerp|log|max|min|negative|pow|rand|randf|random|round|sign|sin|sqrt|tan)\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex OperatorRegex = new Regex(@"[\\+\\-\\*/=<>!&\\|\\^]+", RegexOptions.Compiled);

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

        [MenuItem("Tools/Formula Framework/Formula Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormulaBuilderWindow>("Formula Builder");
            window.minSize = new Vector2(520, 640);
            window.Show();
        }

        private void OnEnable()
        {
            _tempLoader = new FormulaLoader();
            _tempRunner = new FormulaRunner(_tempLoader);
            LoadExamples();
        }

        private void OnDisable()
        {
            _tempLoader = null;
            _tempRunner = null;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            if (visualTree == null)
            {
                rootVisualElement.Add(new Label("Missing layout asset"));
                return;
            }

            VisualElement layout = visualTree.CloneTree();
            rootVisualElement.Add(layout);

            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (style != null)
            {
                rootVisualElement.styleSheets.Add(style);
            }

            CacheElements(layout);
            SetupBindings();
            RefreshEditorMode();
            RefreshInputsUI();
            UpdateStatus();
        }

        private void CacheElements(VisualElement root)
        {
            _idField = root.Q<TextField>("formula-id");
            _basicExpression = root.Q<TextField>("basic-expression");
            _editorHost = root.Q<VisualElement>("editor-host");
            _advancedToggle = root.Q<Toggle>("advanced-toggle");
            _examplesButton = root.Q<Button>("examples-button");
            _functionsButton = root.Q<Button>("functions-button");
            _helpButton = root.Q<Button>("help-button");
            _autoDetectButton = root.Q<Button>("auto-detect-button");
            _inputsContainer = root.Q<ScrollView>("inputs-container");
            _addInputButton = root.Q<Button>("add-input-button");
            _clearInputsButton = root.Q<Button>("clear-inputs-button");
            _randomInputsButton = root.Q<Button>("random-inputs-button");
            _evaluateButton = root.Q<Button>("evaluate-button");
            _resultLabel = root.Q<Label>("result-label");
            _statusBox = root.Q<HelpBox>("status-box");

            _codeEditor = new CodeEditorElement();
            _codeEditor.name = "advanced-expression";
            _codeEditor.SetHighlighter(HighlightFormula);
            _codeEditor.TextChanged += OnAdvancedExpressionChanged;
            _editorHost.Add(_codeEditor);
        }

        private void SetupBindings()
        {
            _idField?.RegisterValueChangedCallback(evt =>
            {
                _formulaId = evt.newValue ?? string.Empty;
            });

            if (_basicExpression != null)
            {
                _basicExpression.multiline = true;
                _basicExpression.label = "Expression";
                _basicExpression.RegisterValueChangedCallback(evt =>
                {
                    if (!_advancedMode)
                    {
                        _formulaExpression = evt.newValue ?? string.Empty;
                    }
                });
            }

            if (_advancedToggle != null)
            {
                _advancedToggle.value = _advancedMode;
                _advancedToggle.RegisterValueChangedCallback(evt =>
                {
                    _advancedMode = evt.newValue;
                    RefreshEditorMode();
                });
            }

            _examplesButton?.RegisterCallback<ClickEvent>(_ => ShowExamplesMenu());
            _functionsButton?.RegisterCallback<ClickEvent>(_ => ShowFunctionMenu());
            _helpButton?.RegisterCallback<ClickEvent>(_ => ShowSyntaxHelp());

            _autoDetectButton?.RegisterCallback<ClickEvent>(_ => AutoDetectInputs());
            _addInputButton?.RegisterCallback<ClickEvent>(_ => AddManualInput());
            _clearInputsButton?.RegisterCallback<ClickEvent>(_ => ClearInputs());
            _randomInputsButton?.RegisterCallback<ClickEvent>(_ => RandomizeInputs());
            _evaluateButton?.RegisterCallback<ClickEvent>(_ => EvaluateFormula());

            _codeEditor.SetTextWithoutNotify(_formulaExpression);
            _basicExpression?.SetValueWithoutNotify(_formulaExpression);
            _idField?.SetValueWithoutNotify(_formulaId);
        }

        private void RefreshEditorMode()
        {
            if (_basicExpression == null || _codeEditor == null)
            {
                return;
            }

            if (_advancedMode)
            {
                _basicExpression.style.display = DisplayStyle.None;
                _codeEditor.style.display = DisplayStyle.Flex;
                _codeEditor.SetTextWithoutNotify(_formulaExpression);
            }
            else
            {
                _basicExpression.style.display = DisplayStyle.Flex;
                _codeEditor.style.display = DisplayStyle.None;
                _basicExpression.SetValueWithoutNotify(_formulaExpression);
            }
        }

        private void RefreshInputsUI()
        {
            if (_inputsContainer == null)
            {
                return;
            }

            _inputsContainer.Clear();
            _inputRows.Clear();

            if (_testInputs.Count == 0)
            {
                var empty = new Label("No inputs. Click 'Auto-Detect' or add manually.")
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        color = new Color(0.75f, 0.75f, 0.75f)
                    }
                };
                _inputsContainer.Add(empty);
                return;
            }

            foreach (var pair in _testInputs)
            {
                AddInputRow(pair.Key, pair.Value);
            }
        }

        private void AddInputRow(string key, float value)
        {
            if (_inputsContainer == null)
            {
                return;
            }

            var rowElement = new VisualElement();
            rowElement.AddToClassList("input-row");

            var label = new Label(key);
            label.AddToClassList("input-row__label");
            rowElement.Add(label);

            var valueField = new FloatField
            {
                value = value
            };
            valueField.AddToClassList("input-row__field");
            valueField.RegisterValueChangedCallback(evt =>
            {
                _testInputs[key] = evt.newValue;
            });
            rowElement.Add(valueField);

            var removeButton = new Button(() =>
            {
                _testInputs.Remove(key);
                RefreshInputsUI();
            })
            {
                text = "×"
            };
            removeButton.AddToClassList("input-row__remove");
            rowElement.Add(removeButton);

            _inputsContainer.Add(rowElement);
            _inputRows.Add(new InputRow(key, label, valueField, removeButton));
        }

        private void OnAdvancedExpressionChanged(string value)
        {
            if (_advancedMode)
            {
                _formulaExpression = value ?? string.Empty;
            }
        }

        private void AddManualInput()
        {
            string name = GenerateUniqueInputName();
            _testInputs[name] = 0f;
            RefreshInputsUI();
        }

        private void ClearInputs()
        {
            _testInputs.Clear();
            RefreshInputsUI();
        }

        private void RandomizeInputs()
        {
            foreach (var key in _testInputs.Keys.ToList())
            {
                _testInputs[key] = UnityEngine.Random.Range(0f, 100f);
            }

            foreach (var row in _inputRows)
            {
                if (_testInputs.TryGetValue(row.Key, out float value))
                {
                    row.Field.SetValueWithoutNotify(value);
                }
            }
        }

        private bool AutoDetectInputs(bool silent = false)
        {
            _testInputs.Clear();

            if (string.IsNullOrEmpty(_formulaId) || string.IsNullOrEmpty(_formulaExpression))
            {
                if (!silent)
                {
                    ShowStatus("Enter formula ID and expression first", MessageType.Warning);
                }

                RefreshInputsUI();
                return false;
            }

            bool success = _tempLoader.RegisterFormula(_formulaId, _formulaExpression);
            if (success)
            {
                var inputs = _tempLoader.GetRequiredInputs(_formulaId);
                foreach (var input in inputs)
                {
                    if (!_testInputs.ContainsKey(input))
                    {
                        _testInputs[input] = 0f;
                    }
                }

                RefreshInputsUI();

                if (!silent)
                {
                    ShowStatus($"✓ Detected {inputs.Count} inputs: {string.Join(", ", inputs)}", MessageType.Info);
                }

                return true;
            }

            if (!silent)
            {
                ShowStatus("❌ Formula has errors", MessageType.Error);
            }

            RefreshInputsUI();
            return false;
        }

        private void EvaluateFormula()
        {
            if (string.IsNullOrEmpty(_formulaId) || string.IsNullOrEmpty(_formulaExpression))
            {
                ShowStatus("Enter formula ID and expression first", MessageType.Warning);
                return;
            }

            bool success = _tempLoader.RegisterFormula(_formulaId, _formulaExpression);
            if (!success)
            {
                ShowStatus("❌ Formula has syntax errors", MessageType.Error);
                return;
            }

            try
            {
                float result = _tempRunner.Evaluate(_formulaId, _testInputs);
                _resultLabel.text = result.ToString("F4");
                ShowStatus("✓ Evaluation successful!", MessageType.Info);
            }
            catch (Exception ex)
            {
                _resultLabel.text = "0.0000";
                ShowStatus($"❌ Error: {ex.Message}", MessageType.Error);
            }
        }

        private void ShowStatus(string message, MessageType type)
        {
            if (_statusBox == null)
            {
                return;
            }

            _statusBox.text = message;
            _statusBox.messageType = type;
            if (string.IsNullOrEmpty(message))
            {
                _statusBox.RemoveFromClassList("status--visible");
            }
            else
            {
                _statusBox.AddToClassList("status--visible");
            }
        }

        private void UpdateStatus()
        {
            ShowStatus(string.Empty, MessageType.Info);
        }

        private void ShowExamplesMenu()
        {
            var menu = new GenericMenu();

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
                bool firstCategory = true;
                foreach (var grouping in _examples.GroupBy(e => e.Category))
                {
                    if (!firstCategory)
                    {
                        menu.AddSeparator(string.Empty);
                    }

                    foreach (var example in grouping)
                    {
                        string path = $"{example.Category}/{example.Name}";
                        menu.AddItem(new GUIContent(path), false, () => ApplyExample(example));
                    }

                    firstCategory = false;
                }
            }

            ShowMenu(menu, _examplesButton);
        }

        private void ShowFunctionMenu()
        {
            var menu = new GenericMenu();
            foreach (var snippet in FunctionSnippets)
            {
                menu.AddItem(new GUIContent($"{snippet.Name}    — {snippet.Description}"), false, () => InsertFunctionSnippet(snippet.Snippet));
            }

            ShowMenu(menu, _functionsButton);
        }

        private void ShowMenu(GenericMenu menu, VisualElement source)
        {
            if (menu == null || source == null)
            {
                return;
            }

            Rect rect = source.worldBound;
            menu.DropDown(new Rect(rect.position, Vector2.zero));
        }

        private void ApplyExample(FormulaExample example)
        {
            _formulaId = example.Id;
            _formulaExpression = example.Expression;

            _idField?.SetValueWithoutNotify(_formulaId);
            if (_advancedMode)
            {
                _codeEditor.SetTextWithoutNotify(_formulaExpression);
            }
            else
            {
                _basicExpression.SetValueWithoutNotify(_formulaExpression);
            }

            AutoDetectInputs(true);
        }

        private void InsertFunctionSnippet(string snippet)
        {
            if (string.IsNullOrEmpty(snippet))
            {
                return;
            }

            if (_advancedMode)
            {
                _codeEditor.InsertSnippet(snippet);
                _codeEditor.FocusInput();
            }
            else if (_basicExpression != null)
            {
                var engine = _basicExpression.editorEngine;
                string current = _basicExpression.value ?? string.Empty;
                if (engine == null)
                {
                    _basicExpression.value = current + snippet;
                }
                else
                {
                    int start = Math.Min(engine.cursorIndex, engine.selectIndex);
                    int end = Math.Max(engine.cursorIndex, engine.selectIndex);
                    start = Mathf.Clamp(start, 0, current.Length);
                    end = Mathf.Clamp(end, 0, current.Length);

                    string before = current.Substring(0, start);
                    string after = current.Substring(end);
                    string updated = before + snippet + after;
                    _basicExpression.value = updated;
                    int newIndex = before.Length + snippet.Length;
                    engine.SelectRange(newIndex, newIndex);
                }

                _formulaExpression = _basicExpression.value;
            }
        }

        private void ShowSyntaxHelp()
        {
            EditorUtility.DisplayDialog(
                "Formula Syntax",
                "Supported keywords: let, if, elseif, else, return, true, false\n" +
                "Supported functions: " + string.Join(", ", FunctionSnippets.Select(f => f.Name)) + "\n" +
                "Operators: +, -, *, /, %, ==, !=, <, <=, >, >=, &&, ||, ^",
                "Close");
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

                    void Commit()
                    {
                        if (string.IsNullOrEmpty(currentCategory) ||
                            string.IsNullOrEmpty(currentName) ||
                            string.IsNullOrEmpty(currentId) ||
                            expressionLines == null)
                        {
                            return;
                        }

                        string expression = string.Join("\n", expressionLines).TrimEnd();
                        _examples.Add(new FormulaExample(currentCategory, currentName, currentId, expression));
                    }

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            Commit();
                            currentCategory = null;
                            currentName = null;
                            currentId = null;
                            expressionLines = null;
                            continue;
                        }

                        if (line.StartsWith("#"))
                        {
                            Commit();
                            currentCategory = line.Substring(1).Trim();
                            currentName = null;
                            currentId = null;
                            expressionLines = null;
                            continue;
                        }

                        if (line.StartsWith("-"))
                        {
                            Commit();
                            string[] parts = line.Substring(1).Split('|');
                            if (parts.Length >= 3)
                            {
                                currentName = parts[0].Trim();
                                currentId = parts[1].Trim();
                                currentCategory ??= "Examples";
                                expressionLines = new List<string>();
                            }

                            continue;
                        }

                        expressionLines ??= new List<string>();
                        expressionLines.Add(line);
                    }

                    Commit();
                }
            }
            catch (Exception ex)
            {
                _examplesError = $"Failed to load examples: {ex.Message}";
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

        private readonly struct InputRow
        {
            public string Key { get; }
            public Label Label { get; }
            public FloatField Field { get; }
            public Button RemoveButton { get; }

            public InputRow(string key, Label label, FloatField field, Button removeButton)
            {
                Key = key;
                Label = label;
                Field = field;
                RemoveButton = removeButton;
            }
        }
    }
}
