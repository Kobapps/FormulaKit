using System;
using System.Collections.Generic;
using System.Linq;
using FormulaKit.Editor.Tools.UI;
using FormulaKit.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FormulaKit.Editor.Tools
{
    public sealed class FormulaBuilderWindow : EditorWindow
    {
        private const string TokensUssPath = "Packages/com.barnaff.formulakit/Editor/Tools/UI/KobappsEditorTokens.uss";
        private const string EditorUssPath = "Packages/com.barnaff.formulakit/Editor/Tools/UI/FormulaCodeEditor.uss";
        private const string WindowUssPath = "Packages/com.barnaff.formulakit/Editor/Tools/UI/FormulaBuilderWindow.uss";
        private const string IconPath = "Packages/com.barnaff.formulakit/Editor/Textures/FormulaKitIcon.png";

        private static readonly FormulaExample[] Examples =
        {
            new FormulaExample("Simple", "Basic Damage", "baseDamage * (1 + strength * 0.1)"),
            new FormulaExample("Simple", "Health Regeneration", "baseRegen + vitality * 0.5"),
            new FormulaExample("Simple", "Experience Required", "100 * pow(level, 1.5)"),
            new FormulaExample(
                "Advanced",
                "Critical Hit",
                "let isCrit = random() < critChance;\nisCrit ? baseDamage * 2 : baseDamage"),
            new FormulaExample(
                "Advanced",
                "Damage with Armor",
                "let dmg = baseDamage * (1 + strength * 0.1);\nlet reduction = armor / (armor + 100);\ndmg * (1 - reduction)"),
            new FormulaExample(
                "Advanced",
                "Tiered Bonus",
                "let mult;\nif (score >= 1000) { mult = 3 }\nelse if (score >= 500) { mult = 2 }\nelse { mult = 1 }\nbaseReward * mult"),
            new FormulaExample(
                "Complex",
                "Full Damage System",
                "let weaponDmg = baseDamage * (1 + strength * 0.1);\nlet isCrit = random() < critChance;\n\nif (isCrit) {\n    weaponDmg *= 2\n}\n\nlet reduction = armor / (armor + 100);\nweaponDmg * (1 - reduction)"),
            new FormulaExample("Math", "Quadratic Formula +", "((-1*b)+sqrt((b^2)-(4*a*c)))/(2*a)"),
            new FormulaExample("Math", "Quadratic Formula -", "((-1*b)-sqrt((b^2)-(4*a*c)))/(2*a)")
        };

        private readonly Dictionary<string, float> _testInputs = new Dictionary<string, float>();

        private string _formulaExpression = string.Empty;
        private float _evaluationResult;

        private FormulaCodeEditor _codeEditor;
        private VisualElement _inputsList;
        private Label _resultLabel;
        private HelpBox _statusBox;
        private ScrollView _scroll;
        private bool _autoResizing;

        private const float WindowMinHeight = 320f;
        private const float WindowMaxHeight = 1200f;

        [MenuItem("Tools/Formula Framework/Formula Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormulaBuilderWindow>("Formula Builder");
            window.minSize = new Vector2(520f, WindowMinHeight);
            window.maxSize = new Vector2(4096f, WindowMaxHeight);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
            {
                window.titleContent = new GUIContent("Formula Builder", icon);
            }
            window.Show();
        }


        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.AddToClassList("fb-root");

            LoadStyleSheet(root, TokensUssPath);
            LoadStyleSheet(root, EditorUssPath);
            LoadStyleSheet(root, WindowUssPath);

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("fb-scroll");
            root.Add(_scroll);

            BuildHeader(_scroll);
            BuildFormulaSection(_scroll);
            BuildInputsSection(_scroll);
            BuildEvaluationSection(_scroll);
            BuildStatusSection(_scroll);

            _scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);

            // Initial sizing once the first layout pass settles.
            rootVisualElement.schedule.Execute(ApplyAutoResize).ExecuteLater(16);
        }

        private void OnContentGeometryChanged(GeometryChangedEvent evt)
        {
            if (Mathf.Approximately(evt.newRect.height, evt.oldRect.height))
            {
                return;
            }
            ApplyAutoResize();
        }

        private static void LoadStyleSheet(VisualElement root, string path)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet != null)
            {
                root.styleSheets.Add(sheet);
            }
        }

        private void BuildHeader(VisualElement parent)
        {
            var header = new VisualElement();
            header.AddToClassList("fb-header");
            parent.Add(header);

            var icon = new VisualElement();
            icon.AddToClassList("fb-header__icon");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (tex != null)
            {
                icon.style.backgroundImage = new StyleBackground(tex);
            }
            header.Add(icon);

            var title = new Label("Formula Builder");
            title.AddToClassList("fb-title");
            header.Add(title);
        }

        private void BuildFormulaSection(VisualElement parent)
        {
            var box = MakeSection("Formula", out var header);
            parent.Add(box);

            header.Add(MakeButton("Examples", ShowExamplesMenu));
            header.Add(MakeButton("Functions", ShowFunctionsMenu));
            header.Add(MakeButton("Reference", FormulaReferenceWindow.ShowWindow));

            _codeEditor = new FormulaCodeEditor { Value = _formulaExpression };
            _codeEditor.Changed += value =>
            {
                _formulaExpression = value;
                SyncDetectedInputs();
                ValidateFormula();
            };
            box.Add(_codeEditor);

            ValidateFormula();
        }

        private void ValidateFormula()
        {
            if (_codeEditor == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_formulaExpression))
            {
                _codeEditor.ClearDiagnostics();
                return;
            }

            var parser = new FormulaParser();
            var result = parser.TryParse(_formulaExpression);
            if (result.IsSuccess)
            {
                _codeEditor.ClearDiagnostics();
                return;
            }

            string firstLine = ExtractFirstLine(result.ErrorMessage);
            var diagnostic = new FormulaDiagnostic(
                FormulaDiagnosticSeverity.Error,
                firstLine,
                result.ErrorLine,
                result.ErrorColumn);
            _codeEditor.SetDiagnostics(new[] { diagnostic });
        }

        private static string ExtractFirstLine(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return "Parse error";
            }
            int newline = message.IndexOf('\n');
            string firstLine = newline > 0 ? message.Substring(0, newline) : message;

            // Strip the "Parse error at line X, column Y: " prefix — the row position already
            // conveys that location, and the trailing detail is what the user wants to read.
            int colon = firstLine.IndexOf(": ", StringComparison.Ordinal);
            return colon > 0 ? firstLine.Substring(colon + 2) : firstLine;
        }

        private void BuildInputsSection(VisualElement parent)
        {
            var box = MakeSection("Inputs", out var header);
            parent.Add(box);

            header.Add(MakeButton("Clear", () =>
            {
                foreach (string key in _testInputs.Keys.ToArray())
                {
                    _testInputs[key] = 0f;
                }
                RefreshInputsList();
            }));
            header.Add(MakeButton("Random Values", () =>
            {
                ApplyRandomValues();
                RefreshInputsList();
            }));

            _inputsList = new VisualElement();
            _inputsList.AddToClassList("fb-input-list");
            box.Add(_inputsList);
            SyncDetectedInputs();
        }

        private void BuildEvaluationSection(VisualElement parent)
        {
            var box = MakeSection("Evaluation");
            parent.Add(box);

            var evalButton = new Button(EvaluateFormula) { text = "Evaluate" };
            evalButton.AddToClassList("fb-button");
            evalButton.AddToClassList("fb-button--primary");
            box.Add(evalButton);

            var resultLabel = new Label("Result");
            resultLabel.AddToClassList("fb-result-label");
            box.Add(resultLabel);

            _resultLabel = new Label(_evaluationResult.ToString("F4"));
            _resultLabel.AddToClassList("fb-result");
            box.Add(_resultLabel);
        }

        private void BuildStatusSection(VisualElement parent)
        {
            _statusBox = new HelpBox(string.Empty, HelpBoxMessageType.None);
            _statusBox.AddToClassList("fb-status");
            _statusBox.style.display = DisplayStyle.None;
            parent.Add(_statusBox);
        }

        private void RefreshInputsList()
        {
            if (_inputsList == null)
            {
                return;
            }

            _inputsList.Clear();

            if (_testInputs.Count == 0)
            {
                _inputsList.Add(new HelpBox("No inputs detected in the formula yet.", HelpBoxMessageType.Info));
                return;
            }

            foreach (string key in _testInputs.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                string capturedKey = key;
                var row = new VisualElement();
                row.AddToClassList("fb-input-row");

                var nameLabel = new Label(capturedKey);
                nameLabel.AddToClassList("fb-input-row__name");
                row.Add(nameLabel);

                var floatField = new FloatField { value = _testInputs[capturedKey] };
                floatField.AddToClassList("fb-input-row__value");
                floatField.RegisterValueChangedCallback(evt => _testInputs[capturedKey] = evt.newValue);
                row.Add(floatField);

                _inputsList.Add(row);
            }
        }

        private void ApplyAutoResize()
        {
            if (docked || _scroll == null || _autoResizing)
            {
                return;
            }

            float contentHeight = _scroll.contentContainer.layout.height;
            if (float.IsNaN(contentHeight) || contentHeight <= 0f)
            {
                return;
            }

            // ScrollView padding sits between rootVisualElement and contentContainer —
            // without this, the viewport ends up shorter than the content and a scrollbar
            // appears even after the resize.
            float scrollPadding = _scroll.resolvedStyle.paddingTop + _scroll.resolvedStyle.paddingBottom;
            if (float.IsNaN(scrollPadding) || scrollPadding < 0f)
            {
                scrollPadding = 0f;
            }

            // Window chrome (tab strip + borders) lives outside rootVisualElement.
            float chrome = position.height - rootVisualElement.layout.height;
            if (float.IsNaN(chrome) || chrome < 0f)
            {
                chrome = 0f;
            }

            // Small buffer to absorb sub-pixel rounding / focus rings.
            const float buffer = 2f;

            float desired = contentHeight + scrollPadding + chrome + buffer;
            float min = minSize.y > 0f ? minSize.y : WindowMinHeight;
            float max = maxSize.y > 0f ? maxSize.y : WindowMaxHeight;
            float clamped = Mathf.Clamp(desired, min, max);

            if (Mathf.Abs(clamped - position.height) < 1f)
            {
                return;
            }

            _autoResizing = true;
            try
            {
                var pos = position;
                pos.height = clamped;
                position = pos;
            }
            finally
            {
                _autoResizing = false;
            }
        }

        private void ApplyRandomValues()
        {
            foreach (string key in _testInputs.Keys.ToArray())
            {
                _testInputs[key] = UnityEngine.Random.Range(0f, 100f);
            }
        }

        private void SyncDetectedInputs()
        {
            if (_inputsList == null)
            {
                return;
            }

            var detected = DetectInputVariables(_formulaExpression);

            bool changed = false;

            foreach (string input in detected)
            {
                if (!_testInputs.ContainsKey(input))
                {
                    _testInputs[input] = 0f;
                    changed = true;
                }
            }

            foreach (string existing in _testInputs.Keys.ToArray())
            {
                if (!detected.Contains(existing))
                {
                    _testInputs.Remove(existing);
                    changed = true;
                }
            }

            if (changed)
            {
                RefreshInputsList();
            }
        }

        private static HashSet<string> DetectInputVariables(string source)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(source))
            {
                return result;
            }

            var tokens = FormulaTokenizer.Tokenize(source);
            var locals = new HashSet<string>(StringComparer.Ordinal);

            // First pass: identify local variables.
            // A name is local if it's introduced by `let <name>` OR is the LHS of an
            // assignment (`<name> =`, `+=`, `-=`, `*=`, `/=`). Comparison `==` is not
            // an assignment.
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Kind != FormulaTokenKind.Identifier && token.Kind != FormulaTokenKind.Keyword)
                {
                    continue;
                }

                string text = source.Substring(token.Start, token.Length);

                if (token.Kind == FormulaTokenKind.Keyword && text == "let")
                {
                    int next = NextSignificant(tokens, i);
                    if (next >= 0 && tokens[next].Kind == FormulaTokenKind.Identifier)
                    {
                        locals.Add(source.Substring(tokens[next].Start, tokens[next].Length));
                    }
                    continue;
                }

                if (token.Kind == FormulaTokenKind.Identifier)
                {
                    int next = NextSignificant(tokens, i);
                    if (next >= 0 && tokens[next].Kind == FormulaTokenKind.Operator)
                    {
                        string op = source.Substring(tokens[next].Start, tokens[next].Length);
                        if (op == "=" || op == "+=" || op == "-=" || op == "*=" || op == "/=")
                        {
                            locals.Add(text);
                        }
                    }
                }
            }

            // Second pass: every other identifier is an input.
            foreach (var token in tokens)
            {
                if (token.Kind != FormulaTokenKind.Identifier)
                {
                    continue;
                }
                string name = source.Substring(token.Start, token.Length);
                if (!locals.Contains(name))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        private static int NextSignificant(IReadOnlyList<FormulaToken> tokens, int from)
        {
            for (int i = from + 1; i < tokens.Count; i++)
            {
                var kind = tokens[i].Kind;
                if (kind != FormulaTokenKind.Whitespace && kind != FormulaTokenKind.Newline && kind != FormulaTokenKind.LineComment)
                {
                    return i;
                }
            }
            return -1;
        }

        private void EvaluateFormula()
        {
            if (string.IsNullOrWhiteSpace(_formulaExpression))
            {
                ShowStatus("Enter a formula expression first.", HelpBoxMessageType.Warning);
                return;
            }

            var parser = new FormulaParser();
            var parseResult = parser.TryParse(_formulaExpression);
            if (!parseResult.IsSuccess)
            {
                ShowStatus("Formula contains syntax errors.", HelpBoxMessageType.Error);
                return;
            }

            try
            {
                var localContext = new Dictionary<string, float>(_testInputs);
                _evaluationResult = parseResult.Formula.Evaluate(localContext);
                _resultLabel.text = _evaluationResult.ToString("F4");
                ShowStatus("Evaluation successful.", HelpBoxMessageType.Info);
            }
            catch (Exception ex)
            {
                ShowStatus($"Evaluation failed: {ex.Message}", HelpBoxMessageType.Error);
                _evaluationResult = 0f;
                _resultLabel.text = _evaluationResult.ToString("F4");
            }
        }

        private void ShowStatus(string message, HelpBoxMessageType type)
        {
            if (_statusBox == null)
            {
                return;
            }

            _statusBox.text = message;
            _statusBox.messageType = type;
            _statusBox.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void ShowExamplesMenu()
        {
            var menu = new GenericMenu();
            foreach (var group in Examples.GroupBy(e => e.Category))
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
            _codeEditor.Value = example.Expression;
            // _codeEditor.Value setter fires Changed → SyncDetectedInputs.
        }

        private void ShowFunctionsMenu()
        {
            var menu = new GenericMenu();
            foreach (var info in FormulaFunctions.All.Values.OrderBy(f => f.Name))
            {
                var captured = info;
                menu.AddItem(new GUIContent($"{captured.Name}\t{captured.Summary}"), false,
                    () => _codeEditor.InsertAtCaret(captured.Signature));
            }
            menu.ShowAsContext();
        }

        private static VisualElement MakeSection(string headingText)
        {
            return MakeSection(headingText, out _);
        }

        private static VisualElement MakeSection(string headingText, out VisualElement headerRow)
        {
            var section = new VisualElement();
            section.AddToClassList("fb-section");

            headerRow = new VisualElement();
            headerRow.AddToClassList("fb-section__header");

            var heading = new Label(headingText);
            heading.AddToClassList("fb-section__heading");
            headerRow.Add(heading);

            section.Add(headerRow);

            return section;
        }

        private static Button MakeButton(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("fb-button");
            return button;
        }

        private readonly struct FormulaExample
        {
            public string Category { get; }
            public string Name { get; }
            public string Expression { get; }

            public FormulaExample(string category, string name, string expression)
            {
                Category = category;
                Name = name;
                Expression = expression;
            }
        }
    }
}
