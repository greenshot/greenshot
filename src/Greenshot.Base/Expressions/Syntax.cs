/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// A parsed expression: the tree is built once and cached by the resolver.
    /// </summary>
    public abstract class ExpressionNode
    {
        protected ExpressionNode(int position)
        {
            Position = position;
        }

        /// <summary>Position in the whole text, 0 based</summary>
        public int Position { get; }

        /// <summary>All variables and functions this node uses, for checking</summary>
        public abstract IEnumerable<ExpressionNode> Children { get; }
    }

    public sealed class LiteralNode : ExpressionNode
    {
        public LiteralNode(int position, object value) : base(position)
        {
            Value = value;
        }

        public object Value { get; }
        public override IEnumerable<ExpressionNode> Children => Array.Empty<ExpressionNode>();
    }

    public sealed class VariableNode : ExpressionNode
    {
        public VariableNode(int position, string name) : base(position)
        {
            Name = name;
        }

        /// <summary>Full dotted name, e.g. "payload.title"</summary>
        public string Name { get; }
        public override IEnumerable<ExpressionNode> Children => Array.Empty<ExpressionNode>();
    }

    public sealed class UnaryNode : ExpressionNode
    {
        public UnaryNode(int position, string op, ExpressionNode operand) : base(position)
        {
            Operator = op;
            Operand = operand;
        }

        public string Operator { get; }
        public ExpressionNode Operand { get; }
        public override IEnumerable<ExpressionNode> Children => new[] { Operand };
    }

    public sealed class BinaryNode : ExpressionNode
    {
        public BinaryNode(int position, string op, ExpressionNode left, ExpressionNode right) : base(position)
        {
            Operator = op;
            Left = left;
            Right = right;
        }

        public string Operator { get; }
        public ExpressionNode Left { get; }
        public ExpressionNode Right { get; }
        public override IEnumerable<ExpressionNode> Children => new[] { Left, Right };
    }

    public sealed class ConditionalNode : ExpressionNode
    {
        public ConditionalNode(int position, ExpressionNode condition, ExpressionNode whenTrue, ExpressionNode whenFalse) : base(position)
        {
            Condition = condition;
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }

        public ExpressionNode Condition { get; }
        public ExpressionNode WhenTrue { get; }
        public ExpressionNode WhenFalse { get; }
        public override IEnumerable<ExpressionNode> Children => new[] { Condition, WhenTrue, WhenFalse };
    }

    public sealed class CallNode : ExpressionNode
    {
        public CallNode(int position, string name, IReadOnlyList<ExpressionNode> arguments) : base(position)
        {
            Name = name;
            Arguments = arguments;
        }

        /// <summary>Function name, may be dotted: "left", "counter.scope", "jira.issueKey"</summary>
        public string Name { get; }
        public IReadOnlyList<ExpressionNode> Arguments { get; }
        public override IEnumerable<ExpressionNode> Children => Arguments;
    }

    /// <summary>
    /// One part of a template: fixed text, or a ${...} with an optional format.
    /// </summary>
    public sealed class TemplateSegment
    {
        public TemplateSegment(string text)
        {
            Text = text;
        }

        public TemplateSegment(ExpressionNode expression, string format, int position, string source)
        {
            Expression = expression;
            Format = format;
            Position = position;
            Text = source;
        }

        /// <summary>The fixed text, or for an expression its source "${...}"</summary>
        public string Text { get; }

        /// <summary>Null for fixed text</summary>
        public ExpressionNode Expression { get; }

        /// <summary>The .NET format after the first top-level ':', null when none</summary>
        public string Format { get; }

        public int Position { get; }

        public bool IsLiteral => Expression == null;
    }

    /// <summary>
    /// A parsed text with ${...}. Parse errors are kept, so a check can show all of them.
    /// </summary>
    public sealed class Template
    {
        internal Template(string source, IReadOnlyList<TemplateSegment> segments, IReadOnlyList<TemplateError> errors)
        {
            Source = source ?? string.Empty;
            Segments = segments;
            Errors = errors;
        }

        public string Source { get; }
        public IReadOnlyList<TemplateSegment> Segments { get; }

        /// <summary>Syntax errors; the checks of a purpose come from <see cref="IVariableResolver.Validate"/></summary>
        public IReadOnlyList<TemplateError> Errors { get; }

        /// <summary>No ${...} at all</summary>
        public bool IsPlainText
        {
            get
            {
                foreach (var segment in Segments)
                {
                    if (!segment.IsLiteral) return false;
                }
                return true;
            }
        }

        /// <summary>The whole text is one ${...} without format: its value keeps its type</summary>
        public bool IsSingleExpression => Segments.Count == 1 && !Segments[0].IsLiteral && Segments[0].Format == null;

        /// <summary>Every node of every expression, depth first</summary>
        public IEnumerable<ExpressionNode> AllNodes()
        {
            var stack = new Stack<ExpressionNode>();
            for (int i = Segments.Count - 1; i >= 0; i--)
            {
                if (Segments[i].Expression != null) stack.Push(Segments[i].Expression);
            }
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                yield return node;
                foreach (var child in node.Children)
                {
                    if (child != null) stack.Push(child);
                }
            }
        }
    }

    /// <summary>
    /// Parses texts like "${payload.time:yyyy-MM-dd} ${left(payload.title, 40)}".
    /// </summary>
    public static class TemplateParser
    {
        public static Template Parse(string text)
        {
            text ??= string.Empty;
            var segments = new List<TemplateSegment>();
            var errors = new List<TemplateError>();
            var literal = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '$' && i + 2 < text.Length + 1 && Peek(text, i + 1) == '$' && Peek(text, i + 2) == '{')
                {
                    // $${ is a literal ${
                    literal.Append("${");
                    i += 3;
                    continue;
                }
                if (text[i] == '$' && Peek(text, i + 1) == '{')
                {
                    int start = i;
                    int end = FindClosingBrace(text, i + 2);
                    if (end < 0)
                    {
                        errors.Add(new TemplateError(start, "'${' without its closing '}'"));
                        literal.Append(text, i, text.Length - i);
                        break;
                    }
                    if (literal.Length > 0)
                    {
                        segments.Add(new TemplateSegment(literal.ToString()));
                        literal.Clear();
                    }
                    string inner = text.Substring(i + 2, end - i - 2);
                    segments.Add(ParseSegment(inner, i + 2, text.Substring(start, end - start + 1), errors));
                    i = end + 1;
                    continue;
                }
                literal.Append(text[i]);
                i++;
            }
            if (literal.Length > 0)
            {
                segments.Add(new TemplateSegment(literal.ToString()));
            }
            return new Template(text, segments, errors);
        }

        private static char Peek(string text, int index) => index < text.Length ? text[index] : '\0';

        /// <summary>The '}' ending a ${...}: not inside quotes</summary>
        private static int FindClosingBrace(string text, int from)
        {
            char quote = '\0';
            for (int i = from; i < text.Length; i++)
            {
                char c = text[i];
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    continue;
                }
                if (c == '\'' || c == '"') quote = c;
                else if (c == '}') return i;
            }
            return -1;
        }

        private static TemplateSegment ParseSegment(string inner, int offset, string source, List<TemplateError> errors)
        {
            // The first ':' outside parentheses and quotes starts the format (like C# interpolation)
            string expressionText = inner;
            string format = null;
            int colon = FindTopLevelColon(inner);
            if (colon >= 0)
            {
                expressionText = inner.Substring(0, colon);
                format = Unquote(inner.Substring(colon + 1));
            }
            if (string.IsNullOrWhiteSpace(expressionText))
            {
                errors.Add(new TemplateError(offset - 2, "'${}' is empty"));
                return new TemplateSegment(new LiteralNode(offset, string.Empty), format, offset - 2, source);
            }
            var parser = new ExpressionParser(expressionText, offset, errors, colon >= 0);
            var node = parser.ParseAll();
            return new TemplateSegment(node, format, offset - 2, source);
        }

        private static int FindTopLevelColon(string text)
        {
            int depth = 0;
            char quote = '\0';
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    continue;
                }
                switch (c)
                {
                    case '\'':
                    case '"':
                        quote = c;
                        break;
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth--;
                        break;
                    case ':' when depth == 0:
                        return i;
                }
            }
            return -1;
        }

        private static string Unquote(string format)
        {
            string trimmed = format.Trim();
            if (trimmed.Length >= 2 && (trimmed[0] == '\'' || trimmed[0] == '"') && trimmed[trimmed.Length - 1] == trimmed[0])
            {
                return trimmed.Substring(1, trimmed.Length - 2);
            }
            // Spaces in a format are meaningful ("yyyy-MM-dd HH_mm"), only the ones around it go
            return trimmed;
        }
    }

    /// <summary>
    /// Recursive descent parser of one expression. Precedence, lowest first:
    /// ?: , ??, ||, &amp;&amp;, == !=, &lt; &lt;= &gt; &gt;=, + -, * / %, unary ! -
    /// </summary>
    internal sealed class ExpressionParser
    {
        private readonly string _text;
        private readonly int _offset;
        private readonly List<TemplateError> _errors;
        private readonly bool _hadFormat;
        private int _pos;

        public ExpressionParser(string text, int offset, List<TemplateError> errors, bool hadFormat)
        {
            _text = text;
            _offset = offset;
            _errors = errors;
            _hadFormat = hadFormat;
        }

        private int Here => _offset + _pos;

        public ExpressionNode ParseAll()
        {
            var node = ParseConditional();
            SkipSpaces();
            if (_pos < _text.Length)
            {
                Error($"Unexpected '{_text.Substring(_pos)}'");
            }
            return node;
        }

        private void Error(string message)
        {
            // One message per expression is enough, the next ones are mostly consequences
            if (_errors.Count == 0 || _errors[_errors.Count - 1].Position < _offset)
            {
                _errors.Add(new TemplateError(Here, message));
            }
        }

        private void SkipSpaces()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) _pos++;
        }

        private bool Take(string token)
        {
            SkipSpaces();
            if (string.CompareOrdinal(_text, _pos, token, 0, token.Length) != 0) return false;
            // "|" isn't "||", "?" isn't "??", "=" alone isn't an operator
            if (token == "?" && Peek(1) == '?') return false;
            if ((token == "<" || token == ">" || token == "!") && Peek(1) == '=') return false;
            _pos += token.Length;
            return true;
        }

        private char Peek(int ahead = 0) => _pos + ahead < _text.Length ? _text[_pos + ahead] : '\0';

        private ExpressionNode ParseConditional()
        {
            var condition = ParseCoalesce();
            int position = Here;
            if (Take("?"))
            {
                var whenTrue = ParseConditional();
                if (!Take(":"))
                {
                    Error(_hadFormat
                        ? "A choice (? :) needs parentheses: ${(a ? b : c)}, the first ':' outside them starts the format"
                        : "'?' without its ':'");
                    return new ConditionalNode(position, condition, whenTrue, new LiteralNode(Here, null));
                }
                var whenFalse = ParseConditional();
                return new ConditionalNode(position, condition, whenTrue, whenFalse);
            }
            return condition;
        }

        private ExpressionNode ParseCoalesce()
        {
            var left = ParseOr();
            while (true)
            {
                int position = Here;
                if (!Take("??")) return left;
                left = new BinaryNode(position, "??", left, ParseOr());
            }
        }

        private ExpressionNode ParseOr()
        {
            var left = ParseAnd();
            while (true)
            {
                int position = Here;
                if (!Take("||")) return left;
                left = new BinaryNode(position, "||", left, ParseAnd());
            }
        }

        private ExpressionNode ParseAnd()
        {
            var left = ParseEquality();
            while (true)
            {
                int position = Here;
                if (!Take("&&")) return left;
                left = new BinaryNode(position, "&&", left, ParseEquality());
            }
        }

        private ExpressionNode ParseEquality()
        {
            var left = ParseRelational();
            while (true)
            {
                int position = Here;
                string op = Take("==") ? "==" : Take("!=") ? "!=" : null;
                if (op == null) return left;
                left = new BinaryNode(position, op, left, ParseRelational());
            }
        }

        private ExpressionNode ParseRelational()
        {
            var left = ParseAdditive();
            while (true)
            {
                int position = Here;
                string op = Take("<=") ? "<=" : Take(">=") ? ">=" : Take("<") ? "<" : Take(">") ? ">" : null;
                if (op == null) return left;
                left = new BinaryNode(position, op, left, ParseAdditive());
            }
        }

        private ExpressionNode ParseAdditive()
        {
            var left = ParseMultiplicative();
            while (true)
            {
                int position = Here;
                string op = Take("+") ? "+" : Take("-") ? "-" : null;
                if (op == null) return left;
                left = new BinaryNode(position, op, left, ParseMultiplicative());
            }
        }

        private ExpressionNode ParseMultiplicative()
        {
            var left = ParseUnary();
            while (true)
            {
                int position = Here;
                string op = Take("*") ? "*" : Take("/") ? "/" : Take("%") ? "%" : null;
                if (op == null) return left;
                left = new BinaryNode(position, op, left, ParseUnary());
            }
        }

        private ExpressionNode ParseUnary()
        {
            int position = Here;
            if (Take("!")) return new UnaryNode(position, "!", ParseUnary());
            if (Take("-")) return new UnaryNode(position, "-", ParseUnary());
            return ParsePrimary();
        }

        private ExpressionNode ParsePrimary()
        {
            SkipSpaces();
            int position = Here;
            char c = Peek();
            if (c == '\0')
            {
                Error("A value is missing");
                return new LiteralNode(position, null);
            }
            if (c == '(')
            {
                _pos++;
                var inner = ParseConditional();
                if (!Take(")")) Error("')' is missing");
                return inner;
            }
            if (c == '\'' || c == '"')
            {
                int end = _text.IndexOf(c, _pos + 1);
                if (end < 0)
                {
                    Error("A text without its closing quote");
                    _pos = _text.Length;
                    return new LiteralNode(position, string.Empty);
                }
                string value = _text.Substring(_pos + 1, end - _pos - 1);
                _pos = end + 1;
                return new LiteralNode(position, value);
            }
            if (char.IsDigit(c) || (c == '.' && char.IsDigit(Peek(1))))
            {
                int start = _pos;
                while (char.IsDigit(Peek()) || Peek() == '.') _pos++;
                string number = _text.Substring(start, _pos - start);
                if (number.IndexOf('.') < 0 && long.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out long whole))
                {
                    return new LiteralNode(position, whole);
                }
                if (double.TryParse(number, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double value))
                {
                    return new LiteralNode(position, value);
                }
                Error($"'{number}' isn't a number");
                return new LiteralNode(position, null);
            }
            if (char.IsLetter(c) || c == '_')
            {
                string name = ReadName();
                switch (name.ToLowerInvariant())
                {
                    case "true": return new LiteralNode(position, true);
                    case "false": return new LiteralNode(position, false);
                    case "null": return new LiteralNode(position, null);
                }
                SkipSpaces();
                if (Peek() == '(')
                {
                    _pos++;
                    var arguments = new List<ExpressionNode>();
                    if (!Take(")"))
                    {
                        do
                        {
                            arguments.Add(ParseConditional());
                        } while (Take(","));
                        if (!Take(")")) Error($"')' of {name}(...) is missing");
                    }
                    return new CallNode(position, name, arguments);
                }
                return new VariableNode(position, name);
            }
            Error($"Unexpected '{c}'");
            _pos++;
            return new LiteralNode(position, null);
        }

        private string ReadName()
        {
            int start = _pos;
            while (_pos < _text.Length)
            {
                char c = _text[_pos];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    _pos++;
                }
                else if (c == '.' && _pos + 1 < _text.Length && (char.IsLetterOrDigit(_text[_pos + 1]) || _text[_pos + 1] == '_'))
                {
                    _pos++;
                }
                else
                {
                    break;
                }
            }
            return _text.Substring(start, _pos - start);
        }
    }
}
