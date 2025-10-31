using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Joshcamas.CodeEditor
{
    [Serializable]
    public class EditorViewPalette
    {
        public Color32 WindowBackground { get; set; } = new Color32(34, 34, 34, 255);
        public Color32 LineNumberBackground { get; set; } = new Color32(15, 15, 15, 255);
        public Color LineNumberText { get; set; } = Color.white;
        public Color32 DefaultText { get; set; } = new Color32(255, 255, 255, 255);
        public Color32 Keyword { get; set; } = new Color32(244, 0, 101, 255);
        public Color32 Function { get; set; } = new Color32(165, 255, 11, 255);
        public Color32 Parameter { get; set; } = new Color32(244, 0, 101, 255);
        public Color32 String { get; set; } = new Color32(237, 158, 38, 255);
        public Color32 Comment { get; set; } = new Color32(120, 120, 120, 255);
        public Color32 InlineCode { get; set; } = new Color32(165, 255, 11, 255);
        public Color32 Flag { get; set; } = new Color32(180, 180, 180, 255);
        public Color32 Operator { get; set; } = new Color32(180, 180, 180, 255);
        public Color32 LineHighlight { get; set; } = new Color32(255, 255, 255, 25);
        public Color32 Selection { get; set; } = new Color32(255, 255, 255, 45);
        public Color Cursor { get; set; } = new Color(1f, 1f, 1f, 0.8f);
    }

    [Serializable]
    public class EditorViewOptions
    {
        private static readonly string[] DefaultKeywords =
        {
            "False", "None", "True", "and", "as", "assert", "break", "class", "continue", "def", "del", "elif",
            "else", "except", "finally", "for", "from", "global", "if", "import", "in", "is", "lambda", "nonlocal",
            "not", "or", "pass", "raise", "return", "try", "while", "with", "yield", "self"
        };

        private IEnumerable<string> keywords;
        private IEnumerable<string> functionNames;
        private HashSet<string> keywordLookup;
        private HashSet<string> functionLookup;
        private EditorViewPalette palette;

        public EditorViewOptions()
        {
            keywords = DefaultKeywords;
            functionNames = Array.Empty<string>();
            palette = new EditorViewPalette();
        }

        public IEnumerable<string> Keywords
        {
            get => keywords;
            set
            {
                keywords = value ?? DefaultKeywords;
                keywordLookup = null;
            }
        }

        public IEnumerable<string> FunctionNames
        {
            get => functionNames;
            set
            {
                functionNames = value ?? Array.Empty<string>();
                functionLookup = null;
            }
        }

        public bool ShowLineNumbers { get; set; } = true;

        public Vector2 Padding { get; set; } = new Vector2(44, 15);

        public Vector2 CharacterSize { get; set; } = new Vector2(7, 19);

        public int FontSize { get; set; } = 12;

        public EditorViewPalette Palette
        {
            get => palette;
            set => palette = value ?? new EditorViewPalette();
        }

        internal HashSet<string> KeywordLookup
        {
            get
            {
                if (keywordLookup == null)
                {
                    keywordLookup = new HashSet<string>(Keywords ?? DefaultKeywords, StringComparer.OrdinalIgnoreCase);
                }

                return keywordLookup;
            }
        }

        internal HashSet<string> FunctionLookup
        {
            get
            {
                if (functionLookup == null)
                {
                    functionLookup = new HashSet<string>(FunctionNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                }

                return functionLookup;
            }
        }
    }

    [Serializable]
    public class EditorView
    {
        public ScriptEditorBuffer buffer;

        private readonly EditorViewOptions options;

        [SerializeField]
        private Rect HighLine, LayoutRect, PositionSyntax;

        [SerializeField]
        private List<Rect> highLightSelections;

        [SerializeField]
        private Vector2 PositionScroll = Vector2.zero;

        [SerializeField]
        private Vector2 Padding = new Vector2(44, 15);

        [SerializeField]
        private Vector2 FontSizeXY = new Vector2(7, 19);

        [SerializeField]
        private int FocusID;

        [SerializeField]
        private bool isSelection, Focused;

        private float Bottom;

        /// <summary>
        /// Use for match code
        /// </summary>
        private const string MatchCode = @"(\t)|(\{\{.+\}\})|(\[\[.+\]\])|(\w+)|(\s+)|(.)";

        private EditorViewStyles Style;

        public EditorView(EditorViewOptions options = null)
        {
            this.options = options ?? new EditorViewOptions();
            Padding = this.options.Padding;
            FontSizeXY = this.options.CharacterSize;
            Style = new EditorViewStyles(this.options);
        }

        //Delegate For Repaint Inspector.
        public delegate void CodeRepaint();

        public event CodeRepaint RepaintAction;

        /// <summary>
        /// Enable
        /// </summary>
        public void OnEnable(string rawScript)
        {

            if (buffer == null)
                buffer = new ScriptEditorBuffer();

            buffer.Initialize(rawScript ?? string.Empty);
            buffer.Line = 1;
            buffer.Column = 0;
            buffer.CurrentLine = buffer.GetLine(buffer.Line);
            buffer.SetColumnIndex();
            buffer.TotalLines = Mathf.Max(1, buffer.TotalLines);
            PositionScroll = Vector2.zero;
            highLightSelections = null;
            isSelection = false;
            Focused = false;
        }

        /// <summary>
        /// Editors the view controll.
        /// </summary>
        public void EditorViewGUI()
        {
            FocusControl();
            //Get box rect and background
            GetBoxRect();
            //Begin ScrollView of box
            PositionScroll = GUI.BeginScrollView(new Rect(0, LayoutRect.y, LayoutRect.width + 15, LayoutRect.height),
                    PositionScroll, new Rect(0, LayoutRect.yMin, LayoutRect.xMax, Mathf.Max(LayoutRect.height, 23 * buffer.TotalLines)));
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
            return buffer != null ? buffer.CodeBuffer : string.Empty;
        }

        public void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (buffer == null)
                OnEnable(string.Empty);

            foreach (char character in text)
            {
                if (character == '\r')
                    continue;

                buffer.InsertText(character);
            }

            buffer.SaveCodeToBuffer();
            Repaint();
        }

        public void RequestFocus()
        {
            Focused = true;
        }

        /// <summary>
        /// Events the repainted.
        /// </summary>
        private void EventRepainted()
        {
            if (Event.current.type == EventType.Repaint)
            {
                DrawCodeOnGUI();

                //Draw Number of Lines
                LineNumbers();

                //Trim Column to end of lines
                buffer.Trim();

            }
        }

        private void GetBoxRect()
        {
            //Code Rect Layout
            PositionSyntax = LayoutRect = GUILayoutUtility.GetRect(0, Screen.width, 1, Screen.height - Padding.y);

            //Background
            GUI.Box(LayoutRect, GUIContent.none, Style.Background);

            //Bottom value of box
            Bottom = LayoutRect.yMax - 40;
        }

        /// <summary>
        /// Draws code on Inspector
        /// </summary>
        private void DrawCodeOnGUI()
        {
            if (buffer.Lines.Count == 0)
            {
                buffer.Lines = new List<List<string>>();

                using (StringReader readerLine = new StringReader(buffer.CodeBuffer))
                {

                    string line = string.Empty;

                    while ((line = readerLine.ReadLine()) != null)
                    {

                        List<string> words = new List<string>();

                        Regex pattern = new Regex(MatchCode);

                        foreach (Match results in pattern.Matches(line))
                            words.Add(results.Value);

                        buffer.Lines.Add(words);
                    }
                }

                if (buffer.CodeBuffer == string.Empty)
                    buffer.Lines.Add(new List<string>());
            }

            buffer.TotalLines = 1;
            PositionSyntax.y += 5;

            Style.BlockComment = false;

            for (int i = 0; i < buffer.Lines.Count; i++)
            {

                PositionSyntax.x = Padding.x;

                //Reset Lines styles
                Style.ResetLineStyles();

                for (int j = 0; j < buffer.Lines[i].Count; j++)
                {

                    string word = TabToSpace(buffer.Lines[i][j]);

                    PositionSyntax.width = FontSizeXY.x * word.Length;

                    Style.FontGUIStyle.normal.textColor = Style.CheckWordStyle(word);

                    //Draw word in GUI Label
                    GUI.Label(PositionSyntax, word, Style.FontGUIStyle);

                    PositionSyntax.x += PositionSyntax.width;

                }
                buffer.TotalLines++;
                PositionSyntax.y += FontSizeXY.y;
            }
        }


        /// <summary>
        /// Draw Line Numbers
        /// </summary>
        private void LineNumbers()
        {
            if (!options.ShowLineNumbers)
            {
                return;
            }

            //Background Lines
            GUI.Box(new Rect(PositionScroll.x, LayoutRect.y, 40, Screen.height + PositionScroll.y), GUIContent.none, Style.BackgroundLines);

            Rect RectLineNumbers = new Rect(PositionScroll.x + 3, LayoutRect.y + 5, 30, LayoutRect.height - Padding.y);
            for (int i = 1; i <= buffer.TotalLines + (int)(PositionScroll.y / buffer.TotalLines - 1); i++)
            {

                //Draw number.
                Style.NumberLines.Draw(RectLineNumbers, new GUIContent(i.ToString()), true, false, false, false);
                //Increase line height.
                RectLineNumbers.y += FontSizeXY.y;

            }
        }

        /// <summary>
        /// Cursors
        /// </summary>
        public void Cursor()
        {
            //Cursor for Editing Text.
            EditorGUIUtility.AddCursorRect(new Rect(LayoutRect.x, LayoutRect.y, LayoutRect.width + PositionScroll.x, LayoutRect.height - 15), MouseCursor.Text);

            if (!isSelection)
            {

                Vector2 PositionCursor = ToPixelLine(new Vector2(buffer.Column, buffer.Line));

                Rect CursorRect = new Rect(PositionCursor.x, PositionCursor.y, 1, FontSizeXY.y);

                GUI.Box(CursorRect, GUIContent.none, Style.Cursor);

            }
        }

        /// <summary>
        /// Highlight line clicked
        /// </summary>
        private void HighlightLine()
        {
            if (Event.current.type == EventType.MouseDown)
            {

                //Mouse Position X Y
                float PointerX = Event.current.mousePosition.x;
                float PointerY = Event.current.mousePosition.y;

                Vector2 VectorXY = ToNumberLine(PointerX, PointerY);

                buffer.Initialize((int)VectorXY.y, (int)VectorXY.x);

                isSelection = false;

                Repaint();

            }

            float LinePixel = ToPixelLine(new Vector2(buffer.Column, buffer.Line)).y;
            HighLine = new Rect(0, LinePixel, Screen.width, FontSizeXY.y);

            HighLine.width = Screen.width + PositionScroll.x;

            GUI.Box(HighLine, GUIContent.none, Style.HighLine);

        }

        /// <summary>
        /// Highlighs the selected.
        /// </summary>
        private void HighlightSelected()
        {
            //Double Cliked (select a single word)
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
            {
                isSelection = true;

                string LineSpace = TabToSpace(buffer.CurrentLine);

                //Selected Word
                int begin = 0, index = 0;
                float width = 0;
                //Extract single word
                foreach (Match word in Regex.Matches(LineSpace, MatchCode))
                {
                    if (width == 0)
                        for (int i = 0; i < word.Length; i++)
                        {

                            //Begin of word
                            begin = i == 0 ? index : begin;
                            //word width
                            width = index == buffer.Column ? word.Length : 0;

                            index = width == 0 ? index + 1 : index;
                        }
                }

                Vector2 Pixels = ToPixelLine(new Vector2(begin, buffer.Line));
                width *= FontSizeXY.x;

                highLightSelections = new List<Rect>() { new Rect(Pixels.x, Pixels.y, width, FontSizeXY.y) };

            }
            //TODO
            if (Event.current.type == EventType.MouseDrag)
            {
            }

            //Draw Selection
            if (isSelection)
            {
                foreach(Rect r in highLightSelections)
                {
                    GUI.Box(r, GUIContent.none, Style.Selection);
                }
            }
                
        }

        /// <summary>
        /// Focus of Code Editor
        /// </summary>
        private void FocusControl()
        {
            //TODO: FIX!
            GUIUtility.keyboardControl = FocusID;

            FocusID = GUIUtility.GetControlID(Math.Abs(GetHashCode()), FocusType.Keyboard);

            GUIUtility.keyboardControl = Focused ? FocusID : GUIUtility.keyboardControl;

            Focused = (FocusID > 0) ?
                (GUIUtility.keyboardControl == FocusID) : false;

        }

        /// <summary>
        /// Keies the board controller.
        /// </summary>
        public void KeyBoardController()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (!isSelection)
                        buffer.RemoveText();
                    else
                    {
                        foreach(Rect r in highLightSelections)
                        {
                            buffer.RemoveRange(GetRangeText(r, buffer.CurrentLine));
                        }
                    }
                        


                    SetChanges();

                    isSelection = false;
                }
                //Select all
                else if(e.keyCode == KeyCode.A && e.command)
                {
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {

                }
                else if(e.keyCode == KeyCode.UpArrow)
                {
                    buffer.GoUp();
                }
                else if (e.keyCode == KeyCode.DownArrow)
                {
                    buffer.GoDown();
                }
                else if (e.keyCode == KeyCode.LeftArrow)
                {
                    buffer.GoLeft();
                }
                else if (e.keyCode == KeyCode.RightArrow)
                {
                    buffer.GoRight();
                }
                //Get any key
                else if(e.keyCode == KeyCode.None)
                {
                    char c = Convert.ToChar(e.character.ToString());
                    c = e.shift ? char.ToUpper(c) : c;
                    //Remove Text if has selection text.
                    if (isSelection)
                    {
                        foreach (Rect r in highLightSelections)
                        {
                            buffer.RemoveRange(GetRangeText(r, buffer.CurrentLine));
                        }
                        isSelection = false;
                    }
                    buffer.InsertText(c);

                    SetChanges();
                }

                e.Use();
            }
        }

        /// <summary>
        /// Sets the changes.
        /// </summary>
        private void SetChanges()
        {
            buffer.SaveCodeToBuffer();
        }
        /// <summary>
        /// Gets the range text selected
        /// </summary>
        /// <returns>Array int X Y</returns>
        /// <param name="range">Range of Selection</param>
        /// <param name="text">Text</param>
        private int[] GetRangeText(Rect range, string text)
        {
            //Transform selection rect to coordenadies column number
            Vector2 RangeMin = ToNumberLine(range.xMin, range.y);
            Vector2 RangeMax = ToNumberLine(range.xMax, range.y);

            int begin = buffer.GetIndexColumn((int)RangeMin.x, text);
            int end = buffer.GetIndexColumn((int)RangeMax.x, text);

            return new int[] { begin, end };
        }

        /// <summary>
        /// Convert mouse position to line number
        /// </summary>
        /// <returns>Return PositionXY with Column Line Number (X) and Number Line (y)</returns>
        public Vector2 ToNumberLine(float column, float line)
        {
            line = Math.Min((line - LayoutRect.y + Padding.y) / FontSizeXY.y, buffer.TotalLines - 1);

            column = (column - Padding.x) / FontSizeXY.x;

            line = line == 0 ? 1 : line;

            return new Vector2(column, line);
        }

        /// <summary>
        /// Convert Line Number to Pixel for Inspector.
        /// </summary>
        /// <returns>The pixel line.</returns>
        /// <param name="column">Column.</param>
        public Vector2 ToPixelLine(Vector2 PositionLine)
        {
            //Calculate the column position times the font size plus padding spacing;
            int Column = (int)((FontSizeXY.x * PositionLine.x) + Padding.x);
            int Line = (int)((FontSizeXY.y * PositionLine.y + 1) + LayoutRect.y - Padding.y);

            //Limit the column position to padding spacing;
            Column = (int)(Column < Padding.x ? Padding.x : Column);

            //Limit begin of line
            Line = (int)(Line < LayoutRect.yMin ? LayoutRect.yMin + 5 : Line);

            return new Vector2(Column, Line);
        }

        /// <summary>
        /// Replace Tabs to whitespaces
        /// </summary>
        /// <param name="value">Value.</param>
        private string TabToSpace(string value)
        {
            return value.Replace("\t", "    ");
        }

        /// <summary>
        /// Repaint inspector
        /// </summary>
        private void Repaint()
        {
            if (RepaintAction != null)
                RepaintAction();
        }

    }


    /// <summary>
    /// Editor view styles.
    /// </summary>
    public class EditorViewStyles
    {
        private readonly EditorViewOptions options;
        private GUIStyle background;
        private GUIStyle font;
        private GUIStyle backgroundLines;
        private GUIStyle numberLines;
        private GUIStyle highLine;
        private GUIStyle selection;
        private GUIStyle cursor;
        private GUIStyle interpreter;
        private static readonly char[] OperatorCharacters = "+-*/%<>=!&|^~?:.,()[]{}".ToCharArray();


        public bool BlockComment, LineComment, IsString = false;

        private string WhichQuote, triplequotes = string.Empty;

        public EditorViewStyles(EditorViewOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public GUIStyle Background
        {
            get
            {
                if (background == null)
                {
                    background = new GUIStyle();
                    background.normal.background = TextureColor(options.Palette.WindowBackground);
                    return background;
                }

                return background;
            }
        }

        public GUIStyle FontGUIStyle
        {
            get
            {
                if (font == null)
                {
                    font = new GUIStyle();
                    //font.font = (Font)AssetDatabase.LoadMainAssetAtPath("Assets/Font/Monaco12.ttf");
                    font.fontSize = options.FontSize;
                    font.normal.textColor = options.Palette.DefaultText;
                    return font;
                }

                return font;
            }
        }

        public GUIStyle BackgroundLines
        {
            get
            {
                if (backgroundLines == null)
                {
                    backgroundLines = new GUIStyle();
                    backgroundLines.normal.background = TextureColor(options.Palette.LineNumberBackground);
                    return backgroundLines;
                }

                return backgroundLines;
            }
        }

        public GUIStyle NumberLines
        {
            get
            {
                if (numberLines == null)
                {
                    numberLines = new GUIStyle();
                    numberLines.normal.textColor = options.Palette.LineNumberText;
                    numberLines.font = FontGUIStyle.font;
                    numberLines.alignment = TextAnchor.UpperRight;
                    numberLines.fontSize = options.FontSize;
                    return numberLines;
                }

                return numberLines;
            }
        }

        public GUIStyle HighLine
        {

            get
            {
                if (highLine == null)
                {
                    highLine = new GUIStyle();
                    highLine.normal.background = TextureColor(options.Palette.LineHighlight);
                    return highLine;
                }

                return highLine;
            }
        }

        public GUIStyle Selection
        {

            get
            {
                if (selection == null)
                {
                    selection = new GUIStyle();
                    selection.normal.background = TextureColor(options.Palette.Selection);
                    return selection;
                }

                return selection;
            }
        }

        public GUIStyle Cursor
        {

            get
            {
                if (cursor == null)
                {
                    cursor = new GUIStyle();
                    cursor.normal.background = TextureColor(options.Palette.Cursor);
                    return cursor;
                }

                return cursor;
            }
        }

        public GUIStyle Interpreter
        {

            get
            {
                if (interpreter == null)
                {
                    interpreter = new GUIStyle();
                    ;
                    interpreter.font = FontGUIStyle.font;
                    interpreter.fontSize = options.FontSize;
                    interpreter.normal.textColor = options.Palette.DefaultText;
                    return interpreter;
                }

                return interpreter;
            }
        }

        /// <summary>
        /// Checks the word style.
        /// </summary>
        /// <returns>The word style.</returns>
        /// <param name="word">Word.</param>
        /// <param name="IsComment">If set to <c>true</c> comment.</param>
        public Color32 CheckWordStyle(string word)
        {
            LineComment = !LineComment ? word.StartsWith("#") : LineComment;

            bool IsParameter = word.StartsWith("$");

            bool IsFunction = options.FunctionLookup.Contains(word);

            bool IsCodeBlock = word.StartsWith("[[") && word.EndsWith("]]");

            bool IsCodeFlag = word.StartsWith("{{") && word.EndsWith("}}");

            return LineComment ? options.Palette.Comment
                : IsParameter ? options.Palette.Parameter
                    //Block Comment
                : BlockCommentStyle(word) ? options.Palette.String
                    //Strings
                : StringStyle(word) ? options.Palette.String
                    //Keywords
                : options.KeywordLookup.Contains(word) ? options.Palette.Keyword
                : IsFunction ? options.Palette.Function

                : IsCodeBlock ? options.Palette.InlineCode
                : IsCodeFlag ? options.Palette.Flag
                : IsOperator(word) ? options.Palette.Operator
                    //Default
                : options.Palette.DefaultText;
        }

        /// <summary>
        /// Match block of comment e.g.: """ this is a comment in python """
        /// </summary>
        /// <returns><c>true</c>, if comment style was blocked, <c>false</c> otherwise.</returns>
        /// <param name="word">Word.</param>
        private bool BlockCommentStyle(string word)
        {
            if (Regex.IsMatch(word, "([\"'])"))
            {
                triplequotes += word;

                if (Regex.IsMatch(triplequotes, "([\"]{3})|([\']{3})"))
                {
                    BlockComment = !BlockComment;
                    triplequotes = string.Empty;

                    return true;
                }

                return BlockComment;
            }
            else
                //Reset Quotes
                triplequotes = string.Empty;

            return BlockComment;
        }

        /// <summary>
        /// Match a strings quotes e.g.: "this is a string"
        /// </summary>
        /// <returns><c>true</c>, if checker was strung, <c>false</c> otherwise.</returns>
        /// <param name="word">Word.</param>
        private bool StringStyle(string word)
        {
            //Match quotes checker.
            if (Regex.IsMatch(word, "([\"'])") && !BlockComment)
            {
                //Check double quotes or single quotes.
                IsString = string.IsNullOrEmpty(WhichQuote) ? true : word != WhichQuote;

                //Check if close with the first quotes.  
                WhichQuote = string.IsNullOrEmpty(WhichQuote) ? word
                    : !IsString ? string.Empty
                    : WhichQuote;
                return true;
            }

            return IsString;
        }

        private bool IsOperator(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }

            return word.All(character => OperatorCharacters.Contains(character));
        }

        /// <summary>
        /// Resets styles on new line
        /// </summary>
        public void ResetLineStyles()
        {
            IsString = false;
            LineComment = false;
            WhichQuote = string.Empty;
            triplequotes = string.Empty;
        }

        /// <summary>
        /// Applies a color to a texture.
        /// </summary>
        /// <returns>The color.</returns>
        /// <param name="color">Color.</param>
        private static Texture2D TextureColor(Color color)
        {
            Texture2D TextureColor = new Texture2D(1, 1);
            TextureColor.SetPixels(new Color[] { color });
            TextureColor.Apply();
            TextureColor.hideFlags = HideFlags.HideAndDontSave;
            return TextureColor;
        }
    }


    [Serializable]
    public class ScriptEditorBuffer
    {
        /// <summary>
        /// Current Number Line
        /// </summary>
        public int Line;

        /// <summary>
        /// Current Number Column;
        /// </summary>
        public int Column;

        /// <summary>
        /// Current Number Column Index
        /// </summary>
        public int ColumnIndex;

        /// <summary>
        /// The total lines.
        /// </summary>
        public int TotalLines = 1;

        /// <summary>
        /// List with all lines and all words
        /// </summary>
        public List<List<string>> Lines = new List<List<string>>();

        public string CodeBuffer = string.Empty;
        public string CurrentLine = string.Empty;
        public string InterpreterBuffer = string.Empty;
        public string InterpreterBlock = string.Empty;

        public bool InterpreterView;
        public bool BlockInspector;

        public ScriptEditorBuffer Initialize()
        {
            this.Lines = new List<List<string>>();
            this.CurrentLine = string.Empty;
            this.CodeBuffer = "import UnityEngine as unity";

            SetColumnIndex();

            return this;
        }

        public ScriptEditorBuffer Initialize(string expression)
        {
            this.Lines = new List<List<string>>();
            this.CurrentLine = string.Empty;
            this.CodeBuffer = expression;

            if (this.CodeBuffer == null)
                this.CodeBuffer = "";

            SetColumnIndex();

            return this;
        }

        /// <summary>
        /// Initialize the specified Line and Column.
        /// </summary>
        /// <param name="Line">Line.</param>
        /// <param name="Column">Column.</param>
        public void Initialize(int Line, int Column)
        {
            this.Line = Line;
            this.Column = Column;
            this.CurrentLine = GetLine(this.Line);
            SetColumnIndex();
        }

        /// <summary>
        /// Sets the index of the column.
        /// </summary>
        public void SetColumnIndex()
        {
            Trim();

            ColumnIndex = GetIndexColumn(this.Column, this.CurrentLine);

            ColumnIndex = CurrentLine.Length == 0 ? 0
            : ColumnIndex > CurrentLine.Length ? CurrentLine.Length
            : ColumnIndex;
        }

        /// <summary>
        /// Gets the index column.
        /// </summary>
        /// <returns>The index column.</returns>
        /// <param name="column">Column.</param>
        public int GetIndexColumn(int column, string line)
        {
            if (column == 0)
                return 0;

            int index = 0;
            for (int i = 0; i <= line.Length; i++)
            {

                if (line.Length > 0)
                {
                    index = line[i] == '\t' ? index + 4 : index + 1;
                    if (index >= column)
                        return ++i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Gos up.
        /// </summary>
        public void GoUp()
        {
            if (Line == 0)
                return;

            Line--;

            CurrentLine = GetLine(Line);

            SetColumnIndex();
        }

        /// <summary>
        /// Gos down.
        /// </summary>
        public void GoDown()
        {
            Line++;

            CurrentLine = GetLine(Line);

            SetColumnIndex();
        }

        /// <summary>
        /// Gos the left.
        /// </summary>
        public void GoLeft()
        {

            if (Column == 0)
                Line--;

            if (Event.current.command)
                //Go to begin of line
                Column = Regex.Match(TabToSpace(CurrentLine), @"\w").Index;
            else
            {

                Column = Column > 0 ? Column - 1 : GetLine(Line).Length;

                Column = GetCharIndex(ColumnIndex - 1) == '\t' ? Column - 3 : Column;
            }

            SetColumnIndex();
        }

        /// <summary>
        /// Gos the right.
        /// </summary>
        public void GoRight()
        {
            if (Event.current.command)
                //Go to end of line 
                Column = TabToSpace(CurrentLine).Length;
            else if (ColumnIndex >= CurrentLine.Length && !InterpreterView)
            {
                GoDown();
                Column = Regex.Match(TabToSpace(CurrentLine), @"\w").Index;
            }
            else
                Column = GetCharIndex(ColumnIndex) == '\t' ? Column + 4 : Column + 1;

            SetColumnIndex();
        }

        /// <summary>
        /// Inserts the line.
        /// </summary>
        /// <param name="NumberLine">Number line.</param>
        public void InsertLine(int NumberLine)
        {
            this.Lines.Insert(NumberLine, new List<string>());
        }

        /// <summary>
        /// Inserts the text.
        /// </summary>
        /// <param name="c">char</param>
        public void InsertText(char c)
        {
            string StringJoin = this.CurrentLine;

            //Insert new Line
            if (c == '\n')
            {

                string LeftText = string.Empty;
                string RightText = string.Empty;

                for (int i = 0; i < StringJoin.Length; i++)
                {
                    if (i >= this.ColumnIndex)
                        RightText += StringJoin[i];
                    else
                        LeftText += StringJoin[i];
                }

                Match spaces = Regex.Match(LeftText, @"(^\t+)|(^\s+)");

                RightText = RightText.Insert(0, spaces.Value);

                UpdateLineText(Line, LeftText);

                InsertLine(Line);

                UpdateLineText(++Line, RightText);

                TotalLines++;

                //Jump cursor after indentation.
                Match Indentation = Regex.Match(TabToSpace(LeftText), @"\w");
                Column = Indentation.Success ? Indentation.Index : Column;
                SetColumnIndex();

            }
            else
            {

                //Insert a single char          
                string newChar = (c.ToString());

                StringJoin = StringJoin.Insert(ColumnIndex, newChar);

                UpdateLineText(Line, StringJoin);

                Column = c == '\t' ? Column + 4 : ++Column;
                SetColumnIndex();
            }
        }

        /// <summary>
        /// Inserts the text interpreter.
        /// </summary>
        /// <param name="c">char</param>
        public void InsertTextInterpreter(char c)
        {
            CurrentLine = CurrentLine.Insert(ColumnIndex, c.ToString());
            Column = c == '\t' ? Column + 4 : ++Column;
            SetColumnIndex();
        }

        /// <summary>
        /// Appends the interpreter.
        /// </summary>
        /// <param name="output">Output string</param>
        public void AppendInterpreter(string output)
        {
            StringBuilder sb = new StringBuilder("\n" + InterpreterBuffer);
            string blockSeparator = this.BlockInspector ? "..." : ">>> ";
            string BreakLine = String.IsNullOrEmpty(output) ? "" : "\n";
            sb.Insert(0, output + BreakLine + blockSeparator + CurrentLine);

            InterpreterBuffer = sb.ToString();
            CurrentLine = string.Empty;
            SetColumnIndex();
        }

        /// <summary>
        /// Remove one char from line
        /// </summary>
        public void RemoveText()
        {
            if (!ElementInList(Line) && !InterpreterView)
                return;

            if (Column > 0)
            {
                //Remove single char.
                GoLeft();
                string LineTab = CurrentLine;

                LineTab = LineTab.Remove(Math.Max(0, ColumnIndex), 1);
                UpdateLineText(Line, LineTab);

            }
            else if (!InterpreterView && Line != 1)
            {
                //Remove line.
                Lines[Line - 2].Add(CurrentLine);

                Lines.RemoveAt(Line - 1);
                GoUp();

                Column = GetLine(Line - 1).IndexOf(CurrentLine);
                SetColumnIndex();
            }
        }

        /// <summary>
        /// Removes the range of text
        /// </summary>
        public void RemoveRange(int[] range)
        {
            CurrentLine = (CurrentLine).Remove(range[0], range[1] - range[0]);

            UpdateLineText(Line, CurrentLine);

            Column = range[0] + Regex.Matches(CurrentLine.Substring(0, range[0]), @"(\t)").Count * 3;
            SetColumnIndex();
        }

        /// <summary>
        /// Updates the line text.
        /// </summary>
        /// <param name="LineNumber">Line number.</param>
        /// <param name="LineText">Line text.</param>
        private void UpdateLineText(int NumberLine, string LineText)
        {
            if (InterpreterView)
            {
                CurrentLine = LineText;
                return;
            }

            if (!ElementInList(NumberLine))
                return;

            Lines[NumberLine - 1] = new List<string>();

            foreach (Match results in Regex.Matches(LineText, @"(\t)|(\w+)|(\s+)|(.)"))
                Lines[NumberLine - 1].Add(results.Value);

            CurrentLine = LineText;
        }

        /// <summary>
        /// Saves the code to memory.
        /// </summary>
        public void SaveCodeToBuffer()
        {
            StringBuilder text = new StringBuilder();

            foreach (List<String> Line in Lines)
            {
                foreach (string word in Line)
                    text.Append(word);

                text.AppendLine();
            }

            CodeBuffer = text.ToString();
        }

        /// <summary>
        /// Gets the index of the char.
        /// </summary>
        /// <returns>The char index.</returns>
        /// <param name="index">Index.</param>
        public char GetCharIndex(int index)
        {
            return CurrentLine.ElementAtOrDefault(index);
        }

        /// <summary>
        /// Gets the line.
        /// </summary>
        /// <returns>The line.</returns>
        /// <param name="NumberLine">Number line.</param>
        public string GetLine(int NumberLine)
        {
            if (!ElementInList(NumberLine))
                return string.Empty;

            return string.Join("", Lines[NumberLine - 1].ToArray());
        }

        /// <summary>
        /// Elements the in list.
        /// </summary>
        /// <returns><c>true</c>, if in list was elemented, <c>false</c> otherwise.</returns>
        /// <param name="NumberLine">Number line.</param>
        private bool ElementInList(int NumberLine)
        {
            return this.Lines.ElementAtOrDefault(NumberLine - 1) != null;
        }

        /// <summary>
        /// Limit Column of Cursor
        /// </summary>
        /// <param name="Position">Column and Line position.</param>
        public void Trim()
        {
            string line = CurrentLine.Replace("\t", "    ");

            if (!InterpreterView)
            {
                Line = Line >= TotalLines ? TotalLines - 1 : Line;

                Column = Column > line.Length ? line.Length : Column;

            }
            else
                Column = Column > line.Length ? CurrentLine.Length : Column;

        }

        /// <summary>
        /// Replace Tabs to whitespaces
        /// </summary>
        /// <param name="value">Value.</param>
        private string TabToSpace(string value)
        {
            return value.Replace("\t", "    ");
        }

        /// <summary>
        /// String Class representation
        /// </summary>
        /// <returns>string represented the current instance</returns>
        public override string ToString()
        {
            return string.Format("Line: {0}, Column: {1}, ColumnIndex: {2}, CurrentLine: {3}, TotalLine: {4}", Line, Column, ColumnIndex, CurrentLine, TotalLines);
        }

    }

}
