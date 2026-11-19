using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public abstract class FSharpStringLexerBase(IBuffer buffer, int startBorderLength, int endBorderLength)
    : StringLexerBase(buffer)
  {
    protected override int StartOffset { get; } = startBorderLength;
    protected override int EndOffset { get; } = endBorderLength;

    [CanBeNull]
    protected TokenNodeType TryAdvanceBrace()
    {
      var brace = Buffer[Position];
      if (brace is not ('{' or '}'))
        return null;

      Position++;
      if (CanAdvance && Buffer[Position] == brace)
        return StringTokenTypes.ESCAPE_CHARACTER;

      Position--;
      return StringTokenTypes.INVALID_CHARACTER;
    }
  }

  public class RegularStringLexer(IBuffer buffer, int startBorderLength, int endBorderLength)
    : FSharpStringLexerBase(buffer, startBorderLength, endBorderLength)
  {
    private const uint MaxUnicodeCodePoint = 0x10FFFF;

    private static readonly Dictionary<char, string> ourSimpleEscapes = new()
    {
      { 'n', "\n" },
      { 't', "\t" },
      { 'r', "\r" },
      { 'b', "\b" },
      { 'a', "\a" },
      { 'f', "\f" },
      { 'v', "\v" },
      { '"', "\"" },
      { '\'', "'" },
      { '\\', "\\" }
    };

    protected override TokenNodeType AdvanceInternal()
    {
      if (Buffer[Position] != '\\')
        return StringTokenTypes.CHARACTER;

      Position++;
      if (CanAdvance)
        return ProcessEscapeSequence();

      Position--;
      return StringTokenTypes.CHARACTER;
    }

    protected virtual TokenNodeType ProcessEscapeSequence()
    {
      switch (Buffer[Position])
      {
        case 'u':
          return ProcessFixedLengthEscapeSequence(4, 1, CharEx.IsHexDigitFast);
        case 'U':
          return ProcessLongUnicodeEscapeSequence();
        case 'x':
          return ProcessFixedLengthEscapeSequence(2, 1, CharEx.IsHexDigitFast);
        case '\r':
        case '\n':
          return ProcessLineContinuation();
        case var c when char.IsAsciiDigit(c):
          return ProcessFixedLengthEscapeSequence(3, 0, char.IsAsciiDigit);
        case var c:
          return ourSimpleEscapes.ContainsKey(c) ? StringTokenTypes.ESCAPE_CHARACTER : StringTokenTypes.CHARACTER;
      }
    }

    private TokenNodeType ProcessFixedLengthEscapeSequence(int length, int shift, Func<char, bool> matcher)
    {
      var sequence = ProcessEscapeSequence(length, length, shift, matcher);
      return sequence.Length == length ? StringTokenTypes.ESCAPE_CHARACTER : StringTokenTypes.CHARACTER;
    }

    private TokenNodeType ProcessLongUnicodeEscapeSequence()
    {
      var hex = ProcessEscapeSequence(8, 8, 1, CharEx.IsHexDigitFast);
      if (hex.Length != 8)
        return StringTokenTypes.CHARACTER;

      return uint.TryParse(hex, NumberStyles.HexNumber, null, out var codePoint) && codePoint <= MaxUnicodeCodePoint
        ? StringTokenTypes.ESCAPE_CHARACTER
        : StringTokenTypes.CHARACTER;
    }

    private TokenNodeType ProcessLineContinuation()
    {
      if (Buffer[Position] == '\r' && CheckPosition(Position + 1) && Buffer[Position + 1] == '\n')
        Position++;

      while (CheckPosition(Position + 1) && Buffer[Position + 1] is ' ' or '\t')
        Position++;

      return StringTokenTypes.ESCAPE_CHARACTER;
    }

    protected override string ParseEscapeCharacter(string value)
    {
      if (value[0] != '\\')
        return value[0].ToString();

      switch (value[1])
      {
        case 'u':
          return ParseHexEscapeSequence(value, 4, 4);
        case 'U':
          return ParseHexEscapeSequence(value, 8, 8);
        case 'x':
          return ParseHexEscapeSequence(value, 2, 2);
        case '\r':
        case '\n':
          return string.Empty;
        case var c when char.IsAsciiDigit(c):
          return ((char)unchecked((byte)int.Parse(value.Substring(1), CultureInfo.InvariantCulture))).ToString();
        case var c:
          return ourSimpleEscapes[c];
      }
    }
  }

  public class RegularInterpolatedStringLexer(IBuffer buffer, int startBorderLength, int endBorderLength)
    : RegularStringLexer(buffer, startBorderLength, endBorderLength)
  {
    protected override TokenNodeType AdvanceInternal() =>
      TryAdvanceBrace() ?? base.AdvanceInternal();

    protected override TokenNodeType ProcessEscapeSequence()
    {
      if (Buffer[Position] is not ('{' or '}'))
        return base.ProcessEscapeSequence();

      Position--;
      return StringTokenTypes.CHARACTER;
    }
  }

  public class VerbatimStringLexer(IBuffer buffer, int startBorderLength, int endBorderLength)
    : FSharpStringLexerBase(buffer, startBorderLength, endBorderLength)
  {
    protected override TokenNodeType AdvanceInternal()
    {
      if (Buffer[Position] != '"')
        return StringTokenTypes.CHARACTER;

      Position++;
      return CanAdvance && Buffer[Position] == '"'
        ? StringTokenTypes.ESCAPE_CHARACTER
        : StringTokenTypes.CHARACTER;
    }

    protected override string ParseEscapeCharacter(string value) =>
      value[0].ToString();
  }

  public class VerbatimInterpolatedStringLexer(IBuffer buffer, int startBorderLength, int endBorderLength)
    : VerbatimStringLexer(buffer, startBorderLength, endBorderLength)
  {
    protected override TokenNodeType AdvanceInternal() =>
      TryAdvanceBrace() ?? base.AdvanceInternal();
  }

  public class TripleQuoteStringLexer(IBuffer buffer, int startBorderLength, int endBorderLength)
    : VerbatimStringLexer(buffer, startBorderLength, endBorderLength)
  {
    protected override TokenNodeType AdvanceInternal() =>
      StringTokenTypes.CHARACTER;
  }

  public class TripleQuoteInterpolatedStringLexer(IBuffer buffer, int startBorderLength, int endBorderLength,
    bool checkEscapedBraces) : TripleQuoteStringLexer(buffer, startBorderLength, endBorderLength)
  {
    protected override TokenNodeType AdvanceInternal() =>
      checkEscapedBraces && TryAdvanceBrace() is { } tokenType ? tokenType : base.AdvanceInternal();
  }
}
