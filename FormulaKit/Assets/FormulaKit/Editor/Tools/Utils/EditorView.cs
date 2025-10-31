/// <summary>
/// based on https://github.com/joshcamas/unity-code-editor
/// </summary> 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FormulaKit.Editor.Tools.Utils
{
    [Serializable]
    public class EditorViewPalette
    {
        public Color32 WindowBackground { get; set; } = new(34, 34, 34, 255);
        public Color32 LineNumberBackground { get; set; } = new(15, 15, 15, 255);
        public Color LineNumberText { get; set; } = Color.white;
        public Color32 DefaultText { get; set; } = new Color32(255, 255, 255, 255);
        public Color32 Keyword { get; set; } = new(244, 0, 101, 255);
        public Color32 Function { get; set; } = new(165, 255, 11, 255);
        public Color32 Parameter { get; set; } = new(244, 0, 101, 255);
        public Color32 String { get; set; } = new(237, 158, 38, 255);
        public Color32 Comment { get; set; } = new(120, 120, 120, 255);
        public Color32 InlineCode { get; set; } = new(165, 255, 11, 255);
        public Color32 Flag { get; set; } = new(180, 180, 180, 255);
        public Color32 Operator { get; set; } = new(180, 180, 180, 255);
        public Color32 LineHighlight { get; set; } = new(255, 255, 255, 25);
        public Color32 Selection { get; set; } = new(255, 255, 255, 45);
        public Color Cursor { get; set; } = new(1f, 1f, 1f, 0.8f);
    }

    [Serializable]
    public class EditorViewOptions
    {
        private static readonly string[] DefaultKeywords =
        {
            "False", "None", "True", "and", "as", "assert", "break", "class", "continue", "def", "del", "elif",
            "else", "except", "finally", "for", "from", "global", "if", "import", "in", "is", "lambda", "nonlocal",
            "not", "or", "pass", "raise", "return", "try", "while", "with", "yield", "self", "let"
        };

        private IEnumerable<string> _keywords = DefaultKeywords;
        private IEnumerable<string> _functionNames = Array.Empty<string>();
        private HashSet<string> _keywordLookup;
        private HashSet<string> _functionLookup;
        private EditorViewPalette _palette = new();

        public IEnumerable<string> Keywords
        {
            get => _keywords;
            set
            {
                _keywords = value ?? DefaultKeywords;
                _keywordLookup = null;
            }
        }

        public IEnumerable<string> FunctionNames
        {
            get => _functionNames;
            set
            {
                _functionNames = value ?? Array.Empty<string>();
                _functionLookup = null;
            }
        }

        public bool ShowLineNumbers { get; set; } = true;
        public Vector2 Padding { get; set; } = new (44, 15);
        public Vector2 CharacterSize { get; set; } = new (7, 19);
        public int FontSize { get; set; } = 12;

        public EditorViewPalette Palette
        {
            get => _palette;
            set => _palette = value ?? new EditorViewPalette();
        }

        internal HashSet<string> KeywordLookup
        {
            get
            {
                return _keywordLookup ?? (_keywordLookup =
                    new HashSet<string>(Keywords ?? DefaultKeywords, StringComparer.OrdinalIgnoreCase));
            }
        }

        internal HashSet<string> FunctionLookup
        {
            get
            {
                return _functionLookup ?? (_functionLookup = new HashSet<string>(FunctionNames ?? Array.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase));
            }
        }
    }

    [Serializable]
    public class EditorView
    {
        private const string MatchCode = @"(\t)|(\{\{.+\}\})|(\[\[.+\]\])|(\w+)|(\s+)|(.)";
        
       
        [SerializeField] private Rect _highLine;
        [SerializeField] private Rect _layoutRect;
        [SerializeField] private Rect _positionSyntax;
        [SerializeField] private List<Rect> _highLightSelections;
        [SerializeField] private Vector2 _positionScroll = Vector2.zero;
        [SerializeField] private Vector2 _padding = new(44, 15);
        [SerializeField] private Vector2 _fontSizeXY = new(7, 19);
        [SerializeField] private int _focusID;
        [SerializeField] private bool _isSelection;
        [SerializeField] private bool _focused;
        
        private readonly EditorViewOptions _options;
        private ScriptEditorBuffer _buffer;
        private EditorViewStyles _style;

        public EditorView(EditorViewOptions options = null)
        {
            _options = options ?? new EditorViewOptions();
            _padding = _options.Padding;
            _fontSizeXY = _options.CharacterSize;
            _style = new EditorViewStyles(_options);
        }

        //Delegate For Repaint Inspector.
        public delegate void CodeRepaint();

        public event CodeRepaint RepaintAction;


        public void OnEnable(string rawScript)
        {
            _buffer ??= new ScriptEditorBuffer();
            _buffer.Initialize(rawScript ?? string.Empty);
            _buffer.Line = 1;
            _buffer.Column = 0;
            _buffer.CurrentLine = _buffer.GetLine(_buffer.Line);
            _buffer.SetColumnIndex();
            _buffer.TotalLines = Mathf.Max(1, _buffer.TotalLines);
            _positionScroll = Vector2.zero;
            _highLightSelections = null;
            _isSelection = false;
            _focused = false;
        }
        
        public void EditorViewGUI()
        {
            FocusControl();
            //Get box rect and background
            GetBoxRect();
            //Begin ScrollView of box
            _positionScroll = GUI.BeginScrollView(new Rect(0, _layoutRect.y, _layoutRect.width + 15, _layoutRect.height),
                    _positionScroll, new Rect(0, _layoutRect.yMin, _layoutRect.xMax, Mathf.Max(_layoutRect.height, 23 * _buffer.TotalLines)));
            //Draw Line Numbers.
            EventRepainted();
            //Draw Cursor for text
            Cursor();
            //HighLight Current Line.
            HighlightLine();
            //HightLigh Selection Text
            HighlightSelected();

            GUI.EndScrollView();

            //KeyBoard Events
            KeyBoardController();
        }

        public string GetText()
        {
            return _buffer != null ? _buffer.CodeBuffer : string.Empty;
        }

        public void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (_buffer == null)
            {
                OnEnable(string.Empty);
            }

            foreach (var character in text)
            {
                if (character == '\r')
                {
                    continue;
                }

                _buffer.InsertText(character);
            }

            _buffer.SaveCodeToBuffer();
            Repaint();
        }

        public void RequestFocus()
        {
            _focused = true;
        }
        
        private void EventRepainted()
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            DrawCodeOnGUI();
            LineNumbers();
            _buffer.Trim();
        }

        private void GetBoxRect()
        {
            _positionSyntax = _layoutRect = GUILayoutUtility.GetRect(0, Screen.width, 1, Screen.height - _padding.y);
            GUI.Box(_layoutRect, GUIContent.none, _style.Background);
        }
        
        private void DrawCodeOnGUI()
        {
            if (_buffer.Lines.Count == 0)
            {
                _buffer.Lines = new List<List<string>>();

                using (var readerLine = new StringReader(_buffer.CodeBuffer))
                {
                    while (readerLine.ReadLine() is { } line)
                    {
                        var words = new List<string>();
                        var pattern = new Regex(MatchCode);
                        foreach (Match results in pattern.Matches(line))
                        {
                            words.Add(results.Value);
                        }
                        _buffer.Lines.Add(words);
                    }
                }

                if (_buffer.CodeBuffer == string.Empty)
                {
                    _buffer.Lines.Add(new List<string>());
                }
            }

            _buffer.TotalLines = 1;
            _positionSyntax.y += 5;

            _style.BlockComment = false;

            for (var i = 0; i < _buffer.Lines.Count; i++)
            {
                _positionSyntax.x = _padding.x;
                _style.ResetLineStyles();

                for (var j = 0; j < _buffer.Lines[i].Count; j++)
                {
                    var word = TabToSpace(_buffer.Lines[i][j]);
                    _positionSyntax.width = _fontSizeXY.x * word.Length;
                    _style.FontGUIStyle.normal.textColor = _style.CheckWordStyle(word);
                    GUI.Label(_positionSyntax, word, _style.FontGUIStyle);
                    _positionSyntax.x += _positionSyntax.width;

                }
                _buffer.TotalLines++;
                _positionSyntax.y += _fontSizeXY.y;
            }
        }
        
        private void LineNumbers()
        {
            if (!_options.ShowLineNumbers)
            {
                return;
            }

            GUI.Box(new Rect(_positionScroll.x, _layoutRect.y, 40, Screen.height + _positionScroll.y), GUIContent.none, _style.BackgroundLines);

            var rectLineNumbers = new Rect(_positionScroll.x + 3, _layoutRect.y + 5, 30, _layoutRect.height - _padding.y);
            for (var i = 1; i <= _buffer.TotalLines + (int)(_positionScroll.y / _buffer.TotalLines - 1); i++)
            {
                _style.NumberLines.Draw(rectLineNumbers, new GUIContent(i.ToString()), true, false, false, false);
                rectLineNumbers.y += _fontSizeXY.y;
            }
        }
        
        public void Cursor()
        {
            EditorGUIUtility.AddCursorRect(new Rect(_layoutRect.x, _layoutRect.y, _layoutRect.width + _positionScroll.x, _layoutRect.height - 15), MouseCursor.Text);
            if (_isSelection)
            {
                return;
            }
            
            var positionCursor = ToPixelLine(new Vector2(_buffer.Column, _buffer.Line));
            var cursorRect = new Rect(positionCursor.x, positionCursor.y, 1, _fontSizeXY.y);
            GUI.Box(cursorRect, GUIContent.none, _style.Cursor);
        }
        
        private void HighlightLine()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                var vectorXY = ToNumberLine(Event.current.mousePosition.x, Event.current.mousePosition.y);
                _buffer.Initialize((int)vectorXY.y, (int)vectorXY.x);
                _isSelection = false;
                Repaint();
            }

            var linePixel = ToPixelLine(new Vector2(_buffer.Column, _buffer.Line)).y;
            _highLine = new Rect(0, linePixel, Screen.width, _fontSizeXY.y);
            _highLine.width = Screen.width + _positionScroll.x;
            GUI.Box(_highLine, GUIContent.none, _style.HighLine);
        }
        
        private void HighlightSelected()
        {
            //Double Cliked (select a single word)
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
            {
                _isSelection = true;
                var lineSpace = TabToSpace(_buffer.CurrentLine);
                int begin = 0, index = 0;
                float width = 0;
                foreach (Match word in Regex.Matches(lineSpace, MatchCode))
                {
                    if (width != 0)
                    {
                        continue;
                    }
                    
                    for (var i = 0; i < word.Length; i++)
                    {
                        begin = i == 0 ? index : begin;
                        width = index == _buffer.Column ? word.Length : 0;
                        index = width == 0 ? index + 1 : index;
                    }
                }

                var pixels = ToPixelLine(new Vector2(begin, _buffer.Line));
                width *= _fontSizeXY.x;

                _highLightSelections = new List<Rect>() { new (pixels.x, pixels.y, width, _fontSizeXY.y) };

            }
            
            //TODO : Drag Selection
            if (Event.current.type == EventType.MouseDrag)
            {
                //??
            }

            if (!_isSelection)
            {
                return;
            }
            
            foreach(var r in _highLightSelections)
            {
                GUI.Box(r, GUIContent.none, _style.Selection);
            }

        }
        
        private void FocusControl()
        {
            //TODO: FIX!
            GUIUtility.keyboardControl = _focusID;
            _focusID = GUIUtility.GetControlID(Math.Abs(GetHashCode()), FocusType.Keyboard);
            GUIUtility.keyboardControl = _focused ? _focusID : GUIUtility.keyboardControl;
            _focused = (_focusID > 0) && (GUIUtility.keyboardControl == _focusID);

        }
        
        public void KeyBoardController()
        {
            var e = Event.current;

            if (e.type != EventType.KeyDown)
            {
                return;
            }
            
            switch (e.keyCode)
            {
                case KeyCode.Backspace:
                {
                    if (!_isSelection)
                    {
                        _buffer.RemoveText();
                    }
                    else
                    {
                        foreach(var r in _highLightSelections)
                        {
                            _buffer.RemoveRange(GetRangeText(r, _buffer.CurrentLine));
                        }
                    }
                    
                    SetChanges();
                    _isSelection = false;
                    break;
                }
                //Select all
                case KeyCode.A when e.command:
                {
                    break;
                }
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                {
                    break;
                }
                case KeyCode.UpArrow:
                {
                    _buffer.GoUp();
                    break;
                }
                case KeyCode.DownArrow:
                {
                    _buffer.GoDown();
                    break;
                }
                case KeyCode.LeftArrow:
                {
                    _buffer.GoLeft();
                    break;
                }
                case KeyCode.RightArrow:
                {
                    _buffer.GoRight();
                    break;
                }
                case KeyCode.End:
                {
                    _buffer.GoEnd();
                    break;
                }
                case KeyCode.Home:
                {
                    _buffer.GoHome();
                    break;
                }
                //Get any key
                case KeyCode.None:
                {
                    var c = Convert.ToChar(e.character.ToString());
                    c = e.shift ? char.ToUpper(c) : c;
                    //Remove Text if has selection text.
                    if (_isSelection)
                    {
                        foreach (Rect r in _highLightSelections)
                        {
                            _buffer.RemoveRange(GetRangeText(r, _buffer.CurrentLine));
                        }
                        _isSelection = false;
                    }
                    _buffer.InsertText(c);

                    SetChanges();
                    break;
                }
            }

            e.Use();
        }
        
        private void SetChanges()
        {
            _buffer.SaveCodeToBuffer();
        }

        private int[] GetRangeText(Rect range, string text)
        {
            var rangeMin = ToNumberLine(range.xMin, range.y);
            var rangeMax = ToNumberLine(range.xMax, range.y);
            var begin = _buffer.GetIndexColumn((int)rangeMin.x, text);
            var end = _buffer.GetIndexColumn((int)rangeMax.x, text);
            return new [] { begin, end };
        }


        public Vector2 ToNumberLine(float column, float line)
        {
            line = Math.Min((line - _layoutRect.y + _padding.y) / _fontSizeXY.y, _buffer.TotalLines - 1);
            column = (column - _padding.x) / _fontSizeXY.x;
            line = line == 0 ? 1 : line;
            return new Vector2(column, line);
        }
        
        public Vector2 ToPixelLine(Vector2 positionLine)
        {
            var column = (int)((_fontSizeXY.x * positionLine.x) + _padding.x);
            var line = (int)((_fontSizeXY.y * positionLine.y + 1) + _layoutRect.y - _padding.y);
            column = (int)(column < _padding.x ? _padding.x : column);
            line = (int)(line < _layoutRect.yMin ? _layoutRect.yMin + 5 : line);
            return new Vector2(column, line);
        }
        
        private string TabToSpace(string value)
        {
            return value.Replace("\t", "    ");
        }
        
        private void Repaint()
        {
            RepaintAction?.Invoke();
        }

    }
    
    public class EditorViewStyles
    {
        public bool BlockComment;

        private readonly EditorViewOptions _options;
        private GUIStyle _background;
        private GUIStyle _font;
        private GUIStyle _backgroundLines;
        private GUIStyle _numberLines;
        private GUIStyle _highLine;
        private GUIStyle _selection;
        private GUIStyle _cursor;
        private GUIStyle _interpreter;
        private static readonly char[] OperatorCharacters = "+-*/%<>=!&|^~?:.,()[]{}".ToCharArray();
        
        private bool _lineComment;
        private bool _isString;
        private string _whichQuote = string.Empty;
        private string _tripleQuotes = string.Empty;
        
        public EditorViewStyles(EditorViewOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public GUIStyle Background
        {
            get
            {
                if (_background != null)
                {
                    return _background;
                }
                _background = new GUIStyle
                {
                    normal =
                    {
                        background = TextureColor(_options.Palette.WindowBackground)
                    }
                };
                return _background;

            }
        }

        public GUIStyle FontGUIStyle
        {
            get
            {
                if (_font != null)
                {
                    return _font;
                }
                _font = new GUIStyle
                {
                    fontSize = _options.FontSize,
                    normal =
                    {
                        textColor = _options.Palette.DefaultText
                    }
                };
                return _font;

            }
        }

        public GUIStyle BackgroundLines
        {
            get
            {
                if (_backgroundLines != null)
                {
                    return _backgroundLines;
                }
                _backgroundLines = new GUIStyle
                {
                    normal =
                    {
                        background = TextureColor(_options.Palette.LineNumberBackground)
                    }
                };
                return _backgroundLines;

            }
        }

        public GUIStyle NumberLines
        {
            get
            {
                if (_numberLines != null)
                {
                    return _numberLines;
                }
                _numberLines = new GUIStyle
                {
                    normal =
                    {
                        textColor = _options.Palette.LineNumberText
                    },
                    font = FontGUIStyle.font,
                    alignment = TextAnchor.UpperRight,
                    fontSize = _options.FontSize
                };
                return _numberLines;

            }
        }

        public GUIStyle HighLine
        {
            get
            {
                if (_highLine != null)
                {
                    return _highLine;
                }
                _highLine = new GUIStyle
                {
                    normal =
                    {
                        background = TextureColor(_options.Palette.LineHighlight)
                    }
                };
                return _highLine;

            }
        }

        public GUIStyle Selection
        {
            get
            {
                if (_selection != null)
                {
                    return _selection;
                }
                _selection = new GUIStyle
                {
                    normal =
                    {
                        background = TextureColor(_options.Palette.Selection)
                    }
                };
                return _selection;

            }
        }

        public GUIStyle Cursor
        {
            get
            {
                if (_cursor != null)
                {
                    return _cursor;
                }
                _cursor = new GUIStyle
                {
                    normal =
                    {
                        background = TextureColor(_options.Palette.Cursor)
                    }
                };
                return _cursor;

            }
        }

        public GUIStyle Interpreter
        {
            get
            {
                if (_interpreter != null)
                {
                    return _interpreter;
                }
                _interpreter = new GUIStyle
                {
                    font = FontGUIStyle.font,
                    fontSize = _options.FontSize,
                    normal =
                    {
                        textColor = _options.Palette.DefaultText
                    }
                };

                return _interpreter;

            }
        }
        
        public Color32 CheckWordStyle(string word)
        {
            _lineComment = !_lineComment ? word.StartsWith("#") : _lineComment;

            var isParameter = word.StartsWith("$");

            var isFunction = _options.FunctionLookup.Contains(word);

            var isCodeBlock = word.StartsWith("[[") && word.EndsWith("]]");

            var isCodeFlag = word.StartsWith("{{") && word.EndsWith("}}");

            return _lineComment ? _options.Palette.Comment
                : isParameter ? _options.Palette.Parameter
                    //Block Comment
                : BlockCommentStyle(word) ? _options.Palette.String
                    //Strings
                : StringStyle(word) ? _options.Palette.String
                    //Keywords
                : _options.KeywordLookup.Contains(word) ? _options.Palette.Keyword
                : isFunction ? _options.Palette.Function

                : isCodeBlock ? _options.Palette.InlineCode
                : isCodeFlag ? _options.Palette.Flag
                : IsOperator(word) ? _options.Palette.Operator
                    //Default
                : _options.Palette.DefaultText;
        }
        
        private bool BlockCommentStyle(string word)
        {
            if (Regex.IsMatch(word, "([\"'])"))
            {
                _tripleQuotes += word;

                if (Regex.IsMatch(_tripleQuotes, "([\"]{3})|([\']{3})"))
                {
                    BlockComment = !BlockComment;
                    _tripleQuotes = string.Empty;

                    return true;
                }

                return BlockComment;
            }
            else
            {
                //Reset Quotes
                _tripleQuotes = string.Empty;
            }

            return BlockComment;
        }
        
        private bool StringStyle(string word)
        {
            if (Regex.IsMatch(word, "([\"'])") && !BlockComment)
            {
                _isString = string.IsNullOrEmpty(_whichQuote) || word != _whichQuote;
                _whichQuote = string.IsNullOrEmpty(_whichQuote) ? word
                    : !_isString ? string.Empty
                    : _whichQuote;
                return true;
            }

            return _isString;
        }

        private bool IsOperator(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }

            return word.All(character => OperatorCharacters.Contains(character));
        }
        
        public void ResetLineStyles()
        {
            _isString = false;
            _lineComment = false;
            _whichQuote = string.Empty;
            _tripleQuotes = string.Empty;
        }
        
        private static Texture2D TextureColor(Color color)
        {
            var textureColor = new Texture2D(1, 1);
            textureColor.SetPixels(new [] { color });
            textureColor.Apply();
            textureColor.hideFlags = HideFlags.HideAndDontSave;
            return textureColor;
        }
    }

    [Serializable]
    public class ScriptEditorBuffer
    {
      
        public int Line;
        public int Column;
        public int ColumnIndex;
        public int TotalLines = 1;
        public List<List<string>> Lines = new List<List<string>>();
        public string CodeBuffer = string.Empty;
        public string CurrentLine = string.Empty;
        public string InterpreterBuffer = string.Empty;
        public string InterpreterBlock = string.Empty;
        public bool InterpreterView;
        public bool BlockInspector;

        public ScriptEditorBuffer Initialize()
        {
            Lines = new List<List<string>>();
            CurrentLine = string.Empty;
            CodeBuffer = string.Empty;

            SetColumnIndex();

            return this;
        }

        public ScriptEditorBuffer Initialize(string expression)
        {
            Lines = new List<List<string>>();
            CurrentLine = string.Empty;
            CodeBuffer = expression ?? string.Empty;
            SetColumnIndex();
            return this;
        }
        
        public void Initialize(int line, int column)
        {
            Line = line;
            Column = column;
            CurrentLine = GetLine(Line);
            SetColumnIndex();
        }
        
        public void SetColumnIndex()
        {
            Trim();

            ColumnIndex = GetIndexColumn(Column, CurrentLine);

            ColumnIndex = CurrentLine.Length == 0 ? 0
            : ColumnIndex > CurrentLine.Length ? CurrentLine.Length
            : ColumnIndex;
        }
        
        public int GetIndexColumn(int column, string line)
        {
            if (column == 0)
            {
                return 0;
            }

            var index = 0;
            for (var i = 0; i <= line.Length; i++)
            {
                if (line.Length <= 0)
                {
                    continue;
                }
                index = line[i] == '\t' ? index + 4 : index + 1;
                if (index >= column)
                {
                    return ++i;
                }
            }

            return 0;
        }
        
        public void GoUp()
        {
            if (Line == 0)
            {
                return;
            }
            Line--;
            CurrentLine = GetLine(Line);
            SetColumnIndex();
        }
        
        public void GoDown()
        {
            Line++;
            CurrentLine = GetLine(Line);
            SetColumnIndex();
        }
        
        public void GoLeft()
        {
            if (Column == 0)
            {
                Line--;
            }

            if (Event.current.command)
            {
                Column = Regex.Match(TabToSpace(CurrentLine), @"\w").Index;
            }            
            else
            {
                Column = Column > 0 ? Column - 1 : GetLine(Line).Length;
                Column = GetCharIndex(ColumnIndex - 1) == '\t' ? Column - 3 : Column;
            }
            SetColumnIndex();
        }

       
        public void GoRight()
        {
            if (Event.current.command)
            {
                Column = TabToSpace(CurrentLine).Length;
            }
            else if (ColumnIndex >= CurrentLine.Length && !InterpreterView)
            {
                GoDown();
                Column = Regex.Match(TabToSpace(CurrentLine), @"\w").Index;
            }
            else
            {
                Column = GetCharIndex(ColumnIndex) == '\t' ? Column + 4 : Column + 1;
            }
            SetColumnIndex();
        }
        
       
        public void GoEnd()
        {
            Column = TabToSpace(CurrentLine).Length;
            SetColumnIndex();
        }
        
   
        public void GoHome()
        {
            Column = Regex.Match(TabToSpace(CurrentLine), @"\w").Index;
            SetColumnIndex();
        }
        
        public void InsertLine(int numberLine)
        {
            Lines.Insert(numberLine, new List<string>());
        }

        public void InsertText(char c)
        {
            var stringJoin = this.CurrentLine;

            if (c == '\n')
            {
                var leftText = string.Empty;
                var rightText = string.Empty;

                for (var i = 0; i < stringJoin.Length; i++)
                {
                    if (i >= ColumnIndex)
                    {
                        rightText += stringJoin[i];
                    }
                    else
                    {
                        leftText += stringJoin[i];
                    }
                }

                var spaces = Regex.Match(leftText, @"(^\t+)|(^\s+)");

                rightText = rightText.Insert(0, spaces.Value);

                UpdateLineText(Line, leftText);
                InsertLine(Line);
                UpdateLineText(++Line, rightText);
                TotalLines++;

                //Jump cursor after indentation.
                var indentation = Regex.Match(TabToSpace(leftText), @"\w");
                Column = indentation.Success ? indentation.Index : Column;
                SetColumnIndex();
            }
            else
            {
                var newChar = (c.ToString());
                stringJoin = stringJoin.Insert(ColumnIndex, newChar);
                UpdateLineText(Line, stringJoin);
                Column = c == '\t' ? Column + 4 : ++Column;
                SetColumnIndex();
            }
        }
        
        public void InsertTextInterpreter(char c)
        {
            CurrentLine = CurrentLine.Insert(ColumnIndex, c.ToString());
            Column = c == '\t' ? Column + 4 : ++Column;
            SetColumnIndex();
        }
        
        public void AppendInterpreter(string output)
        {
            var sb = new StringBuilder("\n" + InterpreterBuffer);
            var blockSeparator = this.BlockInspector ? "..." : ">>> ";
            var breakLine = String.IsNullOrEmpty(output) ? "" : "\n";
            sb.Insert(0, output + breakLine + blockSeparator + CurrentLine);

            InterpreterBuffer = sb.ToString();
            CurrentLine = string.Empty;
            SetColumnIndex();
        }
        
        public void RemoveText()
        {
            if (!ElementInList(Line) && !InterpreterView)
            {
                return;
            }

            if (Column > 0)
            {
                GoLeft();
                var lineTab = CurrentLine;
                lineTab = lineTab.Remove(Math.Max(0, ColumnIndex), 1);
                UpdateLineText(Line, lineTab);

            }
            else if (!InterpreterView && Line != 1)
            {
                Lines[Line - 2].Add(CurrentLine);
                Lines.RemoveAt(Line - 1);
                GoUp();
                Column = GetLine(Line - 1).IndexOf(CurrentLine, StringComparison.Ordinal);
                SetColumnIndex();
            }
        }
        
        public void RemoveRange(int[] range)
        {
            CurrentLine = (CurrentLine).Remove(range[0], range[1] - range[0]);
            UpdateLineText(Line, CurrentLine);
            Column = range[0] + Regex.Matches(CurrentLine.Substring(0, range[0]), @"(\t)").Count * 3;
            SetColumnIndex();
        }
        
        private void UpdateLineText(int numberLine, string lineText)
        {
            if (InterpreterView)
            {
                CurrentLine = lineText;
                return;
            }

            if (!ElementInList(numberLine))
            {
                return;
            }

            Lines[numberLine - 1] = new List<string>();

            foreach (Match results in Regex.Matches(lineText, @"(\t)|(\w+)|(\s+)|(.)"))
                Lines[numberLine - 1].Add(results.Value);

            CurrentLine = lineText;
        }
        
        public void SaveCodeToBuffer()
        {
            var text = new StringBuilder();

            foreach (var line in Lines)
            {
                foreach (var word in line)
                {
                    text.Append(word);
                }

                text.AppendLine();
            }

            CodeBuffer = text.ToString();
        }
        
        public char GetCharIndex(int index)
        {
            return CurrentLine.ElementAtOrDefault(index);
        }

        public string GetLine(int numberLine)
        {
            return !ElementInList(numberLine) ? string.Empty : string.Join("", Lines[numberLine - 1].ToArray());
        }
        
        private bool ElementInList(int numberLine)
        {
            return Lines.ElementAtOrDefault(numberLine - 1) != null;
        }
        
        public void Trim()
        {
            var line = CurrentLine.Replace("\t", "    ");

            if (!InterpreterView)
            {
                Line = Line >= TotalLines ? TotalLines - 1 : Line;
                Column = Column > line.Length ? line.Length : Column;
            }
            else
            {
                Column = Column > line.Length ? CurrentLine.Length : Column;
            }

        }
        
        private string TabToSpace(string value)
        {
            return value.Replace("\t", "    ");
        }

       
        public override string ToString()
        {
            return $"Line: {Line}, Column: {Column}, ColumnIndex: {ColumnIndex}, CurrentLine: {CurrentLine}, TotalLine: {TotalLines}";
        }

    }

}
