using System;
using System.Collections.Generic;

namespace FormulaKit.Runtime
{
    public enum FormulaTokenKind
    {
        Whitespace,
        Newline,
        Identifier,
        Keyword,
        FunctionName,
        Number,
        Operator,
        Punctuation,
        LineComment,
        Unknown
    }

    public readonly struct FormulaToken
    {
        public FormulaTokenKind Kind { get; }
        public int Start { get; }
        public int Length { get; }

        public FormulaToken(FormulaTokenKind kind, int start, int length)
        {
            Kind = kind;
            Start = start;
            Length = length;
        }

        public int End => Start + Length;
    }

    public static class FormulaTokenizer
    {
        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "let", "if", "else", "return", "true", "false"
        };

        public static IReadOnlyList<FormulaToken> Tokenize(string source)
        {
            var tokens = new List<FormulaToken>();
            if (string.IsNullOrEmpty(source))
            {
                return tokens;
            }

            int i = 0;
            int length = source.Length;

            while (i < length)
            {
                char c = source[i];

                if (c == '\r' || c == '\n')
                {
                    int start = i;
                    if (c == '\r' && i + 1 < length && source[i + 1] == '\n')
                    {
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                    tokens.Add(new FormulaToken(FormulaTokenKind.Newline, start, i - start));
                    continue;
                }

                if (c == ' ' || c == '\t')
                {
                    int start = i;
                    while (i < length && (source[i] == ' ' || source[i] == '\t'))
                    {
                        i++;
                    }
                    tokens.Add(new FormulaToken(FormulaTokenKind.Whitespace, start, i - start));
                    continue;
                }

                if (c == '#')
                {
                    int start = i;
                    while (i < length && source[i] != '\n' && source[i] != '\r')
                    {
                        i++;
                    }
                    tokens.Add(new FormulaToken(FormulaTokenKind.LineComment, start, i - start));
                    continue;
                }

                if (char.IsDigit(c) || (c == '.' && i + 1 < length && char.IsDigit(source[i + 1])))
                {
                    int start = i;
                    bool sawDot = false;
                    while (i < length)
                    {
                        char ch = source[i];
                        if (char.IsDigit(ch))
                        {
                            i++;
                        }
                        else if (ch == '.' && !sawDot)
                        {
                            sawDot = true;
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    tokens.Add(new FormulaToken(FormulaTokenKind.Number, start, i - start));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < length && (char.IsLetterOrDigit(source[i]) || source[i] == '_'))
                    {
                        i++;
                    }
                    string word = source.Substring(start, i - start);
                    FormulaTokenKind kind = Keywords.Contains(word)
                        ? FormulaTokenKind.Keyword
                        : FormulaFunctions.IsKnown(word)
                            ? FormulaTokenKind.FunctionName
                            : FormulaTokenKind.Identifier;
                    tokens.Add(new FormulaToken(kind, start, i - start));
                    continue;
                }

                if (IsOperatorChar(c))
                {
                    int start = i;
                    while (i < length && IsOperatorChar(source[i]))
                    {
                        i++;
                    }
                    tokens.Add(new FormulaToken(FormulaTokenKind.Operator, start, i - start));
                    continue;
                }

                if (IsPunctuationChar(c))
                {
                    tokens.Add(new FormulaToken(FormulaTokenKind.Punctuation, i, 1));
                    i++;
                    continue;
                }

                tokens.Add(new FormulaToken(FormulaTokenKind.Unknown, i, 1));
                i++;
            }

            return tokens;
        }

        private static bool IsOperatorChar(char c)
        {
            switch (c)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '%':
                case '=':
                case '<':
                case '>':
                case '!':
                case '&':
                case '|':
                case '^':
                case '?':
                case ':':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsPunctuationChar(char c)
        {
            switch (c)
            {
                case '(':
                case ')':
                case '{':
                case '}':
                case '[':
                case ']':
                case ',':
                case ';':
                case '.':
                    return true;
                default:
                    return false;
            }
        }
    }
}
