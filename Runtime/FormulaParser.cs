using System;
using System.Collections.Generic;
using System.Text;
using FormulaKit.Runtime.Nodes;
using UnityEngine;

namespace FormulaKit.Runtime
{
    /// <summary>
    /// Unified formula parser supporting both simple expressions and advanced features.
    /// Automatically detects and parses: variables, conditionals, comparisons, logical operators, etc.
    /// </summary>
    public readonly struct FormulaParseResult
    {
        public Formula Formula { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public int ErrorLine { get; }
        public int ErrorColumn { get; }

        private FormulaParseResult(Formula formula, bool isSuccess, string error, int line, int column)
        {
            Formula = formula;
            IsSuccess = isSuccess;
            ErrorMessage = error;
            ErrorLine = line;
            ErrorColumn = column;
        }

        public static FormulaParseResult Success(Formula formula) =>
            new FormulaParseResult(formula, true, null, 0, 0);

        public static FormulaParseResult Error(string message, int line, int column) =>
            new FormulaParseResult(null, false, message, line, column);
    }

    public class FormulaParser
    {
        private string _expression;
        private int _position;
        private HashSet<string> _inputVariables;
        private HashSet<string> _localVariables;
        private readonly IRandomProvider _randomProvider;
        private bool _suppressLogging;

        public FormulaParser(IRandomProvider randomProvider = null)
        {
            _randomProvider = randomProvider ?? new DefaultRandomProvider();
        }

        public Formula Parse(string formulaExpression)
        {
            _expression = formulaExpression;
            _position = 0;
            _inputVariables = new HashSet<string>();
            _localVariables = new HashSet<string>();

            try
            {
                var rootNode = ParseStatements();

                return new Formula(formulaExpression, rootNode, _inputVariables);
            }
            catch (FormatException ex)
            {
                throw CreateDetailedParseException(ex);
            }
            catch (Exception ex)
            {
                LogUnexpectedParserException(ex);
                throw;
            }
        }

        public FormulaParseResult TryParse(string formulaExpression)
        {
            _suppressLogging = true;
            try
            {
                var formula = Parse(formulaExpression);
                return FormulaParseResult.Success(formula);
            }
            catch (FormatException ex)
            {
                var ctx = BuildErrorContext();
                return FormulaParseResult.Error(ex.Message, ctx.line, ctx.column);
            }
            catch (Exception ex)
            {
                var ctx = BuildErrorContext();
                return FormulaParseResult.Error(ex.Message, ctx.line, ctx.column);
            }
            finally
            {
                _suppressLogging = false;
            }
        }

        // ============== STATEMENT PARSING ==============

        private IFormulaNode ParseStatements()
        {
            var statements = new List<IFormulaNode>();

            SkipWhitespaceAndNewlines();

            while (_position < _expression.Length)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }

                SkipWhitespaceAndNewlines();

                // Optional semicolon
                if (Peek() == ';')
                {
                    Read();
                    SkipWhitespaceAndNewlines();
                }
            }

            return statements.Count switch
            {
                0 => new ConstantNode(0f),
                1 => statements[0],
                _ => new SequenceNode(statements.ToArray())
            };
        }

        private IFormulaNode ParseStatement()
        {
            SkipWhitespaceAndNewlines();

            if (_position >= _expression.Length)
            {
                return null;
            }

            // Check for 'let' declaration
            if (PeekWord() == "let")
            {
                return ParseDeclaration();
            }

            // Check for 'if' statement
            if (PeekWord() == "if")
            {
                return ParseIfStatement();
            }

            // Check for 'return' statement
            if (PeekWord() == "return")
            {
                return ParseReturnStatement();
            }

            // Check for block
            if (Peek() == '{')
            {
                return ParseBlock();
            }

            // Otherwise, it's an expression or assignment
            return ParseAssignmentOrExpression();
        }

        private IFormulaNode ParseReturnStatement()
        {
            ConsumeWord("return");
            SkipWhitespace();

            // 'return' alone returns 0; 'return <expr>' returns the expression's value.
            if (_position >= _expression.Length)
            {
                return new ReturnNode(new ConstantNode(0f));
            }

            char next = Peek();
            if (next == ';' || next == '\n' || next == '\r' || next == '}')
            {
                return new ReturnNode(new ConstantNode(0f));
            }

            var value = ParseExpression();
            return new ReturnNode(value);
        }

        private IFormulaNode ParseDeclaration()
        {
            ConsumeWord("let");
            SkipWhitespace();

            var varName = ParseIdentifier();
            _localVariables.Add(varName);

            SkipWhitespace();

            IFormulaNode initialValue = null;
            if (Peek() == '=')
            {
                Read();
                SkipWhitespace();
                initialValue = ParseExpression();
            }

            return new DeclarationNode(varName, initialValue);
        }

        private IFormulaNode ParseIfStatement()
        {
            ConsumeWord("if");
            SkipWhitespace();

            if (Peek() != '(')
            {
                throw new FormatException("Expected '(' after 'if'");
            }
            Read();

            var condition = ParseExpression();

            if (Peek() != ')')
            {
                throw new FormatException("Expected ')' after if condition");
            }
            Read();

            SkipWhitespace();

            var thenBranch = ParseStatement();
            IFormulaNode elseBranch = null;

            SkipWhitespaceAndNewlines();

            if (PeekWord() == "else")
            {
                ConsumeWord("else");
                SkipWhitespace();
                elseBranch = ParseStatement();
            }

            return new ConditionalNode(condition, thenBranch, elseBranch);
        }

        private IFormulaNode ParseBlock()
        {
            if (Peek() != '{')
            {
                throw new FormatException("Expected '{'");
            }
            Read();

            var statements = new List<IFormulaNode>();

            SkipWhitespaceAndNewlines();

            while (_position < _expression.Length && Peek() != '}')
            {
                IFormulaNode statement = ParseStatement();
                if (statement != null)
                {
                    statements.Add(statement);
                }

                SkipWhitespaceAndNewlines();

                if (Peek() == ';')
                {
                    Read();
                    SkipWhitespaceAndNewlines();
                }
            }

            if (Peek() != '}')
            {
                throw new FormatException("Expected '}'");
            }
            Read();

            if (statements.Count == 0)
            {
                return new NoOpNode();
            }
            else if (statements.Count == 1)
            {
                return statements[0];
            }
            else
            {
                return new SequenceNode(statements.ToArray());
            }
        }

        private IFormulaNode ParseAssignmentOrExpression()
        {
            // Try to detect assignment
            var savedPosition = _position;
            
            // Look ahead to see if this is an assignment
            if (!char.IsLetter(Peek()))
            {
                return ParseExpression();
            }
            var identifier = ParseIdentifier();
            SkipWhitespace();

            // Check for assignment operators
            if (Peek() == '=' && PeekNext() != '=') // = but not ==
            {
                Read(); // consume '='
                SkipWhitespace();
                var value = ParseExpression();
                    
                if (!_localVariables.Contains(identifier))
                {
                    _localVariables.Add(identifier);
                }
                    
                return new AssignmentNode(identifier, value, AssignmentNode.AssignmentOperator.Assign);
            }
            else if (Peek() == '+' && PeekNext() == '=')
            {
                Read(); Read(); // consume '+='
                SkipWhitespace();
                var value = ParseExpression();
                return new AssignmentNode(identifier, value, AssignmentNode.AssignmentOperator.AddAssign);
            }
            else if (Peek() == '-' && PeekNext() == '=')
            {
                Read(); Read(); // consume '-='
                SkipWhitespace();
                var value = ParseExpression();
                return new AssignmentNode(identifier, value, AssignmentNode.AssignmentOperator.SubAssign);
            }
            else if (Peek() == '*' && PeekNext() == '=')
            {
                Read(); Read(); // consume '*='
                SkipWhitespace();
                var value = ParseExpression();
                return new AssignmentNode(identifier, value, AssignmentNode.AssignmentOperator.MulAssign);
            }
            else if (Peek() == '/' && PeekNext() == '=')
            {
                Read(); Read(); // consume '/='
                SkipWhitespace();
                var value = ParseExpression();
                return new AssignmentNode(identifier, value, AssignmentNode.AssignmentOperator.DivAssign);
            }

            // Not an assignment, restore position and parse as expression
            _position = savedPosition;

            return ParseExpression();
        }

        // ============== EXPRESSION PARSING ==============

        private IFormulaNode ParseExpression()
        {
            return ParseTernary();
        }

        private IFormulaNode ParseTernary()
        {
            var node = ParseLogicalOr();

            SkipWhitespace();
            if (Peek() == '?')
            {
                Read();
                SkipWhitespace();
                var trueValue = ParseLogicalOr();
                SkipWhitespace();
                
                if (Peek() != ':')
                {
                    throw new FormatException("Expected ':' in ternary operator");
                }                
                Read();
                SkipWhitespace();
                
                var falseValue = ParseTernary(); // Right associative
                
                return new TernaryNode(node, trueValue, falseValue);
            }

            return node;
        }

        private IFormulaNode ParseLogicalOr()
        {
            var node = ParseLogicalAnd();

            SkipWhitespace();
            while (Peek() == '|' && PeekNext() == '|')
            {
                Read(); Read();
                SkipWhitespace();
                var right = ParseLogicalAnd();
                node = new LogicalNode(node, right, LogicalNode.LogicalOperator.Or);
                SkipWhitespace();
            }

            return node;
        }

        private IFormulaNode ParseLogicalAnd()
        {
            var node = ParseComparison();

            SkipWhitespace();
            while (Peek() == '&' && PeekNext() == '&')
            {
                Read(); Read();
                SkipWhitespace();
                var right = ParseComparison();
                node = new LogicalNode(node, right, LogicalNode.LogicalOperator.And);
                SkipWhitespace();
            }

            return node;
        }

        private IFormulaNode ParseComparison()
        {
            var node = ParseAdditive();

            SkipWhitespace();
            
            // Check for comparison operators
            if (Peek() == '<')
            {
                if (PeekNext() == '=')
                {
                    Read(); Read();
                    SkipWhitespace();
                    IFormulaNode right = ParseAdditive();
                    return new ComparisonNode(node, right, ComparisonNode.ComparisonOperator.LessThanOrEqual);
                }
                else
                {
                    Read();
                    SkipWhitespace();
                    IFormulaNode right = ParseAdditive();
                    return new ComparisonNode(node, right, ComparisonNode.ComparisonOperator.LessThan);
                }
            }
            else if (Peek() == '>')
            {
                if (PeekNext() == '=')
                {
                    Read(); Read();
                    SkipWhitespace();
                    var right = ParseAdditive();
                    return new ComparisonNode(node, right, ComparisonNode.ComparisonOperator.GreaterThanOrEqual);
                }
                else
                {
                    Read();
                    SkipWhitespace();
                    var right = ParseAdditive();
                    return new ComparisonNode(node, right, ComparisonNode.ComparisonOperator.GreaterThan);
                }
            }
            else if (Peek() == '=' && PeekNext() == '=')
            {
                Read(); Read();
                SkipWhitespace();
                var right = ParseAdditive();
                return new ComparisonNode(node, right, ComparisonNode.ComparisonOperator.Equal);
            }
            else if (Peek() == '!' && PeekNext() == '=')
            {
                Read(); Read();
                SkipWhitespace();
                var right = ParseAdditive();
                return new ComparisonNode(node, right, ComparisonNode.ComparisonOperator.NotEqual);
            }

            return node;
        }

        private IFormulaNode ParseAdditive()
        {
            IFormulaNode node = ParseMultiplicative();

            SkipWhitespace();
            while (_position < _expression.Length && (Peek() == '+' || Peek() == '-'))
            {
                // Check it's not += or -=
                if ((Peek() == '+' || Peek() == '-') && PeekNext() == '=')
                {
                    break;
                }

                var op = Read();
                SkipWhitespace();
                var right = ParseMultiplicative();

                node = op == '+' ? new BinaryOpNode(node, right, (a, b) => a + b) 
                    : new BinaryOpNode(node, right, (a, b) => a - b);

                SkipWhitespace();
            }

            return node;
        }

        private IFormulaNode ParseMultiplicative()
        {
            IFormulaNode node = ParseExponent();

            SkipWhitespace();
            while (_position < _expression.Length && (Peek() == '*' || Peek() == '/' || Peek() == '%'))
            {
                // Check it's not *= or /=
                if ((Peek() == '*' || Peek() == '/') && PeekNext() == '=')
                {
                    break;
                }

                var op = Read();
                SkipWhitespace();
                var right = ParseExponent();

                node = op switch
                {
                    '*' => new BinaryOpNode(node, right, (a, b) => a * b),
                    '/' => new BinaryOpNode(node, right, (a, b) => a / b),
                    '%' => new ModuloNode(node, right),
                    _ => node
                };

                SkipWhitespace();
            }

            return node;
        }

        private IFormulaNode ParseExponent()
        {
            IFormulaNode node = ParseUnary();

            SkipWhitespace();
            if (_position < _expression.Length && Peek() == '^')
            {
                Read();
                SkipWhitespace();
                IFormulaNode right = ParseExponent(); // Right associative
                node = new BinaryOpNode(node, right, (a, b) => Mathf.Pow(a, b));
            }

            return node;
        }

        private IFormulaNode ParseUnary()
        {
            SkipWhitespace();

            if (_position < _expression.Length && Peek() == '-')
            {
                Read();
                SkipWhitespace();
                return new UnaryOpNode(ParseUnary(), x => -x);
            }

            if (_position < _expression.Length && Peek() == '+')
            {
                Read();
                SkipWhitespace();
                return ParseUnary();
            }

            if (_position < _expression.Length && Peek() == '!')
            {
                Read();
                SkipWhitespace();
                return new LogicalNode(ParseUnary());
            }

            return ParsePrimary();
        }

        private IFormulaNode ParsePrimary()
        {
            SkipWhitespace();

            // Parentheses
            if (Peek() == '(')
            {
                Read();
                SkipWhitespace();
                IFormulaNode node = ParseExpression();
                SkipWhitespace();
                if (Peek() != ')')
                    throw new FormatException("Expected closing parenthesis");
                Read();
                return node;
            }

            // Numbers
            if (char.IsDigit(Peek()) || Peek() == '.')
            {
                return new ConstantNode(ParseNumber());
            }

            // Functions and variables
            if (char.IsLetter(Peek()) || Peek() == '_')
            {
                string identifier = ParseIdentifier();

                // Check for function call
                SkipWhitespace();
                if (_position < _expression.Length && Peek() == '(')
                {
                    return ParseFunction(identifier);
                }

                // It's a variable - track if it's input or local
                if (!_localVariables.Contains(identifier))
                {
                    _inputVariables.Add(identifier);
                }
                
                return new VariableNode(identifier);
            }

            throw new FormatException($"Unexpected character at position {_position}: '{Peek()}'");
        }

        private IFormulaNode ParseFunction(string functionName)
        {
            Read(); // consume '('
            SkipWhitespace();
            
            var arguments = new List<IFormulaNode>();
            
            if (Peek() != ')')
            {
                arguments.Add(ParseExpression());
                
                SkipWhitespace();
                while (Peek() == ',')
                {
                    Read();
                    SkipWhitespace();
                    arguments.Add(ParseExpression());
                    SkipWhitespace();
                }
            }

            if (Peek() != ')')
            {
                throw new FormatException("Expected closing parenthesis in function call");
            }
            Read();

            // Check unary functions
            if (arguments.Count == 1 && TryGetUnaryFunction(functionName, out var unaryFunc))
            {
                return new UnaryOpNode(arguments[0], unaryFunc);
            }

            // Check multi-parameter functions
            if (TryGetMultiFunction(functionName, out var multiFunc))
            {
                return new FunctionNode(arguments.ToArray(), multiFunc);
            }
            
            if (functionName == "rand" && arguments.Count == 1)
            {
                return new RandomIntNode(arguments[0], _randomProvider);
            }

            if (functionName == "randf" && arguments.Count == 1)
            {
                return new RandomFloatNode(arguments[0], _randomProvider);
            }

            if (functionName == "random" && arguments.Count == 0)
            {
                return new RandomValueNode(_randomProvider);
            }

            throw new FormatException($"Unknown function: {functionName}");
        }
      
        private float ParseNumber()
        {
            var sb = new StringBuilder();

            while (_position < _expression.Length && (char.IsDigit(Peek()) || Peek() == '.'))
            {
                sb.Append(Read());
            }

            return float.Parse(sb.ToString());
        }

        private string ParseIdentifier()
        {
            var sb = new StringBuilder();

            while (_position < _expression.Length && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
            {
                sb.Append(Read());
            }

            return sb.ToString();
        }

        // ============== UTILITY METHODS ==============

        private static bool TryGetUnaryFunction(string id, out Func<float, float> func)
            => FormulaFunctions.TryGetUnary(id, out func);

        private static bool TryGetMultiFunction(string id, out Func<float[], float> func)
            => FormulaFunctions.TryGetMulti(id, out func);
        
        private string PeekWord()
        {
            var savedPosition = _position;
            SkipWhitespace();
            
            if (_position >= _expression.Length || !char.IsLetter(Peek()))
            {
                _position = savedPosition;
                return "";
            }

            var word = ParseIdentifier();
            _position = savedPosition;
            return word;
        }

        private void ConsumeWord(string expected)
        {
            SkipWhitespace();
            var word = ParseIdentifier();
            if (word != expected)
            {
                throw new FormatException($"Expected '{expected}' but got '{word}'");
            }
        }

        private void SkipWhitespace()
        {
            while (_position < _expression.Length && (_expression[_position] == ' ' || _expression[_position] == '\t'))
            {
                _position++;
            }
        }

        private FormatException CreateDetailedParseException(FormatException ex)
        {
            var context = BuildErrorContext();
            var messageBuilder = new StringBuilder();
            messageBuilder.Append($"Parse error at line {context.line}, column {context.column}: {ex.Message}");

            if (!string.IsNullOrEmpty(context.lineText))
            {
                messageBuilder.Append('\n');
                messageBuilder.Append(context.lineText);
                messageBuilder.Append('\n');
                messageBuilder.Append(context.pointer);
            }

            messageBuilder.Append("\nExpression:\n");
            messageBuilder.Append(_expression);

            if (!_suppressLogging)
            {
                Debug.LogError($"[FormulaParser] {messageBuilder}");
            }

            return new FormatException(messageBuilder.ToString(), ex);
        }

        private void LogUnexpectedParserException(Exception ex)
        {
            if (_suppressLogging)
            {
                return;
            }

            var context = BuildErrorContext();
            var messageBuilder = new StringBuilder();
            messageBuilder.Append($"Unexpected error at line {context.line}, column {context.column}: {ex.Message}");

            if (!string.IsNullOrEmpty(context.lineText))
            {
                messageBuilder.Append('\n');
                messageBuilder.Append(context.lineText);
                messageBuilder.Append('\n');
                messageBuilder.Append(context.pointer);
            }

            messageBuilder.Append("\nExpression:\n");
            messageBuilder.Append(_expression);

            Debug.LogError($"[FormulaParser] {messageBuilder}");
        }

        private (int line, int column, string lineText, string pointer) BuildErrorContext()
        {
            if (string.IsNullOrEmpty(_expression))
            {
                return (1, 1, string.Empty, "^");
            }

            int position = GetErrorPosition();
            var (line, column) = GetLineAndColumn(position);
            string lineText = GetLineText(position);
            string pointer = BuildPointer(column, lineText.Length);

            return (line, column, lineText, pointer);
        }

        private int GetErrorPosition()
        {
            if (string.IsNullOrEmpty(_expression))
            {
                return 0;
            }

            int maxIndex = Math.Max(0, _expression.Length - 1);
            int clamped = Math.Max(0, Math.Min(_position, maxIndex));

            return clamped;
        }

        private (int line, int column) GetLineAndColumn(int position)
        {
            int line = 1;
            int column = 1;

            for (int i = 0; i < position && i < _expression.Length; i++)
            {
                char current = _expression[i];
                if (current == '\n')
                {
                    line++;
                    column = 1;
                }
                else if (current != '\r')
                {
                    column++;
                }
            }

            return (line, column);
        }

        private string GetLineText(int position)
        {
            if (string.IsNullOrEmpty(_expression))
            {
                return string.Empty;
            }

            int start = position;
            while (start > 0)
            {
                char previous = _expression[start - 1];
                if (previous == '\n' || previous == '\r')
                {
                    break;
                }

                start--;
            }

            int end = position;
            while (end < _expression.Length)
            {
                char current = _expression[end];
                if (current == '\n' || current == '\r')
                {
                    break;
                }

                end++;
            }

            return _expression.Substring(start, end - start);
        }

        private string BuildPointer(int column, int lineLength)
        {
            int maxColumn = lineLength > 0 ? lineLength + 1 : 1;
            int safeColumn = Math.Max(1, Math.Min(column, maxColumn));
            return new string(' ', safeColumn - 1) + '^';
        }

        private void SkipWhitespaceAndNewlines()
        {
            while (_position < _expression.Length && char.IsWhiteSpace(_expression[_position]))
            {
                _position++;
            }
        }

        private char Peek()
        {
            return _position < _expression.Length ? _expression[_position] : '\0';
        }

        private char PeekNext()
        {
            return _position + 1 < _expression.Length ? _expression[_position + 1] : '\0';
        }

        private char Read()
        {
            return _expression[_position++];
        }
    }
}