using System.Globalization;

namespace UiSharp.Core.Scripting;

// Recursive-descent parser for UI++ expressions (post-variable-substitution).
//
// Used two ways, exactly as the original uses its one CScriptHost:
//   * TryEvaluate    — a condition, for its truth value.
//   * TryEvaluateValue — an expression, for its resulting value (TSVar, Switch).
//
// Precedence follows VBScript, loosest to tightest:
//   expr      → or_expr
//   or_expr   → and_expr ( 'OR' and_expr )*
//   and_expr  → not_expr ( 'AND' not_expr )*
//   not_expr  → 'NOT' not_expr | compare
//   compare   → concat ( ( '=' | '<>' | '<' | '>' | '<=' | '>=' ) concat )?
//   concat    → additive ( '&' additive )*
//   additive  → modulo ( ( '+' | '-' ) modulo )*
//   modulo    → intdiv ( 'MOD' intdiv )*
//   intdiv    → muldiv ( '\' muldiv )*
//   muldiv    → unary ( ( '*' | '/' ) unary )*
//   unary     → ( '-' | '+' ) unary | power
//   power     → postfix ( '^' unary )?
//   postfix   → atom ( '.' IDENT [ '(' arg_list ')' ] )*
//   atom      → number | string_lit | call | '(' expr ')' | IDENT
//   call      → IDENT '(' arg_list ')'
//   string_lit → '"' ... '"' | "'" ... "'"
public sealed class NativeConditionEvaluator : IConditionEvaluator
{
    private readonly IScriptHostServices _services;

    public NativeConditionEvaluator() : this(DefaultScriptHostServices.Instance) { }

    /// <summary>
    /// Supplies the machine lookups behind the CreateObject equivalents. Tests
    /// inject a fixed implementation so results do not depend on the host.
    /// </summary>
    public NativeConditionEvaluator(IScriptHostServices services) => _services = services;

    public bool Evaluate(string expression, IReadOnlyDictionary<string, string> variables) =>
        TryEvaluate(expression, variables).Value;

    /// <summary>
    /// Evaluates an expression for its value. Declines (returns false) whenever
    /// the engine could not fully evaluate it or the result is empty, so the
    /// caller keeps the literal text — the same rule the original applies to
    /// CScriptHost::Eval's HRESULT and VARIANT.
    /// </summary>
    public bool TryEvaluateValue(string expression, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(expression)) return false;

        var diagnostics = new List<ConditionDiagnostic>();
        var parser = new Parser(expression, diagnostics, _services);
        var result = parser.ParseExpr();

        // Blocking diagnostics, or input the grammar could not consume, mean
        // VBScript would most likely have raised an error here. Decline rather
        // than hand back a half-evaluated value. Advisory diagnostics are fine.
        if (!parser.AtEnd || diagnostics.Any(d => d.IsBlocking)) return false;

        // An object reference is not a value. VBScript would hand back an
        // IDispatch that has no sensible string form, and nothing useful can be
        // stored in a task-sequence variable, so decline and keep the literal.
        if (result.Kind == ValueKind.Object) return false;

        var text = result.AsString();
        if (text.Length == 0) return false;   // C++ requires a non-empty result

        value = text;
        return true;
    }

    public ConditionResult TryEvaluate(string expression, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(expression)) return ConditionResult.Ok(true);

        var diagnostics = new List<ConditionDiagnostic>();
        var parser = new Parser(expression, diagnostics, _services);
        var result = parser.ParseExpr();

        // Anything left over means the grammar bailed out part-way and the rest of
        // the expression was ignored. When the token we stopped on is punctuation
        // the grammar has no rule for, that operator is the real story — say so
        // rather than blaming trailing junk.
        if (!parser.AtEnd)
        {
            diagnostics.Add(parser.StoppedOnUnsupportedToken
                ? new ConditionDiagnostic(
                    ConditionDiagnosticKind.UnsupportedConstruct,
                    DescribeUnsupportedToken(parser.StoppedTokenText, expression))
                : new ConditionDiagnostic(
                    ConditionDiagnosticKind.TrailingInput,
                    $"stopped at '{parser.RemainingText}' in \"{expression}\""));
        }

        // Fail closed on BLOCKING diagnostics only. Anything the engine could not
        // evaluate faithfully would have raised a VBScript error in the original,
        // and C++ EvalCondition treats a failed Eval as false
        // (ActionHelper.cpp:89). Advisory diagnostics — migration guidance from
        // the COM compatibility shim — must not change the answer, or every
        // config using CreateObject would suddenly evaluate false.
        var blocked = diagnostics.Any(d => d.IsBlocking);

        return new ConditionResult(!blocked && result.IsTrue, diagnostics);
    }

    private static string DescribeUnsupportedToken(string token, string expression) => token switch
    {
        "." => $"member access ('.') is not supported by the native engine, in \"{expression}\"",
        "&" => $"string concatenation ('&') is not supported by the native engine, in \"{expression}\"",
        _   => $"unsupported operator or character '{token}' in \"{expression}\"",
    };

    // -------------------------------------------------------------------------
    // Value types produced by the parser
    // -------------------------------------------------------------------------

    private enum ValueKind { String, Number, Bool, Object }

    private readonly struct Value
    {
        public readonly ValueKind Kind;   // read by ParseAdditive to pick + semantics
        public readonly string    Str;
        public readonly double    Num;
        public readonly bool      Bool;
        public readonly object?   Obj;    // ScriptObject when Kind is Object

        private Value(ValueKind k, string s, double n, bool b, object? o = null)
        { Kind=k; Str=s; Num=n; Bool=b; Obj=o; }

        public static Value FromString(string s) => new(ValueKind.String, s, 0, false);
        public static Value FromNumber(double n) => new(ValueKind.Number, n.ToString(CultureInfo.InvariantCulture), n, false);
        public static Value FromBool(bool b)     => new(ValueKind.Bool,   b ? "True" : "False", b ? 1 : 0, b);
        public static Value FromObject(ScriptObject o) => new(ValueKind.Object, o.ProgId, 0, false, o);

        // Truthy: non-empty string, non-zero number, or bool true
        public bool IsTrue => Kind switch
        {
            ValueKind.Bool   => Bool,
            ValueKind.Number => Num != 0,
            ValueKind.Object => true,
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
        Plus, Minus, Star, Slash, Backslash, Caret, Amp, Dot,
        // Punctuation the grammar has no rule for, such as '.' member access.
        // Kept as its own kind so the parser can report it instead of silently
        // treating it as a bare identifier.
        Unknown,
    }

    private sealed class Lexer(string src)
    {
        private int _pos;

        public TokenKind Kind  { get; private set; }
        public string    Text  { get; private set; } = string.Empty;
        public double    Num   { get; private set; }

        // Current token plus everything after it — used to describe where the
        // parser gave up.
        public string Remaining => Kind == TokenKind.Eof
            ? string.Empty
            : (Text + src[Math.Min(_pos, src.Length)..]).Trim();

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

            if (char.IsDigit(c))
            {
                var start = _pos;
                while (_pos < src.Length && (char.IsDigit(src[_pos]) || src[_pos] == '.')) _pos++;
                Text = src[start.._pos];

                // A run of digits and dots is not necessarily a number: version
                // strings like "4.5.2" appear in real config text. double.Parse
                // threw an unhandled FormatException on those, which during a
                // deployment meant UiSharp died rather than reporting a bad
                // condition. Hand it to the parser as an unrecognised token
                // instead, which VBScript also treats as a syntax error.
                if (double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    Num  = parsed;
                    Kind = TokenKind.Number;
                }
                else
                {
                    Kind = TokenKind.Unknown;
                }
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
                case '+':  _pos++; Kind = TokenKind.Plus;      Text = "+";  return;
                case '-':  _pos++; Kind = TokenKind.Minus;     Text = "-";  return;
                case '*':  _pos++; Kind = TokenKind.Star;      Text = "*";  return;
                case '/':  _pos++; Kind = TokenKind.Slash;     Text = "/";  return;
                case '\\': _pos++; Kind = TokenKind.Backslash; Text = "\\"; return;
                case '^':  _pos++; Kind = TokenKind.Caret;     Text = "^";  return;
                case '&':  _pos++; Kind = TokenKind.Amp;       Text = "&";  return;
                case '.':  _pos++; Kind = TokenKind.Dot;       Text = ".";  return;
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
                    Kind = TokenKind.Unknown;
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
        private readonly List<ConditionDiagnostic> _diagnostics;
        private readonly string _src;
        private readonly IScriptHostServices _services;

        public Parser(string src, List<ConditionDiagnostic> diagnostics, IScriptHostServices services)
        {
            _lex = new Lexer(src);
            _src = src;
            _diagnostics = diagnostics;
            _services = services;
            _lex.Advance();
        }

        public bool   AtEnd         => _lex.Kind == TokenKind.Eof;
        public string RemainingText => _lex.Remaining;

        // True when parsing halted on punctuation the grammar has no rule for,
        // rather than on otherwise-valid but unexpected input.
        public bool   StoppedOnUnsupportedToken => _lex.Kind == TokenKind.Unknown;
        public string StoppedTokenText          => _lex.Text;

        private void Report(ConditionDiagnosticKind kind, string detail) =>
            _diagnostics.Add(new ConditionDiagnostic(kind, detail));

        private static string Str(List<Value> args, int index) =>
            index < args.Count ? args[index].AsString() : string.Empty;

        private void Advise(ConditionDiagnosticKind kind, string detail) =>
            _diagnostics.Add(new ConditionDiagnostic(
                kind, detail, ConditionDiagnosticSeverity.Advisory));

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
            var left = ParseConcat();
            var op   = _lex.Kind;
            if (op is not (TokenKind.Eq or TokenKind.Ne or TokenKind.Lt
                          or TokenKind.Gt or TokenKind.Le or TokenKind.Ge))
                return left;

            _lex.Advance();
            var right = ParseConcat();

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

            // VBScript compares strings with Option Compare Binary unless a
            // script says otherwise, and the UI++ host never does — so this is
            // case-SENSITIVE. Confirmed by differential test against vbscript.dll.
            var cmp = string.Compare(left.AsString(), right.AsString(),
                StringComparison.Ordinal);
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

        // '&' always concatenates, coercing both sides to text (VBScript: 1 & 2 = "12").
        private Value ParseConcat()
        {
            var left = ParseAdditive();
            while (_lex.Kind == TokenKind.Amp)
            {
                _lex.Advance();
                var right = ParseAdditive();
                left = Value.FromString(left.AsString() + right.AsString());
            }
            return left;
        }

        // '+' adds numbers but concatenates when both sides are strings, which is
        // what VBScript does: "1" + "2" is "12" while 1 + 2 is 3.
        private Value ParseAdditive()
        {
            var left = ParseModulo();
            while (_lex.Kind is TokenKind.Plus or TokenKind.Minus)
            {
                var op = _lex.Kind;
                _lex.Advance();
                var right = ParseModulo();

                if (op == TokenKind.Plus &&
                    left.Kind == ValueKind.String && right.Kind == ValueKind.String)
                {
                    left = Value.FromString(left.AsString() + right.AsString());
                    continue;
                }

                if (!TryNumbers(left, right, op == TokenKind.Plus ? "+" : "-", out var ln, out var rn))
                    return Value.FromString(string.Empty);

                left = Value.FromNumber(op == TokenKind.Plus ? ln + rn : ln - rn);
            }
            return left;
        }

        // VBScript Mod operator: integer modulo.
        private Value ParseModulo()
        {
            var left = ParseIntDiv();
            while (_lex.Kind == TokenKind.Ident &&
                   string.Equals(_lex.Text, "Mod", StringComparison.OrdinalIgnoreCase))
            {
                _lex.Advance();
                var right = ParseIntDiv();

                if (!TryNumbers(left, right, "Mod", out var ln, out var rn))
                    return Value.FromString(string.Empty);

                if (Math.Truncate(rn) == 0)
                {
                    Report(ConditionDiagnosticKind.EvaluationError, "division by zero in 'Mod'");
                    return Value.FromString(string.Empty);
                }

                left = Value.FromNumber(Math.Truncate(ln) % Math.Truncate(rn));
            }
            return left;
        }

        // Backslash is VBScript's integer division.
        private Value ParseIntDiv()
        {
            var left = ParseMulDiv();
            while (_lex.Kind == TokenKind.Backslash)
            {
                _lex.Advance();
                var right = ParseMulDiv();

                if (!TryNumbers(left, right, "\\", out var ln, out var rn))
                    return Value.FromString(string.Empty);

                if (Math.Truncate(rn) == 0)
                {
                    Report(ConditionDiagnosticKind.EvaluationError, "division by zero in '\\'");
                    return Value.FromString(string.Empty);
                }

                left = Value.FromNumber(Math.Truncate(Math.Truncate(ln) / Math.Truncate(rn)));
            }
            return left;
        }

        private Value ParseMulDiv()
        {
            var left = ParseUnary();
            while (_lex.Kind is TokenKind.Star or TokenKind.Slash)
            {
                var op = _lex.Kind;
                _lex.Advance();
                var right = ParseUnary();

                if (!TryNumbers(left, right, op == TokenKind.Star ? "*" : "/", out var ln, out var rn))
                    return Value.FromString(string.Empty);

                if (op == TokenKind.Slash && rn == 0)
                {
                    Report(ConditionDiagnosticKind.EvaluationError, "division by zero in '/'");
                    return Value.FromString(string.Empty);
                }

                left = Value.FromNumber(op == TokenKind.Star ? ln * rn : ln / rn);
            }
            return left;
        }

        private Value ParseUnary()
        {
            if (_lex.Kind is TokenKind.Minus or TokenKind.Plus)
            {
                var negate = _lex.Kind == TokenKind.Minus;
                _lex.Advance();
                var operand = ParseUnary();

                if (!operand.TryGetDouble(out var n))
                {
                    Report(ConditionDiagnosticKind.EvaluationError,
                        $"unary '{(negate ? "-" : "+")}' applied to a non-numeric value");
                    return Value.FromString(string.Empty);
                }

                return Value.FromNumber(negate ? -n : n);
            }
            return ParsePower();
        }

        // '^' is right-associative in VBScript, so the exponent recurses through
        // ParseUnary to allow 2 ^ -1.
        private Value ParsePower()
        {
            var left = ParsePostfix();
            if (_lex.Kind != TokenKind.Caret) return left;

            _lex.Advance();
            var right = ParseUnary();

            if (!TryNumbers(left, right, "^", out var ln, out var rn))
                return Value.FromString(string.Empty);

            return Value.FromNumber(Math.Pow(ln, rn));
        }

        // Member access, chainable: obj.Member or obj.Method(args). Only objects
        // produced by a supported CreateObject have members; anything else is a
        // construct the engine cannot honour, and is reported as such.
        private Value ParsePostfix()
        {
            var value = ParseAtom();

            while (_lex.Kind == TokenKind.Dot)
            {
                _lex.Advance();

                if (_lex.Kind != TokenKind.Ident)
                {
                    Report(ConditionDiagnosticKind.UnsupportedConstruct,
                        $"expected a member name after '.' in \"{_src}\"");
                    return Value.FromString(string.Empty);
                }

                var member = _lex.Text;
                _lex.Advance();

                // Arguments are optional: FSO.FileExists("x") has them,
                // Network.ComputerName does not.
                var args = new List<string>();
                if (_lex.Kind == TokenKind.LParen)
                {
                    _lex.Advance();
                    while (_lex.Kind != TokenKind.RParen && _lex.Kind != TokenKind.Eof)
                    {
                        args.Add(ParseExpr().AsString());
                        if (_lex.Kind == TokenKind.Comma) _lex.Advance();
                    }
                    if (_lex.Kind == TokenKind.RParen) _lex.Advance();
                }

                if (value.Kind != ValueKind.Object || value.Obj is not ScriptObject target)
                {
                    Report(ConditionDiagnosticKind.UnsupportedConstruct,
                        $"'.{member}' requires an object, but the left side was " +
                        $"\"{value.AsString()}\"");
                    return Value.FromString(string.Empty);
                }

                if (!target.TryInvoke(member, args, out var text))
                {
                    Report(ConditionDiagnosticKind.RequiresComHost,
                        $"{target.ProgId} member '{member}' is not implemented by the " +
                        "native engine; use the vbscript condition engine");
                    return Value.FromString(string.Empty);
                }

                // Point at the native replacement. Advisory, so the result is
                // unaffected — this is a nudge to modernise the XML, not a fault.
                if (ScriptObjectMigration.NativeEquivalentOf(target.ProgId, member) is { } modern)
                {
                    Advise(ConditionDiagnosticKind.ComCompatibilityShim,
                        $"{target.ProgId}.{member} was evaluated by the COM " +
                        $"compatibility shim; {modern} is the native equivalent");
                }

                // Members report booleans as True/False, so fold those back into a
                // real boolean and let anything else stay text.
                value = text is "True" or "False"
                    ? Value.FromBool(text == "True")
                    : Value.FromString(text);
            }

            return value;
        }

        // Arithmetic on something that is not a number is a runtime error in
        // VBScript, so report one instead of silently treating it as zero.
        private bool TryNumbers(Value left, Value right, string op, out double ln, out double rn)
        {
            if (left.TryGetDouble(out ln) && right.TryGetDouble(out rn)) return true;

            var culprit = left.TryGetDouble(out _) ? right : left;
            Report(ConditionDiagnosticKind.EvaluationError,
                $"operator '{op}' needs numbers, but got \"{culprit.AsString()}\"");
            ln = 0;
            rn = 0;
            return false;
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

                    // VBScript keyword literals. Without these, "False" parsed as
                    // the non-empty string "False" and was therefore truthy, so
                    // "True AND False" came out true.
                    if (string.Equals(name, "True", StringComparison.OrdinalIgnoreCase))
                        return Value.FromBool(true);
                    if (string.Equals(name, "False", StringComparison.OrdinalIgnoreCase))
                        return Value.FromBool(false);
                    if (string.Equals(name, "Empty", StringComparison.OrdinalIgnoreCase))
                        return Value.FromString(string.Empty);

                    // Any other bare identifier is an undefined variable to
                    // VBScript. Keep returning its text: callers that want a
                    // value fall back to the literal anyway, and conditions are
                    // failed by the diagnostic the caller records.
                    return Value.FromString(name);
                }
                case TokenKind.Unknown:
                {
                    // Value behaviour is unchanged from before this kind existed —
                    // the character becomes a string — but it is now reported.
                    var text = _lex.Text;
                    Report(ConditionDiagnosticKind.UnsupportedConstruct,
                        DescribeUnsupportedToken(text, _src));
                    _lex.Advance();
                    return Value.FromString(text);
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

        private Value DispatchBuiltin(string name, List<Value> args)
        {
            switch (name.ToUpperInvariant())
            {
                case "CREATEOBJECT":
                {
                    var progId = args.Count > 0 ? args[0].AsString() : string.Empty;
                    var obj = ScriptObject.Create(progId, _services);

                    if (obj is null)
                    {
                        Report(ConditionDiagnosticKind.RequiresComHost,
                            $"CreateObject(\"{progId}\") has no native equivalent; " +
                            "use the vbscript condition engine");
                        return Value.FromString(string.Empty);
                    }

                    return Value.FromObject(obj);
                }

                // GetObject reaches WMI and the running object table, and
                // Eval/Execute run arbitrary script. None has a native
                // equivalent, so configs using them need the script host.
                case "GETOBJECT":
                case "EVAL":
                case "EXECUTE":
                    Report(ConditionDiagnosticKind.RequiresComHost,
                        $"{name}() requires the vbscript condition engine");
                    return Value.FromString(string.Empty);

                case "SPLIT":
                    Report(ConditionDiagnosticKind.UnsupportedConstruct,
                        "Split() returns an array, which the native engine cannot represent");
                    return Value.FromString(string.Empty);
            }

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

                // ---- UiSharp-native additions (no VBScript equivalent) --------
                // These exist so a config can be migrated off the CreateObject
                // compatibility shim. VBScript does not have them, so a config
                // using them will NOT run under the vbscript engine or the
                // original C++ UI++ — that is the trade for dropping the
                // WinPE-Scripting dependency.
                "FILEEXISTS"       => Value.FromBool(_services.FileExists(Str(args, 0))),
                "FOLDEREXISTS"     => Value.FromBool(_services.FolderExists(Str(args, 0))),
                "DRIVEEXISTS"      => Value.FromBool(_services.DriveExists(Str(args, 0))),
                "COMPUTERNAME"     => Value.FromString(_services.ComputerName),
                "USERNAME"         => Value.FromString(_services.UserName),
                "USERDOMAIN"       => Value.FromString(_services.UserDomain),
                "EXPANDENVIRONMENT" => Value.FromString(_services.ExpandEnvironmentStrings(Str(args, 0))),
                "PATHPARENT"       => Value.FromString(FileSystemObject.GetParentFolderName(Str(args, 0))),
                "PATHFILENAME"     => Value.FromString(FileSystemObject.GetFileName(Str(args, 0))),
                "PATHBASENAME"     => Value.FromString(FileSystemObject.GetBaseName(Str(args, 0))),
                "PATHEXTENSION"    => Value.FromString(FileSystemObject.GetExtensionName(Str(args, 0))),
                "PATHDRIVE"        => Value.FromString(FileSystemObject.GetDriveName(Str(args, 0))),
                "PATHCOMBINE"      => Value.FromString(FileSystemObject.BuildPath(Str(args, 0), Str(args, 1))),

                // Numeric functions. VBScript's Round and CInt both use
                // banker's rounding, which is also .NET's MidpointRounding
                // default, so Math.Round matches without extra work.
                // UI++5.xml uses Round() in a preflight check.
                "ROUND"      => Builtin_Round(args),
                "FIX"        => args.Count > 0 && args[0].TryGetDouble(out var fx) ? Value.FromNumber(Math.Truncate(fx)) : Value.FromNumber(0),
                "SGN"        => args.Count > 0 && args[0].TryGetDouble(out var sg) ? Value.FromNumber(Math.Sign(sg))     : Value.FromNumber(0),
                "SQR"        => args.Count > 0 && args[0].TryGetDouble(out var sq) && sq >= 0 ? Value.FromNumber(Math.Sqrt(sq)) : Value.FromNumber(0),
                "CLNG"       => args.Count > 0 && args[0].TryGetDouble(out var cl) ? Value.FromNumber(Math.Round(cl)) : Value.FromNumber(0),
                "CSTR"       => args.Count > 0 ? Value.FromString(args[0].AsString()) : Value.FromString(""),
                "HEX"        => args.Count > 0 && args[0].TryGetDouble(out var hx) ? Value.FromString(((long)Math.Truncate(hx)).ToString("X", CultureInfo.InvariantCulture)) : Value.FromString(""),

                // String functions.
                "ASC"        => args.Count > 0 && args[0].AsString().Length > 0 ? Value.FromNumber(args[0].AsString()[0]) : Value.FromNumber(0),
                "CHR"        => args.Count > 0 && args[0].TryGetDouble(out var ch) ? Value.FromString(((char)(int)ch).ToString()) : Value.FromString(""),
                "STRREVERSE" => Builtin_StrReverse(args),
                "SPACE"      => args.Count > 0 && args[0].TryGetDouble(out var sp) && sp >= 0 ? Value.FromString(new string(' ', (int)sp)) : Value.FromString(""),
                "NOW"        => Value.FromString(DateTime.Now.ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture)),
                "DATE"       => Value.FromString(DateTime.Today.ToString("M/d/yyyy", CultureInfo.InvariantCulture)),
                "TIME"       => Value.FromString(DateTime.Now.ToString("h:mm:ss tt", CultureInfo.InvariantCulture)),
                "YEAR"       => args.Count > 0 ? Value.FromNumber(ParseVbDate(args[0].AsString()).Year)   : Value.FromNumber(DateTime.Now.Year),
                "MONTH"      => args.Count > 0 ? Value.FromNumber(ParseVbDate(args[0].AsString()).Month)  : Value.FromNumber(DateTime.Now.Month),
                "DAY"        => args.Count > 0 ? Value.FromNumber(ParseVbDate(args[0].AsString()).Day)    : Value.FromNumber(DateTime.Now.Day),
                "WEEKDAY"    => args.Count > 0 ? Value.FromNumber((int)ParseVbDate(args[0].AsString()).DayOfWeek + 1) : Value.FromNumber((int)DateTime.Now.DayOfWeek + 1),
                _            => UnknownFunction(name),
            };
        }

        // Round(number [, decimalPlaces])
        private static Value Builtin_Round(List<Value> args)
        {
            if (args.Count == 0 || !args[0].TryGetDouble(out var n)) return Value.FromNumber(0);

            var digits = 0;
            if (args.Count > 1 && args[1].TryGetDouble(out var d))
                digits = Math.Clamp((int)d, 0, 15);

            return Value.FromNumber(Math.Round(n, digits));
        }

        private static Value Builtin_StrReverse(List<Value> args)
        {
            if (args.Count == 0) return Value.FromString("");
            var chars = args[0].AsString().ToCharArray();
            Array.Reverse(chars);
            return Value.FromString(new string(chars));
        }

        // Preserves the historical value (empty string) while making the fact that
        // the function was never evaluated visible to the caller.
        private Value UnknownFunction(string name)
        {
            Report(ConditionDiagnosticKind.UnknownFunction,
                $"'{name}()' is not implemented by the native engine");
            return Value.FromString(string.Empty);
        }

        // VBScript's string functions take an optional trailing compare mode:
        // 0 = vbBinaryCompare (the default), 1 = vbTextCompare. Everything here
        // is binary unless a config explicitly asks for text comparison.
        private static StringComparison CompareModeOf(List<Value> args, int compareArgIndex) =>
            args.Count > compareArgIndex &&
            args[compareArgIndex].TryGetDouble(out var mode) &&
            (int)mode == 1
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

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

            var idx = haystack.IndexOf(needle, startIdx, CompareModeOf(args, compareArgIndex: 3));
            return Value.FromNumber(idx < 0 ? 0 : idx + 1);
        }

        private static Value Builtin_InStrRev(List<Value> args)
        {
            if (args.Count < 2) return Value.FromNumber(0);
            var haystack = args[0].AsString();
            var needle   = args[1].AsString();
            if (needle.Length == 0) return Value.FromNumber(haystack.Length);
            var idx = haystack.LastIndexOf(needle, CompareModeOf(args, compareArgIndex: 2));
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
            return Value.FromString(s.Replace(find, repl, CompareModeOf(args, compareArgIndex: 5)));
        }

        private static DateTime ParseVbDate(string s) =>
            DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt : DateTime.Now;
    }
}
