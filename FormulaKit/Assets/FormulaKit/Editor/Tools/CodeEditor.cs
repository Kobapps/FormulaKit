using System;
using UnityEditor;
using UnityEngine;

namespace FormulaKit.Editor.Tools.Tools
{
    /// <summary>
    /// Lightweight code editor widget with syntax highlighting, line numbers,
    /// custom selection rendering, and caret drawing. Inspired by the
    /// UnityCodeEditor project but tailored for the Formula Builder window.
    /// </summary>
    internal sealed class CodeEditor : IDisposable
    {
        private const float LineNumberWidth = 40f;

        private readonly string _controlName;
        private readonly Func<string, string> _highlight;

        private GUIStyle _inputStyle;
        private GUIStyle _overlayStyle;
        private GUIStyle _lineNumberStyle;
        private bool _stylesInitialized;

        private readonly GUIContent _cursorContent = new GUIContent();
        private string _cachedHighlightSource;
        private string _cachedHighlightResult;
        private Vector2 _lastScrollPosition = new Vector2(float.MinValue, float.MinValue);
        private int _lastCursorIndex = -1;
        private int _lastSelectIndex = -1;
        private bool _hadFocusLastFrame;
        private bool _caretVisible = true;
        private double _nextCaretBlink;

        private const double CaretBlinkInterval = 0.55;

        private readonly Color _backgroundColor = new Color(0.13f, 0.13f, 0.13f);
        private readonly Color _lineBackgroundColor = new Color(0.16f, 0.16f, 0.16f);
        private readonly Color _selectionColor = new Color(0.24f, 0.49f, 0.90f, 0.35f);
        private readonly Color _caretColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);

        public CodeEditor(string controlName, Func<string, string> highlight)
        {
            _controlName = controlName;
            _highlight = highlight;
        }

        public bool Draw(ref string text, ref Vector2 scrollPosition, float minHeight)
        {
            EnsureStyles();

            text ??= string.Empty;
            _cursorContent.text = text;

            Rect outerRect = GUILayoutUtility.GetRect(0f, 10000f, minHeight, float.MaxValue, GUILayout.ExpandHeight(true));
            if (outerRect.width <= 0f)
            {
                return false;
            }

            float lineHeight = GetLineHeight();
            int lineCount = Mathf.Max(1, GetLineCount(text));
            float contentHeight = Mathf.Max(minHeight, lineCount * lineHeight + _inputStyle.padding.vertical + 6f);
            float maxLineWidth = CalculateMaxLineWidth(text);
            float contentWidth = Mathf.Max(outerRect.width - 16f, maxLineWidth + LineNumberWidth + _inputStyle.padding.horizontal + 8f);

            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);
            Rect numberRect = new Rect(0f, 0f, LineNumberWidth, viewRect.height);
            Rect textRect = new Rect(LineNumberWidth, 0f, viewRect.width - LineNumberWidth, viewRect.height);

            Vector2 newScroll = GUI.BeginScrollView(outerRect, scrollPosition, viewRect, true, true);
            bool scrollChanged = newScroll != _lastScrollPosition;
            scrollPosition = newScroll;
            _lastScrollPosition = newScroll;

            EditorGUI.DrawRect(viewRect, _backgroundColor);
            EditorGUI.DrawRect(numberRect, _lineBackgroundColor);

            Color previousCursor = GUI.skin.settings.cursorColor;
            Color previousSelection = GUI.skin.settings.selectionColor;
            GUI.skin.settings.cursorColor = Color.clear;
            GUI.skin.settings.selectionColor = Color.clear;

            GUI.SetNextControlName(_controlName);
            EditorGUI.BeginChangeCheck();
            string newValue = EditorGUI.TextArea(textRect, text, _inputStyle);
            bool changed = false;
            if (EditorGUI.EndChangeCheck())
            {
                text = newValue;
                _cursorContent.text = text;
                changed = true;
                _cachedHighlightSource = null;
            }

            TextEditor editor = null;
            bool hasFocus = GUI.GetNameOfFocusedControl() == _controlName;
            if (hasFocus)
            {
                editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (editor != null)
                {
                    if (editor.cursorIndex != _lastCursorIndex || editor.selectIndex != _lastSelectIndex)
                    {
                        _lastCursorIndex = editor.cursorIndex;
                        _lastSelectIndex = editor.selectIndex;
                        changed = true;
                    }
                }

                double time = EditorApplication.timeSinceStartup;
                if (!_hadFocusLastFrame)
                {
                    _caretVisible = true;
                    _nextCaretBlink = time + CaretBlinkInterval;
                    changed = true;
                }
                else if (time >= _nextCaretBlink)
                {
                    _caretVisible = !_caretVisible;
                    _nextCaretBlink = time + CaretBlinkInterval;
                    changed = true;
                }
            }
            else
            {
                if (_lastCursorIndex != -1 || _lastSelectIndex != -1)
                {
                    _lastCursorIndex = -1;
                    _lastSelectIndex = -1;
                    changed = true;
                }

                if (_hadFocusLastFrame)
                {
                    _caretVisible = false;
                    changed = true;
                }
            }

            _hadFocusLastFrame = hasFocus;

            GUI.skin.settings.cursorColor = previousCursor;
            GUI.skin.settings.selectionColor = previousSelection;

            if (Event.current.type == EventType.Repaint)
            {
                DrawLineNumbers(numberRect, lineHeight, text);

                Color previousColor = GUI.color;
                GUI.color = Color.white;
                GUI.Label(textRect, GetHighlightedText(text), _overlayStyle);
                GUI.color = previousColor;

                if (editor != null)
                {
                    DrawSelectionOverlay(textRect, editor, lineHeight);
                    DrawCaret(textRect, editor, lineHeight);
                }
            }

            GUI.EndScrollView();

            if (scrollChanged)
            {
                changed = true;
            }

            return changed;
        }

        public void Focus()
        {
            EditorGUI.FocusTextInControl(_controlName);
        }

        public void Dispose()
        {
            _cachedHighlightSource = null;
            _cachedHighlightResult = null;
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _stylesInitialized = true;

            _inputStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false,
                fontSize = 12,
                richText = false
            };

            SetAllStateTextColor(_inputStyle, Color.clear);
            ClearBackgrounds(_inputStyle);

            _overlayStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = false,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Overflow,
                font = _inputStyle.font,
                fontSize = _inputStyle.fontSize,
                padding = new RectOffset(
                    _inputStyle.padding.left,
                    _inputStyle.padding.right,
                    _inputStyle.padding.top,
                    _inputStyle.padding.bottom)
            };

            SetAllStateTextColor(_overlayStyle, EditorStyles.textField.normal.textColor);
            ClearBackgrounds(_overlayStyle);

            _lineNumberStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperRight,
                fontSize = _inputStyle.fontSize,
                font = _inputStyle.font,
                richText = false
            };
            SetAllStateTextColor(_lineNumberStyle, new Color(0.6f, 0.6f, 0.6f));
            _lineNumberStyle.padding = new RectOffset(0, 6, _inputStyle.padding.top, 0);
        }

        private static void SetAllStateTextColor(GUIStyle style, Color color)
        {
            GUIStyleState[] states =
            {
                style.normal,
                style.focused,
                style.hover,
                style.active,
                style.onNormal,
                style.onFocused,
                style.onHover,
                style.onActive
            };

            foreach (GUIStyleState state in states)
            {
                state.textColor = color;
            }
        }

        private static void ClearBackgrounds(GUIStyle style)
        {
            GUIStyleState[] states =
            {
                style.normal,
                style.focused,
                style.hover,
                style.active,
                style.onNormal,
                style.onFocused,
                style.onHover,
                style.onActive
            };

            foreach (GUIStyleState state in states)
            {
                state.background = null;
                state.scaledBackgrounds = null;
            }
        }

        private float GetLineHeight()
        {
            if (_overlayStyle.lineHeight > 0f)
            {
                return _overlayStyle.lineHeight;
            }

            return _overlayStyle.CalcSize(new GUIContent("A")).y;
        }

        private static int GetLineCount(string text)
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

        private float CalculateMaxLineWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return _overlayStyle.CalcSize(new GUIContent(" ")).x;
            }

            float max = 0f;
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    max = Mathf.Max(max, LineWidth(text, start, i - start));
                    start = i + 1;
                }
            }

            max = Mathf.Max(max, LineWidth(text, start, text.Length - start));
            return max;
        }

        private float LineWidth(string text, int start, int length)
        {
            if (length <= 0)
            {
                return _overlayStyle.CalcSize(new GUIContent(" ")).x;
            }

            string segment = text.Substring(start, length);
            Vector2 size = _overlayStyle.CalcSize(new GUIContent(segment));
            return size.x;
        }

        private void DrawLineNumbers(Rect rect, float lineHeight, string text)
        {
            int lineCount = Mathf.Max(1, GetLineCount(text));
            float y = rect.y + _inputStyle.padding.top;

            for (int i = 0; i < lineCount; i++)
            {
                Rect labelRect = new Rect(rect.x, y, rect.width - 4f, lineHeight);
                GUI.Label(labelRect, (i + 1).ToString(), _lineNumberStyle);
                y += lineHeight;
            }
        }

        private void DrawSelectionOverlay(Rect rect, TextEditor editor, float lineHeight)
        {
            if (editor.cursorIndex == editor.selectIndex)
            {
                return;
            }

            int start = Mathf.Min(editor.cursorIndex, editor.selectIndex);
            int end = Mathf.Max(editor.cursorIndex, editor.selectIndex);
            if (start == end)
            {
                return;
            }

            Vector2 startPos = _inputStyle.GetCursorPixelPosition(rect, _cursorContent, start);
            Vector2 endPos = _inputStyle.GetCursorPixelPosition(rect, _cursorContent, end);

            float contentStartX = rect.x + _inputStyle.padding.left;
            float contentEndX = rect.x + rect.width - _inputStyle.padding.right;

            if (Mathf.Approximately(startPos.y, endPos.y))
            {
                float left = Mathf.Min(startPos.x, endPos.x);
                float width = Mathf.Abs(endPos.x - startPos.x);
                if (width > 0f)
                {
                    Rect selectionRect = new Rect(rect.x + left, rect.y + startPos.y, width, lineHeight);
                    EditorGUI.DrawRect(selectionRect, _selectionColor);
                }
                return;
            }

            float firstWidth = Mathf.Max(contentEndX - (rect.x + startPos.x), 0f);
            if (firstWidth > 0f)
            {
                Rect firstRect = new Rect(rect.x + startPos.x, rect.y + startPos.y, firstWidth, lineHeight);
                EditorGUI.DrawRect(firstRect, _selectionColor);
            }

            float currentY = startPos.y + lineHeight;
            while (currentY + 0.1f < endPos.y)
            {
                Rect middleRect = new Rect(contentStartX, rect.y + currentY, Mathf.Max(contentEndX - contentStartX, 0f), lineHeight);
                EditorGUI.DrawRect(middleRect, _selectionColor);
                currentY += lineHeight;
            }

            float lastWidth = Mathf.Max((rect.x + endPos.x) - contentStartX, 0f);
            if (lastWidth > 0f)
            {
                Rect lastRect = new Rect(contentStartX, rect.y + endPos.y, lastWidth, lineHeight);
                EditorGUI.DrawRect(lastRect, _selectionColor);
            }
        }

        private void DrawCaret(Rect rect, TextEditor editor, float lineHeight)
        {
            if (!_caretVisible)
            {
                return;
            }

            Vector2 cursorPos = _inputStyle.GetCursorPixelPosition(rect, _cursorContent, editor.cursorIndex);
            Rect caretRect = new Rect(rect.x + cursorPos.x, rect.y + cursorPos.y, 1f, lineHeight);
            EditorGUI.DrawRect(caretRect, _caretColor);
        }

        private string GetHighlightedText(string text)
        {
            if (_highlight == null)
            {
                return text ?? string.Empty;
            }

            text ??= string.Empty;

            if (_cachedHighlightSource == text)
            {
                return _cachedHighlightResult;
            }

            _cachedHighlightSource = text;
            _cachedHighlightResult = _highlight(text) ?? string.Empty;
            return _cachedHighlightResult;
        }
    }
}
