using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Joshcamas.CodeEditor
{
    [Serializable]
    public class EditorView
    {
        public ScriptEditorBuffer buffer;

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

        private EditorViewStyles Style = new EditorViewStyles();

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

            buffer.Initialize(rawScript);
        }

        /// <summary>
        /// Editors the view controll.
        /// </summary>
        public void EditorViewGUI()
        {
            //Get box rect and background
            GetBoxRect();
            //Begin ScrollView of box
            PositionScroll = GUI.BeginScrollView(new Rect(0, LayoutRect.y, LayoutRect.width + 15, LayoutRect.height),
                    PositionScroll, new Rect(0, LayoutRect.yMin, LayoutRect.xMax, 23 * buffer.TotalLines));
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

            //Background ColorScheme
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

}
