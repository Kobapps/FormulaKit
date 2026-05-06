using System;
using System.Collections.Generic;
using System.Text;
using FormulaKit.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace FormulaKit.Editor.Tools.UI
{
    public enum FormulaDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct FormulaDiagnostic
    {
        public int Line { get; }
        public int Column { get; }
        public FormulaDiagnosticSeverity Severity { get; }
        public string Message { get; }

        public FormulaDiagnostic(FormulaDiagnosticSeverity severity, string message, int line, int column)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            Line = Math.Max(1, line);
            Column = Math.Max(1, column);
        }
    }

    public sealed class FormulaCodeEditor : VisualElement
    {
        private const int MinLineCount = 6;
        private const float LineHeightPx = 16f;
        private const float ContentPaddingTop = 4f;
        private const float ContentPaddingLeft = 8f;
        private const int MaxUndoDepth = 200;
        private const double SnapshotIdleSeconds = 0.6;

        private readonly TextField _input;
        private readonly Label _highlight;
        private readonly VisualElement _gutter;
        private readonly VisualElement _inputArea;
        private readonly VisualElement _diagnosticsLayer;

        private string _value = string.Empty;
        private int _lastLineCount = -1;
        private TextElement _innerText;
        private IReadOnlyList<FormulaDiagnostic> _diagnostics;

        private readonly List<EditorSnapshot> _undoStack = new List<EditorSnapshot>();
        private readonly List<EditorSnapshot> _redoStack = new List<EditorSnapshot>();
        private EditorSnapshot _pendingSnapshot;
        private double _lastSnapshotTime;
        private bool _suppressSnapshot;

        public event Action<string> Changed;
        public event Action LineCountChanged;

        public FormulaCodeEditor()
        {
            AddToClassList("formula-code-editor");

            _gutter = new VisualElement();
            _gutter.AddToClassList("formula-code-editor__gutter");
            Add(_gutter);

            _inputArea = new VisualElement();
            _inputArea.AddToClassList("formula-code-editor__input-area");
            Add(_inputArea);

            _highlight = new Label
            {
                enableRichText = true,
                pickingMode = PickingMode.Ignore
            };
            _highlight.AddToClassList("formula-code-editor__highlight");
            _inputArea.Add(_highlight);

            _input = new TextField
            {
                multiline = true,
                isDelayed = false,
                selectAllOnFocus = false,
                selectAllOnMouseUp = false
            };
            _input.AddToClassList("formula-code-editor__input");
            _input.RegisterCallback<AttachToPanelEvent>(_ => OnInputAttached());
            _input.RegisterValueChangedCallback(OnValueChanged);
            _input.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _inputArea.Add(_input);

            _pendingSnapshot = new EditorSnapshot(string.Empty, 0, 0);
            _lastSnapshotTime = EditorTimeNow();

            _diagnosticsLayer = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            _diagnosticsLayer.AddToClassList("formula-code-editor__diagnostics");
            _inputArea.Add(_diagnosticsLayer);

            Refresh();
        }

        public void SetDiagnostics(IReadOnlyList<FormulaDiagnostic> diagnostics)
        {
            _diagnostics = diagnostics;
            RefreshDiagnostics();
        }

        public void ClearDiagnostics()
        {
            _diagnostics = null;
            RefreshDiagnostics();
        }

        private void RefreshDiagnostics()
        {
            _diagnosticsLayer.Clear();
            if (_diagnostics == null || _diagnostics.Count == 0)
            {
                return;
            }

            float lineHeight = MeasureLineHeight();

            foreach (var diag in _diagnostics)
            {
                float top = MeasureLineTop(diag.Line, lineHeight);

                var row = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };
                row.AddToClassList("formula-code-editor__diagnostic-row");
                if (diag.Severity == FormulaDiagnosticSeverity.Error)
                {
                    row.AddToClassList("formula-code-editor__diagnostic-row--error");
                }
                else if (diag.Severity == FormulaDiagnosticSeverity.Warning)
                {
                    row.AddToClassList("formula-code-editor__diagnostic-row--warning");
                }
                row.style.top = top;
                row.style.height = lineHeight;

                if (diag.Column > 0)
                {
                    var caretMarker = new Label("|")
                    {
                        pickingMode = PickingMode.Ignore
                    };
                    caretMarker.AddToClassList("formula-code-editor__diagnostic-caret");
                    if (diag.Severity == FormulaDiagnosticSeverity.Error)
                    {
                        caretMarker.AddToClassList("formula-code-editor__diagnostic-caret--error");
                    }
                    else if (diag.Severity == FormulaDiagnosticSeverity.Warning)
                    {
                        caretMarker.AddToClassList("formula-code-editor__diagnostic-caret--warning");
                    }
                    caretMarker.style.left = MeasureColumnX(diag.Line, diag.Column);
                    row.Add(caretMarker);
                }

                var label = new Label(diag.Message)
                {
                    pickingMode = PickingMode.Ignore,
                    tooltip = diag.Message
                };
                label.AddToClassList("formula-code-editor__diagnostic-label");
                row.Add(label);

                _diagnosticsLayer.Add(row);
            }
        }

        private float MeasureLineHeight()
        {
            if (_innerText == null)
            {
                return LineHeightPx;
            }
            var size = _innerText.MeasureTextSize("Mg", 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            return size.y > 0 ? size.y : LineHeightPx;
        }

        private float MeasureLineTop(int line, float lineHeight)
        {
            if (line <= 1 || _innerText == null || string.IsNullOrEmpty(_value))
            {
                return ContentPaddingTop;
            }

            string[] lines = _value.Split('\n');
            int linesBefore = Math.Min(line - 1, lines.Length);
            if (linesBefore <= 0)
            {
                return ContentPaddingTop;
            }

            // Sum of measured line heights for the lines BEFORE the target line.
            // Per-line measurement is more reliable than measuring a multi-line block,
            // because trailing newlines in MeasureTextSize are inconsistent.
            float y = ContentPaddingTop;
            for (int i = 0; i < linesBefore; i++)
            {
                string text = lines[i];
                if (string.IsNullOrEmpty(text))
                {
                    y += lineHeight;
                    continue;
                }
                var size = _innerText.MeasureTextSize(text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
                y += size.y > 0 ? size.y : lineHeight;
            }
            return y;
        }

        private float MeasureColumnX(int line, int column)
        {
            if (_innerText == null || column <= 1 || string.IsNullOrEmpty(_value))
            {
                return ContentPaddingLeft;
            }

            string[] lines = _value.Split('\n');
            int lineIdx = Math.Max(0, Math.Min(line - 1, lines.Length - 1));
            string lineText = lines[lineIdx];
            int colIdx = Math.Max(0, Math.Min(column - 1, lineText.Length));
            if (colIdx <= 0)
            {
                return ContentPaddingLeft;
            }

            string prefix = lineText.Substring(0, colIdx);
            var size = _innerText.MeasureTextSize(prefix, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            return ContentPaddingLeft + size.x;
        }

        private void OnInputAttached()
        {
            _innerText = _input.Q<TextElement>();
            if (_innerText != null)
            {
                // Hide the TextField glyphs — cursor and selection are drawn separately.
                _innerText.style.color = new Color(1f, 1f, 1f, 0f);
            }
            Refresh();
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            _value = evt.newValue ?? string.Empty;
            MaybeRecordSnapshot();
            Refresh();
            Changed?.Invoke(_value);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!evt.actionKey)
            {
                return;
            }

            if (evt.keyCode == KeyCode.Z)
            {
                bool ok = evt.shiftKey ? Redo() : Undo();
                if (ok)
                {
                    evt.StopPropagation();
                }
                return;
            }

            if (evt.keyCode == KeyCode.Y)
            {
                if (Redo())
                {
                    evt.StopPropagation();
                }
            }
        }

        public bool IsReadOnly
        {
            get => _input.isReadOnly;
            set
            {
                _input.isReadOnly = value;
                EnableInClassList("formula-code-editor--readonly", value);
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                value ??= string.Empty;
                if (_value == value)
                {
                    return;
                }
                _value = value;
                _suppressSnapshot = true;
                _input.SetValueWithoutNotify(value);
                _suppressSnapshot = false;
                _undoStack.Clear();
                _redoStack.Clear();
                _pendingSnapshot = new EditorSnapshot(value, value.Length, value.Length);
                _lastSnapshotTime = EditorTimeNow();
                Refresh();
                Changed?.Invoke(_value);
            }
        }

        public new void Focus()
        {
            _input.Focus();
        }

        public void InsertAtCaret(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            CommitPendingSnapshot();

            int selectStart = Mathf.Clamp(Math.Min(_input.cursorIndex, _input.selectIndex), 0, _value.Length);
            int selectEnd = Mathf.Clamp(Math.Max(_input.cursorIndex, _input.selectIndex), 0, _value.Length);

            string before = _value.Substring(0, selectStart);
            string after = _value.Substring(selectEnd);
            string next = before + text + after;

            _value = next;
            _suppressSnapshot = true;
            _input.SetValueWithoutNotify(next);
            _suppressSnapshot = false;

            int newCaret = selectStart + text.Length;
            _input.cursorIndex = newCaret;
            _input.selectIndex = newCaret;

            _pendingSnapshot = new EditorSnapshot(_value, newCaret, newCaret);
            CommitPendingSnapshot();
            _lastSnapshotTime = EditorTimeNow();

            Refresh();
            Changed?.Invoke(_value);
            _input.Focus();
        }

        private void Refresh()
        {
            int textLineCount = CountLines(_value);
            int displayLineCount = Math.Max(MinLineCount, textLineCount);

            float pinnedHeight = displayLineCount * LineHeightPx;
            _input.style.minHeight = pinnedHeight;
            if (_innerText != null)
            {
                _innerText.style.minHeight = pinnedHeight;
            }

            _highlight.text = BuildHighlightedMarkup(_value);
            UpdateGutter(displayLineCount);
        }

        private void UpdateGutter(int lineCount)
        {
            if (lineCount == _lastLineCount)
            {
                return;
            }

            _lastLineCount = lineCount;
            _gutter.Clear();
            for (int i = 1; i <= lineCount; i++)
            {
                var lineLabel = new Label(i.ToString());
                lineLabel.AddToClassList("formula-code-editor__gutter-line");
                _gutter.Add(lineLabel);
            }
            LineCountChanged?.Invoke();
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 1;
            }

            int count = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    count++;
                }
            }
            return count;
        }

        private void MaybeRecordSnapshot()
        {
            if (_suppressSnapshot)
            {
                return;
            }

            double now = EditorTimeNow();
            bool boundary = LastEditCrossesBoundary(_pendingSnapshot.Text, _value);
            bool idle = now - _lastSnapshotTime >= SnapshotIdleSeconds;

            if (idle || boundary || _undoStack.Count == 0)
            {
                CommitPendingSnapshot();
            }

            _pendingSnapshot = new EditorSnapshot(_value, _input.cursorIndex, _input.selectIndex);
            _lastSnapshotTime = now;
            _redoStack.Clear();
        }

        private void CommitPendingSnapshot()
        {
            if (_undoStack.Count > 0 && string.Equals(_undoStack[_undoStack.Count - 1].Text, _pendingSnapshot.Text, StringComparison.Ordinal))
            {
                return;
            }
            _undoStack.Add(_pendingSnapshot);
            if (_undoStack.Count > MaxUndoDepth)
            {
                _undoStack.RemoveAt(0);
            }
        }

        private bool Undo()
        {
            // Make sure the latest edit is in the stack so we can step back from it.
            if (_undoStack.Count == 0 ||
                !string.Equals(_undoStack[_undoStack.Count - 1].Text, _value, StringComparison.Ordinal))
            {
                CommitPendingSnapshot();
                _pendingSnapshot = new EditorSnapshot(_value, _input.cursorIndex, _input.selectIndex);
            }

            if (_undoStack.Count <= 1)
            {
                return false;
            }

            var current = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _redoStack.Add(current);

            var target = _undoStack[_undoStack.Count - 1];
            ApplySnapshot(target);
            _pendingSnapshot = target;
            _lastSnapshotTime = EditorTimeNow();
            return true;
        }

        private bool Redo()
        {
            if (_redoStack.Count == 0)
            {
                return false;
            }
            var target = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _undoStack.Add(target);
            ApplySnapshot(target);
            _pendingSnapshot = target;
            _lastSnapshotTime = EditorTimeNow();
            return true;
        }

        private void ApplySnapshot(EditorSnapshot snapshot)
        {
            _value = snapshot.Text;
            _suppressSnapshot = true;
            _input.SetValueWithoutNotify(snapshot.Text);
            _suppressSnapshot = false;

            int caret = Mathf.Clamp(snapshot.Caret, 0, _value.Length);
            int select = Mathf.Clamp(snapshot.Select, 0, _value.Length);
            _input.cursorIndex = caret;
            _input.selectIndex = select;

            Refresh();
            Changed?.Invoke(_value);
        }

        private static bool LastEditCrossesBoundary(string previous, string current)
        {
            if (previous == null || current == null)
            {
                return true;
            }
            int diff = current.Length - previous.Length;
            if (Math.Abs(diff) > 1)
            {
                return true;
            }
            if (diff == 1)
            {
                int idx = current.Length - 1;
                int limit = Math.Min(previous.Length, current.Length);
                for (int i = 0; i < limit; i++)
                {
                    if (previous[i] != current[i])
                    {
                        idx = i;
                        break;
                    }
                }
                char inserted = current[Mathf.Clamp(idx, 0, current.Length - 1)];
                return inserted == ' ' || inserted == '\n' || inserted == '\t';
            }
            return false;
        }

        private static double EditorTimeNow()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.timeSinceStartup;
#else
            return Time.realtimeSinceStartupAsDouble;
#endif
        }

        private readonly struct EditorSnapshot
        {
            public string Text { get; }
            public int Caret { get; }
            public int Select { get; }

            public EditorSnapshot(string text, int caret, int select)
            {
                Text = text ?? string.Empty;
                Caret = caret;
                Select = select;
            }
        }

        private static string BuildHighlightedMarkup(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            var tokens = FormulaTokenizer.Tokenize(source);
            var builder = new StringBuilder(source.Length + 32 * tokens.Count);

            foreach (var token in tokens)
            {
                string slice = source.Substring(token.Start, token.Length);
                string color = ColorFor(token.Kind);
                if (color == null)
                {
                    AppendEscaped(builder, slice);
                }
                else
                {
                    builder.Append("<color=#").Append(color).Append('>');
                    AppendEscaped(builder, slice);
                    builder.Append("</color>");
                }
            }

            return builder.ToString();
        }

        private static void AppendEscaped(StringBuilder builder, string text)
        {
            foreach (char c in text)
            {
                switch (c)
                {
                    case '<': builder.Append("<noparse><</noparse>"); break;
                    default: builder.Append(c); break;
                }
            }
        }

        private static string ColorFor(FormulaTokenKind kind)
        {
            switch (kind)
            {
                case FormulaTokenKind.Keyword:      return "569CD6";
                case FormulaTokenKind.FunctionName: return "DCDCAA";
                case FormulaTokenKind.Number:       return "B5CEA8";
                case FormulaTokenKind.Operator:     return "D4D4D4";
                case FormulaTokenKind.LineComment:  return "6A9955";
                case FormulaTokenKind.Punctuation:  return "B0B0B0";
                default: return null;
            }
        }
    }
}
