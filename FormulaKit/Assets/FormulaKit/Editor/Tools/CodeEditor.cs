using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FormulaKit.Editor.Tools.Tools
{
    internal sealed class CodeEditor
    {
        private const float MinimumContentWidth = 200f;
        private const float ScrollbarWidth = 16f;

        private readonly string _controlName;
        private readonly GUIContent _textContent = new GUIContent();
        private readonly GUIContent _highlightContent = new GUIContent();

        private GUIStyle _textStyle;
        private GUIStyle _highlightStyle;
        private GUIStyle _lineNumberStyle;
        private Texture2D _lineBackgroundTexture;

        private Vector2 _scroll;
        private bool _pendingFocus;
        private int? _pendingCursorIndex;
        private int? _pendingSelectIndex;
        private int _cursorIndex;
        private int _selectIndex;
        private bool _hasFocus;

        private string _cachedText = string.Empty;
        private int[] _lineStarts = Array.Empty<int>();
        private float _lineHeight;

        private string _highlightedText = string.Empty;

        public string Text { get; set; } = string.Empty;
        public Func<string, string> Highlighter { get; set; }
        public event Action<string> TextChanged;

        public CodeEditor(string controlName)
        {
            _controlName = controlName;
        }

        public void RequestFocus()
        {
            _pendingFocus = true;
        }

        public void SetCursor(int cursorIndex)
        {
            _pendingCursorIndex = cursorIndex;
            _pendingSelectIndex = cursorIndex;
        }

        public void InsertText(string text)
        {
            if (text == null)
            {
                text = string.Empty;
            }

            string current = Text ?? string.Empty;
            int start = Mathf.Min(_cursorIndex, _selectIndex);
            int end = Mathf.Max(_cursorIndex, _selectIndex);
            start = Mathf.Clamp(start, 0, current.Length);
            end = Mathf.Clamp(end, 0, current.Length);

            string before = current.Substring(0, start);
            string after = current.Substring(end);
            string updated = before + text + after;
            Text = updated;
            _pendingCursorIndex = before.Length + text.Length;
            _pendingSelectIndex = _pendingCursorIndex;
            _pendingFocus = true;
            TextChanged?.Invoke(Text);
        }

        public void OnGUI(Rect rect)
        {
            EnsureStyles();

            string rawText = Text ?? string.Empty;
            EnsureLineCache(rawText);

            _highlightedText = Highlighter != null ? Highlighter(rawText) : rawText;
            if (string.IsNullOrEmpty(_highlightedText))
            {
                _highlightedText = "\u200B"; // zero-width space to keep height consistent
            }

            int lineCount = Mathf.Max(1, _lineStarts.Length);
            float gutterWidth = CalculateGutterWidth(lineCount);
            float contentWidth = Mathf.Max(MinimumContentWidth, rect.width - gutterWidth - ScrollbarWidth);
            float contentHeight = Mathf.Max(rect.height, CalculateContentHeight(rawText, contentWidth));

            Rect viewRect = new Rect(0f, 0f, gutterWidth + contentWidth, contentHeight);
            Rect gutterRect = new Rect(0f, 0f, gutterWidth, contentHeight);
            Rect textRect = new Rect(gutterWidth, 0f, contentWidth, contentHeight);

            GUI.BeginGroup(rect);
            _scroll = GUI.BeginScrollView(new Rect(0f, 0f, rect.width, rect.height), _scroll, viewRect);

            DrawLineNumbers(gutterRect, lineCount);

            GUI.SetNextControlName(_controlName);
            string newText = GUI.TextArea(textRect, rawText, _textStyle);
            if (!ReferenceEquals(newText, rawText))
            {
                if (!string.Equals(newText, rawText, StringComparison.Ordinal))
                {
                    Text = newText;
                    TextChanged?.Invoke(Text);
                    EnsureLineCache(Text ?? string.Empty);
                }
            }

            HandleFocus();

            if (Event.current.type == EventType.Repaint)
            {
                DrawHighlight(textRect);
                DrawSelection(textRect);
                DrawCaret(textRect);
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void HandleFocus()
        {
            _hasFocus = GUI.GetNameOfFocusedControl() == _controlName;

            if (_pendingFocus)
            {
                GUI.FocusControl(_controlName);
                _hasFocus = true;
                _pendingFocus = false;
            }

            if (_hasFocus)
            {
                if (GUIUtility.keyboardControl != 0)
                {
                    var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    if (editor != null)
                    {
                        if (_pendingCursorIndex.HasValue)
                        {
                            editor.cursorIndex = _pendingCursorIndex.Value;
                            editor.selectIndex = _pendingSelectIndex ?? _pendingCursorIndex.Value;
                            _pendingCursorIndex = null;
                            _pendingSelectIndex = null;
                        }

                        _cursorIndex = editor.cursorIndex;
                        _selectIndex = editor.selectIndex;
                    }
                }
            }
        }

        private void DrawLineNumbers(Rect rect, int lineCount)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (_lineBackgroundTexture == null)
            {
                _lineBackgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _lineBackgroundTexture.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f));
                _lineBackgroundTexture.Apply();
            }

            GUI.DrawTexture(rect, _lineBackgroundTexture, ScaleMode.StretchToFill);

            using (new GUI.GroupScope(rect))
            {
                float y = _textStyle.padding.top;
                for (int i = 0; i < lineCount; i++)
                {
                    Rect labelRect = new Rect(0f, y - 2f, rect.width - 4f, _lineHeight);
                    GUI.Label(labelRect, (i + 1).ToString(), _lineNumberStyle);
                    y += _lineHeight;
                }
            }
        }

        private void DrawHighlight(Rect textRect)
        {
            _highlightContent.text = _highlightedText;
            _highlightStyle.Draw(textRect, _highlightContent, false, false, _hasFocus, false);
        }

        private void DrawSelection(Rect textRect)
        {
            if (!_hasFocus)
            {
                return;
            }

            int start = Mathf.Min(_cursorIndex, _selectIndex);
            int end = Mathf.Max(_cursorIndex, _selectIndex);
            if (start == end)
            {
                return;
            }

            string text = Text ?? string.Empty;
            if (text.Length == 0)
            {
                return;
            }

            EnsureLineCache(text);

            var content = new GUIContent(text);
            var selectionColor = new Color(0.24f, 0.45f, 0.82f, 0.35f);

            int startLine = GetLineForIndex(start);
            int endLine = GetLineForIndex(Mathf.Max(0, end - 1));

            for (int line = startLine; line <= endLine; line++)
            {
                int lineStart = _lineStarts[line];
                int lineEnd = GetLineEndIndex(line, text);

                int selectionStart = Mathf.Max(start, lineStart);
                int selectionEnd = Mathf.Min(end, lineEnd);

                Vector2 startPos = _textStyle.GetCursorPixelPosition(textRect, content, selectionStart);
                Vector2 endPos = _textStyle.GetCursorPixelPosition(textRect, content, selectionEnd);

                if (line < endLine && selectionEnd >= lineEnd)
                {
                    Vector2 lineEndPos = _textStyle.GetCursorPixelPosition(textRect, content, lineEnd);
                    endPos.x = lineEndPos.x;
                }

                float width = Mathf.Max(2f, endPos.x - startPos.x);
                Rect highlightRect = new Rect(textRect.x + startPos.x, textRect.y + startPos.y, width, _lineHeight);
                EditorGUI.DrawRect(highlightRect, selectionColor);
            }
        }

        private void DrawCaret(Rect textRect)
        {
            if (!_hasFocus)
            {
                return;
            }

            string text = Text ?? string.Empty;
            var content = new GUIContent(text);
            Vector2 caretPosition = _textStyle.GetCursorPixelPosition(textRect, content, _cursorIndex);
            Rect caretRect = new Rect(textRect.x + caretPosition.x, textRect.y + caretPosition.y, 1f, _lineHeight);
            EditorGUI.DrawRect(caretRect, new Color(0.9f, 0.9f, 0.9f));
        }

        private void EnsureLineCache(string text)
        {
            if (text == null)
            {
                text = string.Empty;
            }

            if (string.Equals(text, _cachedText, StringComparison.Ordinal))
            {
                return;
            }

            _cachedText = text;
            List<int> starts = new List<int> { 0 };
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    starts.Add(i + 1);
                }
            }

            _lineStarts = starts.ToArray();
            _lineHeight = Mathf.Ceil(_textStyle.lineHeight);
            if (_lineHeight <= 0f)
            {
                _lineHeight = _textStyle.CalcSize(new GUIContent("A")).y + 2f;
            }
        }

        private float CalculateGutterWidth(int lineCount)
        {
            string label = lineCount.ToString();
            Vector2 size = _lineNumberStyle.CalcSize(new GUIContent(label));
            return Mathf.Max(40f, size.x + 12f);
        }

        private float CalculateContentHeight(string text, float width)
        {
            _textContent.text = text;
            float height = _textStyle.CalcHeight(_textContent, width);
            return Mathf.Max(height + 8f, _lineHeight * Mathf.Max(1, _lineStarts.Length) + _textStyle.padding.vertical + 8f);
        }

        private int GetLineForIndex(int index)
        {
            if (_lineStarts.Length == 0)
            {
                return 0;
            }

            int low = 0;
            int high = _lineStarts.Length - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                int start = _lineStarts[mid];
                int nextStart = mid + 1 < _lineStarts.Length ? _lineStarts[mid + 1] : int.MaxValue;
                if (index < start)
                {
                    high = mid - 1;
                }
                else if (index >= nextStart)
                {
                    low = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            return Mathf.Clamp(low, 0, _lineStarts.Length - 1);
        }

        private int GetLineEndIndex(int line, string text)
        {
            int textLength = text.Length;
            int nextStart = line + 1 < _lineStarts.Length ? _lineStarts[line + 1] : textLength;
            int end = nextStart;
            while (end > _lineStarts[line] && (text[end - 1] == '\n' || text[end - 1] == '\r'))
            {
                end--;
            }

            return Mathf.Max(_lineStarts[line], end);
        }

        private void EnsureStyles()
        {
            if (_textStyle != null)
            {
                return;
            }

            _textStyle = new GUIStyle(EditorStyles.textArea)
            {
                richText = false,
                wordWrap = false,
                stretchHeight = true
            };
            _textStyle.normal.textColor = new Color(0f, 0f, 0f, 0f);
            _textStyle.focused.textColor = new Color(0f, 0f, 0f, 0f);
            _textStyle.hover.textColor = new Color(0f, 0f, 0f, 0f);
            _textStyle.active.textColor = new Color(0f, 0f, 0f, 0f);

            _highlightStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = false,
                stretchHeight = true,
                alignment = TextAnchor.UpperLeft
            };
            _highlightStyle.padding = _textStyle.padding;
            _highlightStyle.margin = _textStyle.margin;
            _highlightStyle.font = _textStyle.font;
            _highlightStyle.fontSize = _textStyle.fontSize;
            _highlightStyle.normal.textColor = new Color(0.82f, 0.82f, 0.82f);

            _lineNumberStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperRight,
                fontSize = _textStyle.fontSize,
                font = _textStyle.font,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
        }
    }
}
