using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace Joshcamas.CodeEditor
{
    /// <summary>
    /// Editor view styles.
    /// </summary>
    public class EditorViewStyles
    {
        private GUIStyle background;
        private GUIStyle font;
        private GUIStyle backgroundLines;
        private GUIStyle numberLines;
        private GUIStyle highLine;
        private GUIStyle selection;
        private GUIStyle cursor;
        private GUIStyle interpreter;

        /// <summary>
        /// Python keywords
        /// </summary>
        private static string[] KeyWords = new string[]
        {
            "False", "None", "True", "and", "as", "assert", "break", "class", "continue", "def", "del", "elif",
            "else", "except", "finally", "for", "from", "global", "if", "import", "in", "is", "lambda", "nonlocal",
            "not", "or", "pass", "raise", "return", "try", "while", "with", "yield", "self"
        };


        public bool BlockComment, LineComment, IsString = false;

        private string WhichQuote, triplequotes = string.Empty;

        public GUIStyle Background
        {
            get
            {
                if (background == null)
                {
                    background = new GUIStyle();
                    background.normal.background = TextureColor(ColorScheme.Background2);
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
                    font.fontSize = 12;
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
                    backgroundLines.normal.background = TextureColor(new Color32(15, 15, 15, 255));
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
                    numberLines.normal.textColor = Color.white;
                    numberLines.font = FontGUIStyle.font;
                    numberLines.alignment = TextAnchor.UpperRight;
                    numberLines.fontSize = 12;
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
                    highLine.normal.background = TextureColor(new Color32(255, 255, 255, 25));
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
                    selection.normal.background = TextureColor(new Color32(255, 255, 255, 45));
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
                    cursor.normal.background = TextureColor(new Color(255, 255, 255, 0.8f));
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
                    interpreter.fontSize = 14;
                    interpreter.normal.textColor = new Color(255, 255, 255, 1);
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

            bool IsCodeBlock = word.StartsWith("[[") && word.EndsWith("]]");

            bool IsCodeFlag = word.StartsWith("{{") && word.EndsWith("}}");

            return LineComment ? ColorScheme.Gray
                : IsParameter ? ColorScheme.Pink
                    //Block Comment
                : BlockCommentStyle(word) ? ColorScheme.Orange
                    //Strings
                : StringStyle(word) ? ColorScheme.Orange
                    //Keywords
                : KeyWords.Contains(word) ? ColorScheme.Pink

                : IsCodeBlock ? ColorScheme.Green
                : IsCodeFlag ? ColorScheme.LightGray
                    //Default
                : ColorScheme.White;
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
}