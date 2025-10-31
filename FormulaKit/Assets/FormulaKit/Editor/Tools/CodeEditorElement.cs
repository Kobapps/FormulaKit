using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FormulaKit.Editor.Tools.Tools
{
    internal sealed class CodeEditorElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<CodeEditorElement> { }

        private readonly ScrollView _scrollView;
        private readonly Label _lineNumbers;
        private readonly VisualElement _editorLayer;
        private readonly Label _highlightOverlay;
        private readonly TextField _textField;

        private readonly StringBuilder _lineBuilder = new StringBuilder();

        private Func<string, string> _highlighter;
        private bool _suppressNotify;

        public event Action<string> TextChanged;

        public CodeEditorElement()
        {
            AddToClassList("code-editor");

            _scrollView = new ScrollView(ScrollViewMode.Both)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Auto,
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            _scrollView.AddToClassList("code-editor__scroll");
            hierarchy.Add(_scrollView);

            var content = new VisualElement();
            content.AddToClassList("code-editor__content");
            _scrollView.Add(content);

            _lineNumbers = new Label
            {
                pickingMode = PickingMode.Ignore
            };
            _lineNumbers.AddToClassList("code-editor__gutter");
            content.Add(_lineNumbers);

            _editorLayer = new VisualElement();
            _editorLayer.AddToClassList("code-editor__layer");
            content.Add(_editorLayer);

            _highlightOverlay = new Label
            {
                pickingMode = PickingMode.Ignore,
                enableRichText = true
            };
            _highlightOverlay.AddToClassList("code-editor__highlight");
            _editorLayer.Add(_highlightOverlay);

            _textField = new TextField
            {
                multiline = true
            };
            _textField.AddToClassList("code-editor__input");
            _textField.RegisterValueChangedCallback(OnTextChanged);
            _editorLayer.Add(_textField);

            RegisterCallback<AttachToPanelEvent>(OnAttached);
            RegisterCallback<DetachFromPanelEvent>(OnDetached);
        }

        public string Text
        {
            get => _textField.value;
            set => SetText(value, true);
        }

        public void SetHighlighter(Func<string, string> highlighter)
        {
            _highlighter = highlighter;
            UpdateHighlight();
        }

        public void FocusInput()
        {
            _textField.Focus();
        }

        public void SetTextWithoutNotify(string text)
        {
            SetText(text, false);
        }

        public void InsertSnippet(string snippet)
        {
            if (snippet == null)
            {
                return;
            }

            var engine = _textField?.editorEngine;
            string current = _textField.value ?? string.Empty;
            if (engine == null)
            {
                _textField.value = current + snippet;
                return;
            }

            int start = Math.Min(engine.cursorIndex, engine.selectIndex);
            int end = Math.Max(engine.cursorIndex, engine.selectIndex);
            start = Mathf.Clamp(start, 0, current.Length);
            end = Mathf.Clamp(end, 0, current.Length);

            string before = current.Substring(0, start);
            string after = current.Substring(end);
            string updated = before + snippet + after;

            _textField.value = updated;

            int newIndex = before.Length + snippet.Length;
            engine.SelectRange(newIndex, newIndex);
            _textField.schedule.Execute(UpdateHighlight);
        }

        private void OnAttached(AttachToPanelEvent evt)
        {
            ConfigureTextInput();
            UpdateHighlight();
            UpdateLineNumbers();
        }

        private void OnDetached(DetachFromPanelEvent evt)
        {
            TextChanged = null;
        }

        private void ConfigureTextInput()
        {
            var textInput = _textField.Q<TextElement>(TextField.textInputUssName);
            if (textInput == null)
            {
                return;
            }

            Font editorFont = EditorStyles.textArea.font;
            _highlightOverlay.style.unityFont = editorFont;
            _highlightOverlay.style.fontSize = 12;
            _lineNumbers.style.unityFont = editorFont;
            _lineNumbers.style.fontSize = 12;
            textInput.style.unityFont = editorFont;
            textInput.style.whiteSpace = WhiteSpace.Pre;
            textInput.style.unityTextAlign = TextAnchor.UpperLeft;
            textInput.style.backgroundColor = Color.clear;
            textInput.style.color = new Color(0, 0, 0, 0);
            textInput.style.unityFontStyleAndWeight = FontStyle.Normal;
            textInput.style.fontSize = 12;
            textInput.style.paddingLeft = 0;
            textInput.style.paddingRight = 0;
            textInput.style.paddingTop = 0;
            textInput.style.paddingBottom = 0;
            textInput.style.marginLeft = 0;
            textInput.style.marginRight = 0;
            textInput.style.marginTop = 0;
            textInput.style.marginBottom = 0;

            if (textInput is ITextSelection selection)
            {
                selection.selectionColor = new Color(0.24f, 0.49f, 0.9f, 0.35f);
                selection.cursorColor = Color.white;
            }
        }

        private void SetText(string text, bool notify)
        {
            text ??= string.Empty;
            if (notify)
            {
                _textField.value = text;
            }
            else
            {
                _suppressNotify = true;
                _textField.SetValueWithoutNotify(text);
                _suppressNotify = false;
                UpdateHighlight();
                UpdateLineNumbers();
            }
        }

        private void OnTextChanged(ChangeEvent<string> evt)
        {
            if (_suppressNotify)
            {
                return;
            }

            UpdateHighlight();
            UpdateLineNumbers();
            TextChanged?.Invoke(evt.newValue ?? string.Empty);
        }

        private void UpdateHighlight()
        {
            string text = _textField.value ?? string.Empty;
            if (_highlighter != null)
            {
                _highlightOverlay.text = _highlighter(text);
            }
            else
            {
                _highlightOverlay.text = text;
            }
        }

        private void UpdateLineNumbers()
        {
            string text = _textField.value ?? string.Empty;
            int lines = 1;
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    lines++;
                }
            }

            _lineBuilder.Clear();
            for (int i = 1; i <= lines; i++)
            {
                _lineBuilder.Append(i);
                if (i < lines)
                {
                    _lineBuilder.Append('\n');
                }
            }

            _lineNumbers.text = _lineBuilder.ToString();
        }
    }
}
