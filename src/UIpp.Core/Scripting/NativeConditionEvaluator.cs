using System.Globalization;

namespace UIpp.Core.Scripting;

// Recursive-descent parser for UI++ condition expressions (post-variable-substitution).
//
// Grammar:
//   expr     → or_expr
//   or_expr  → and_expr  ( 'OR'  and_expr )*
//   and_expr → not_expr  ( 'AND' not_expr )*
//   not_expr → 'NOT' not_expr | compare
//   compare  → atom ( op atom )?
//   op       → '=' | '<>' | '<' | '>' | '<=' | '>='
//   atom     → number | string_lit | call | '(' expr ')'
//   call     → IDENT '(' arg_list ')'
//   string_lit → '"' ... '"' | "'" ... "'"
public sealed class NativeConditionEvaluator : IConditionEvaluator
{
    public bool Evaluate(string expression, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;
        var parser = new Parser(expression);
        var result = parser.ParseExpr();
        return result.IsTrue;
    }

    // -------------------------------------------------------------------------
    // Value types produced by the parser
    // -------------------------------------------------------------------------

    private enum ValueKind { String, Number, Bool }

    private readonly struct Value
    {
        public readonly ValueKind Kind;
        public readonly string    Str;
        public readonly double    Num;
        public readonly bool      Bool;

        private Value(ValueKind k, string s, double n, bool b) { Kind=k; Str=s; Num=n; Bool=b; }

        public static Value FromString(string s) => new(ValueKind.String, s, 0, false);
        public static Value FromNumber(double n) => new(ValueKind.Number, n.ToString(CultureInfo.InvariantCulture), n, false);
        public static Value FromBool(bool b)     => new(ValueKind.Bool,   b ? "True" : "False", b ? 1 : 0, b);

        // Truthy: non-empty string, non-zero number, or bool true
        public bool IsTrue => Kind switch
        {
            ValueKind.Bool   => Bool,
            ValueKind.Number => Num != 0,
            _                => !string.IsNullOrEmpty(Str),
        };

        // Coerce to double if possible
        public bool TryGetDouble(out double v)
        {
            if (Kind == ValueKind.Number) { v = Num; return true; }
            return double.TryParse(Str, NumberStyles.Any, CultureInfo.InvariantCulture, out v);
        }

        // String representation for comparison
        public string AsString() => Kind == ValueKind.Number
            ? Num.ToString(CultureInfo.InvariantCulture)
            : Str;
    }

    // -------------------------------------------------------------------------
    // Lexer
    // -------------------------------------------------------------------------

    private enum TokenKind
    {
        Eof, Ident, Number, StringLit,
        LParen, RParen, Comma,
        Eq, Ne, Lt, Gt, Le, Ge,
    }

    private sealed class Lexer(string src)
    {
        private int _pos;

        public TokenKind Kind  { get; private set; }
        public string    Text  { get; private set; } = string.Empty;
        public double    Num   { get; private set; }

        public void Advance()
        {
            while (_pos < src.Length && char.IsWhiteSpace(src[_pos])) _pos++;

            if (_pos >= src.Length) { Kind = TokenKind.Eof; Text = ""; return; }

            char c = src[_pos];

            if (c == '"' || c == '\'')
            {
                var q = c; _pos++;
                var start = _pos;
                while (_pos < src.Length && src[_pos] != q) _pos++;
                Text = src[start.._pos];
                if (_pos < src.Length) _pos++;
                Kind = TokenKind.StringLit;
                return;
            }

            if (char.IsDigit(c) || (c == '-' && _pos + 1 < src.Length && char.IsDigit(src[_pos + 1])))
            {
                var start = _pos;
                if (c == '-') _pos++;
                while (_pos < src.Length && (char.IsDigit(src[_pos]) || src[_pos] == '.')) _pos++;
                Text = src[start.._pos];
                Num  = double.Parse(Text, CultureInfo.InvariantCulture);
                Kind = TokenKind.Number;
                return;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = _pos;
                while (_pos < src.Length && (char.IsLetterOrDigit(src[_pos]) || src[_pos] == '_')) _pos++;
                Text = src[start.._pos];
                Kind = TokenKind.Ident;
                return;
            }

            switch (c)
            {
                case '(': _pos++; Kind = TokenKind.LParen; Text = "("; return;
                case ')': _pos++; Kind = TokenKind.RParen; Text = ")"; return;
                case ',': _pos++; Kind = TokenKind.Comma;  Text = ","; return;
                case '=': _pos++; Kind = TokenKind.Eq; Text = "="; return;
                case '<':
                    _pos++;
                    if (_pos < src.Length && src[_pos] == '>') { _pos++; Kind = TokenKind.Ne; Text = "<>"; }
                    else if (_pos < src.Length && src[_pos] == '=') { _pos++; Kind = TokenKind.Le; Text = "<="; }
                    else { Kind = TokenKind.Lt; Text = "<"; }
                    return;
                case '>':
                    _pos++;
                    if (_pos < src.Length && src[_pos] == '=') { _pos++; Kind = TokenKind.Ge; Text = ">="; }
                    else { Kind = TokenKind.Gt; Text = ">"; }
                    return;
                default:
                    _pos++;
                    Kind = TokenKind.Ident;
                    Text = c.ToString();
                    return;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Parser
    // -------------------------------------------------------------------------

    private sealed class Parser
    {
        private readonly Lexer _lex;

        public Parser(string src)
        {
            _lex = new Lexer(src);
            _lex.Advance();
        }

        public Value ParseExpr() => ParseOr();

        private Value ParseOr()
        {
            var left = ParseAnd();
            while (_lex.Kind == TokenKind.Ident &&
                   string.Equals(_lex.Text, "OR", StringComparison.OrdinalIgnoreCase))
            {
                _lex.Advance();
                var right = ParseAnd();
                left = Value.FromBool(left.IsTrue || right.IsTrue);
            }
            return left;
        }

        private Value ParseAnd()
        {
            var left = ParseNot();
            while (_lex.Kind == TokenKind.Ident &&
                   string.Equals(_lex.Text, "AND", StringComparison.OrdinalIgnoreCase))
            {
                _lex.Advance();
                var right = ParseNot();
                left = Value.FromBool(left.IsTrue && right.IsTrue);
            }
            return left;
        }

        private Value ParseNot()
        {
            if (_lex.Kind == TokenKind.Ident &&
                string.Equals(_lex.Text, "NOT", StringComparison.OrdinalIgnoreCase))
            {
                _lex.Advance();
                return Value.FromBool(!ParseNot().IsTrue);
            }
            return ParseCompare();
        }

        private Value ParseCompare()
        {
            var left = ParseAtom();
            var op   = _lex.Kind;
            if (op is not (TokenKind.Eq or TokenKind.Ne or TokenKind.Lt
                          or TokenKind.Gt or TokenKind.Le or TokenKind.Ge))
                return left;

            _lex.Advance();
            var right = ParseAtom();

            // Prefer numeric comparison when both sides parse as numbers
            if (left.TryGetDouble(out var ln) && right.TryGetDouble(out var rn))
            {
                return Value.FromBool(op switch
                {
                    TokenKind.Eq => ln == rn,
                    TokenKind.Ne => ln != rn,
                    TokenKind.Lt => ln < rn,
                    TokenKind.Gt => ln > rn,
                    TokenKind.Le => ln <= rn,
                    TokenKind.Ge => ln >= rn,
                    _            => false,
                });
            }

            var cmp = string.Compare(left.AsString(), right.AsString(),
                StringComparison.OrdinalIgnoreCase);
            return Value.FromBool(op switch
            {
                TokenKind.Eq => cmp == 0,
                TokenKind.Ne => cmp != 0,
                TokenKind.Lt => cmp < 0,
                TokenKind.Gt => cmp > 0,
                TokenKind.Le => cmp <= 0,
                TokenKind.Ge => cmp >= 0,
                _            => false,
            });
        }

        private Value ParseAtom()
        {
            switch (_lex.Kind)
            {
                case TokenKind.Number:
                {
                    var v = Value.FromNumber(_lex.Num);
                    _lex.Advance();
                    return v;
                }
                case TokenKind.StringLit:
                {
                    var v = Value.FromString(_lex.Text);
                    _lex.Advance();
                    return v;
                }
                case TokenKind.LParen:
                {
                    _lex.Advance();
                    var v = ParseExpr();
                    if (_lex.Kind == TokenKind.RParen) _lex.Advance();
                    return v;
                }
                case TokenKind.Ident:
                {
                    var name = _lex.Text;
                    _lex.Advance();
                    if (_lex.Kind == TokenKind.LParen)
                        return ParseCall(name);
                    // Bare identifier — treat as string (already substituted by caller)
                    return Value.FromString(name);
                }
                default:
                    return Value.FromString(string.Empty);
            }
        }

        private Value ParseCall(string name)
        {
            _lex.Advance(); // consume '('
            var args = new List<Value>();
            while (_lex.Kind != TokenKind.RParen && _lex.Kind != TokenKind.Eof)
            {
                args.Add(ParseExpr());
                if (_lex.Kind == TokenKind.Comma) _lex.Advance();
            }
            if (_lex.Kind == TokenKind.RParen) _lex.Advance();

            return DispatchBuiltin(name, args);
        }

        // ----------------------------------------------------------------
        // VBScript built-ins replicated in C#
        // ----------------------------------------------------------------

        private static Value DispatchBuiltin(string name, List<Value> args)
        {
            return name.ToUpperInvariant() switch
            {
                "INSTR"      => Builtin_InStr(args),
                "INSTRREV"   => Builtin_InStrRev(args),
                "UCASE"      => Builtin_UCase(args),
                "LCASE"      => Builtin_LCase(args),
                "LEN"        => Builtin_Len(args),
                "MID"        => Builtin_Mid(args),
                "LEFT"       => Builtin_Left(args),
                "RIGHT"      => Builtin_Right(args),
                "TRIM"       => Builtin_Trim(args),
                "LTRIM"      => args.Count > 0 ? Value.FromString(args[0].AsString().TrimStart()) : Value.FromString(""),
                "RTRIM"      => args.Count > 0 ? Value.FromString(args[0].AsString().TrimEnd())   : Value.FromString(""),
                "ISNUMERIC"  => Builtin_IsNumeric(args),
                "ISNULL"     => Value.FromBool(false),   // no nulls in this evaluator
                "ISEMPTY"    => args.Count > 0 ? Value.FromBool(string.IsNullOrEmpty(args[0].AsString())) : Value.FromBool(true),
                "STR"        => args.Count > 0 && args[0].TryGetDouble(out var d) ? Value.FromString(d.ToString(CultureInfo.InvariantCulture)) : Value.FromString(""),
                "INT"        => args.Count > 0 && args[0].TryGetDouble(out var n) ? Value.FromNumber(Math.Floor(n)) : Value.FromNumber(0),
                "ABS"        => args.Count > 0 && args[0].TryGetDouble(out var a) ? Value.FromNumber(Math.Abs(a))  : Value.FromNumber(0),
                "CBOOL"      => args.Count > 0 ? Value.FromBool(args[0].IsTrue) : Value.FromBool(false),
                "CINT"       => args.Count > 0 && args[0].TryGetDouble(out var ci) ? Value.FromNumber(Math.Round(ci)) : Value.FromNumber(0),
                "CDBL"       => args.Count > 0 && args[0].TryGetDouble(out var cd) ? Value.FromNumber(cd) : Value.FromNumber(0),
                "REPLACE"    => Builtin_Replace(args),
                "SPLIT"      => Value.FromString(""),   // unsupported in simple evaluator
                _            => Value.FromString(""),
            };
        }

        // InStr([start,] string1, string2 [, compare])
        // Returns 1-based position, or 0 if not found.
        private static Value Builtin_InStr(List<Value> args)
        {
            if (args.Count < 2) return Value.FromNumber(0);

            int startIdx = 0;
            string haystack, needle;

            if (args.Count >= 3 && args[0].TryGetDouble(out var startVal))
            {
                startIdx = Math.Max(0, (int)startVal - 1);
                haystack = args[1].AsString();
                needle   = args[2].AsString();
            }
            else
            {
                haystack = args[0].AsString();
                needle   = args[1].AsString();
            }

            if (needle.Length == 0) return Value.FromNumber(startIdx + 1);
            if (startIdx >= haystack.Length) return Value.FromNumber(0);

            var idx = haystack.IndexOf(needle, startIdx, StringComparison.OrdinalIgnoreCase);
            return Value.FromNumber(idx < 0 ? 0 : idx + 1);
        }

        private static Value Builtin_InStrRev(List<Value> args)
        {
            if (args.Count < 2) return Value.FromNumber(0);
            var haystack = args[0].AsString();
            var needle   = args[1].AsString();
            if (needle.Length == 0) return Value.FromNumber(haystack.Length);
            var idx = haystack.LastIndexOf(needle, StringComparison.OrdinalIgnoreCase);
            return Value.FromNumber(idx < 0 ? 0 : idx + 1);
        }

        private static Value Builtin_UCase(List<Value> args) =>
            args.Count > 0 ? Value.FromString(args[0].AsString().ToUpperInvariant()) : Value.FromString("");

        private static Value Builtin_LCase(List<Value> args) =>
            args.Count > 0 ? Value.FromString(args[0].AsString().ToLowerInvariant()) : Value.FromString("");

        private static Value Builtin_Len(List<Value> args) =>
            args.Count > 0 ? Value.FromNumber(args[0].AsString().Length) : Value.FromNumber(0);

        private static Value Builtin_Mid(List<Value> args)
        {
            if (args.Count < 2) return Value.FromString("");
            var s = args[0].AsString();
            args[1].TryGetDouble(out var startD);
            var start = Math.Max(1, (int)startD) - 1;
            if (start >= s.Length) return Value.FromString("");
            if (args.Count >= 3 && args[2].TryGetDouble(out var lenD))
            {
                var len = Math.Min((int)lenD, s.Length - start);
                return Value.FromString(len <= 0 ? "" : s.Substring(start, len));
            }
            return Value.FromString(s[start..]);
        }

        private static Value Builtin_Left(List<Value> args)
        {
            if (args.Count < 2) return Value.FromString("");
            var s = args[0].AsString();
            args[1].TryGetDouble(out var nD);
            var n = Math.Min(Math.Max(0, (int)nD), s.Length);
            return Value.FromString(s[..n]);
        }

        private static Value Builtin_Right(List<Value> args)
        {
            if (args.Count < 2) return Value.FromString("");
            var s = args[0].AsString();
            args[1].TryGetDouble(out var nD);
            var n = Math.Min(Math.Max(0, (int)nD), s.Length);
            return Value.FromString(s[^n..]);
        }

        private static Value Builtin_Trim(List<Value> args) =>
            args.Count > 0 ? Value.FromString(args[0].AsString().Trim()) : Value.FromString("");

        private static Value Builtin_IsNumeric(List<Value> args)
        {
            if (args.Count == 0) return Value.FromBool(false);
            return Value.FromBool(double.TryParse(args[0].AsString(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out _));
        }

        private static Value Builtin_Replace(List<Value> args)
        {
            if (args.Count < 3) return args.Count > 0 ? Value.FromString(args[0].AsString()) : Value.FromString("");
            var s    = args[0].AsString();
            var find = args[1].AsString();
            var repl = args[2].AsString();
            if (find.Length == 0) return Value.FromString(s);
            return Value.FromString(s.Replace(find, repl, StringComparison.OrdinalIgnoreCase));
        }
    }
}
