// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;

namespace Sharpmake;

public enum TokenType
{
    Literal,
    Expression,
    EscapedExpression
}

public readonly struct Delimiter
{
    public string Open { get; }
    public char Close { get; }

    public Delimiter(string open, char close)
    {
        if (string.IsNullOrEmpty(open))
            throw new ArgumentException("Open token cannot be null or empty.", nameof(open));
        Open = open;
        Close = close;
    }

    public ReadOnlySpan<char> OpenSpan => Open.AsSpan();
}

public readonly ref struct Token
{
    public ReadOnlySpan<char> Value { get; }
    public TokenType Type { get; }

    public Token(ReadOnlySpan<char> value, TokenType type)
    {
        Value = value;
        Type = type;
    }
}

public ref struct TokenEnumerator
{
    private readonly ReadOnlySpan<char> _input;
    private readonly Delimiter[] _delimiters;
    private int _pos;

    public TokenEnumerator(ReadOnlySpan<char> input, Delimiter[] delimiters)
    {
        _input = input;
        _delimiters = delimiters;
        _pos = 0;
        Current = default;
    }

    public Token Current { get; private set; }

    public bool MoveNext()
    {
        if (_pos >= _input.Length)
            return false;

        int literalStart = _pos;

        while (_pos < _input.Length)
        {
            int matchedIdx = MatchAnyOpen(_input[_pos..], out Delimiter delimiter);
            if (matchedIdx == -1)
            {
                _pos++;
                continue;
            }

            // Literal before match
            if (_pos > literalStart)
            {
                Current = new Token(_input[literalStart.._pos], TokenType.Literal);
                return true;
            }

            // Escaped expression (only for single-char open)
            if (delimiter.Open.Length == 1 &&
                _pos + 1 < _input.Length &&
                _input[_pos + 1] == delimiter.Open[0])
            {
                var escapeClose = new string(delimiter.Open[0], 1) + delimiter.Close;
                int closeIdx = _input[(_pos + 2)..].IndexOf(escapeClose.AsSpan());
                if (closeIdx == -1)
                {
                    Current = new Token(_input[_pos..], TokenType.Literal);
                    _pos = _input.Length;
                    return true;
                }
                closeIdx += _pos + 2;
                Current = new Token(_input.Slice(_pos + 1, closeIdx - _pos - 1), TokenType.EscapedExpression);
                _pos = closeIdx + 2;
                return true;
            }

            // Normal expression
            int searchStart = _pos + delimiter.Open.Length;
            int closePos = _input[searchStart..].IndexOf(delimiter.Close);
            if (closePos == -1)
            {
                Current = new Token(_input[_pos..], TokenType.Literal);
                _pos = _input.Length;
                return true;
            }
            closePos += searchStart;

            Current = new Token(_input[searchStart..closePos], TokenType.Expression);
            _pos = closePos + 1;
            return true;
        }

        // Emit final literal if any
        if (literalStart < _input.Length)
        {
            Current = new Token(_input[literalStart..], TokenType.Literal);
            _pos = _input.Length;
            return true;
        }

        return false;
    }

    private int MatchAnyOpen(ReadOnlySpan<char> span, out Delimiter matched)
    {
        for (int i = 0; i < _delimiters.Length; i++)
        {
            var open = _delimiters[i].OpenSpan;
            if (span.StartsWith(open))
            {
                matched = _delimiters[i];
                return i;
            }
        }
        matched = default;
        return -1;
    }
}

public static class TokenParser
{
    public static TokenEnumerator ParseTokens(ReadOnlySpan<char> input, Delimiter[] delimiters)
        => new TokenEnumerator(input, delimiters);
}
