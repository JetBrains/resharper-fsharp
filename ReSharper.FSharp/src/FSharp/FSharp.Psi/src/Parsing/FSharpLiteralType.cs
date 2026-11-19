using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public enum FSharpLiteralType
  {
    /// <summary>'{char}'</summary>
    Character,

    /// <summary>"{string}"</summary>
    RegularString,

    /// <summary>@"{string}"</summary>
    VerbatimString,

    /// <summary>@"{string}"B</summary>
    VerbatimByteArray,

    /// <summary>"""{string}"""</summary>
    TripleQuoteString,

    /// <summary>"{string}"B</summary>
    ByteArray,

    /// <summary>$"string"</summary>
    InterpolatedString,

    /// <summary>$"string{</summary>
    InterpolatedStringStart,

    /// <summary>}string{</summary>
    InterpolatedStringMiddle,

    /// <summary>}string"</summary>
    InterpolatedStringEnd,

    /// <summary>$@"{string}" @$"{string}"</summary>
    VerbatimInterpolatedString,

    /// <summary>$@"{string}{ @$"{string}{</summary>
    VerbatimInterpolatedStringStart,

    /// <summary>}{string}{</summary>
    VerbatimInterpolatedStringMiddle,

    /// <summary>}{string}"</summary>
    VerbatimInterpolatedStringEnd,

    /// <summary>$"""{string}"""</summary>
    TripleQuoteInterpolatedString,

    /// <summary>$"""{</summary>
    TripleQuoteInterpolatedStringStart,

    /// <summary>}{string}{</summary>
    TripleQuoteInterpolatedStringMiddle,

    /// <summary>}{string}"""</summary>
    TripleQuoteInterpolatedStringEnd,

    /// <summary>$$"""string"""</summary>
    RawInterpolatedString,

    /// <summary>$$"""string{{</summary>
    RawInterpolatedStringStart,

    /// <summary>}}string{{</summary>
    RawInterpolatedStringMiddle,

    /// <summary>}}string"</summary>
    RawInterpolatedStringEnd
  }

  public static class FSharpLiteralTypeExtensions
  {
    public const int DefaultRawStringDollarCount = 2;

    private static readonly NodeTypeDictionary<FSharpLiteralType?> ourLiteralTypes =
      new(new List<KeyValuePair<NodeType, FSharpLiteralType?>>
      {
        new(FSharpTokenType.CHARACTER_LITERAL, FSharpLiteralType.Character),
        new(FSharpTokenType.STRING, FSharpLiteralType.RegularString),
        new(FSharpTokenType.VERBATIM_STRING, FSharpLiteralType.VerbatimString),
        new(FSharpTokenType.VERBATIM_BYTEARRAY, FSharpLiteralType.VerbatimByteArray),
        new(FSharpTokenType.TRIPLE_QUOTED_STRING, FSharpLiteralType.TripleQuoteString),
        new(FSharpTokenType.BYTEARRAY, FSharpLiteralType.ByteArray),

        new(FSharpTokenType.RAW_INTERPOLATED_STRING, FSharpLiteralType.RawInterpolatedString),
        new(FSharpTokenType.RAW_INTERPOLATED_STRING_START, FSharpLiteralType.RawInterpolatedStringStart),
        new(FSharpTokenType.RAW_INTERPOLATED_STRING_MIDDLE, FSharpLiteralType.RawInterpolatedStringMiddle),
        new(FSharpTokenType.RAW_INTERPOLATED_STRING_END, FSharpLiteralType.RawInterpolatedStringEnd),

        new(FSharpTokenType.REGULAR_INTERPOLATED_STRING, FSharpLiteralType.InterpolatedString),
        new(FSharpTokenType.REGULAR_INTERPOLATED_STRING_START, FSharpLiteralType.InterpolatedStringStart),
        new(FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE, FSharpLiteralType.InterpolatedStringMiddle),
        new(FSharpTokenType.REGULAR_INTERPOLATED_STRING_END, FSharpLiteralType.InterpolatedStringEnd),

        new(FSharpTokenType.VERBATIM_INTERPOLATED_STRING, FSharpLiteralType.VerbatimInterpolatedString),
        new(FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START, FSharpLiteralType.VerbatimInterpolatedStringStart),
        new(FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE, FSharpLiteralType.VerbatimInterpolatedStringMiddle),
        new(FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END, FSharpLiteralType.VerbatimInterpolatedStringEnd),

        new(FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING, FSharpLiteralType.TripleQuoteInterpolatedString),
        new(FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START, FSharpLiteralType.TripleQuoteInterpolatedStringStart),
        new(FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE, FSharpLiteralType.TripleQuoteInterpolatedStringMiddle),
        new(FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END, FSharpLiteralType.TripleQuoteInterpolatedStringEnd),

        new(FSharpTokenType.UNFINISHED_STRING, FSharpLiteralType.RegularString),
        new(FSharpTokenType.UNFINISHED_VERBATIM_STRING, FSharpLiteralType.VerbatimString),
        new(FSharpTokenType.UNFINISHED_TRIPLE_QUOTED_STRING, FSharpLiteralType.TripleQuoteString),
        new(FSharpTokenType.UNFINISHED_REGULAR_INTERPOLATED_STRING, FSharpLiteralType.InterpolatedString),
        new(FSharpTokenType.UNFINISHED_VERBATIM_INTERPOLATED_STRING, FSharpLiteralType.VerbatimInterpolatedString),
        new(FSharpTokenType.UNFINISHED_TRIPLE_QUOTE_INTERPOLATED_STRING, FSharpLiteralType.TripleQuoteInterpolatedString),
        new(FSharpTokenType.UNFINISHED_RAW_INTERPOLATED_STRING, FSharpLiteralType.RawInterpolatedString),
      });

    public static FSharpLiteralType GetLiteralType([NotNull] this TokenNodeType tokenType) =>
      ourLiteralTypes[tokenType] ?? throw new ArgumentException($"Token {tokenType} is not a string literal");

    public static (int Start, int End) GetBorderLengths([NotNull] this TokenNodeType tokenType, int dollarCount)
    {
      var (start, end) = tokenType.GetLiteralType().GetBorderLengths(dollarCount);
      return FSharpTokenType.UnfinishedStrings[tokenType] ? (start, 0) : (start, end);
    }

    private static (int Start, int End) GetBorderLengths(this FSharpLiteralType literalType, int dollarCount) =>
      literalType switch
      {
        FSharpLiteralType.Character => (1, 1),
        FSharpLiteralType.RegularString => (1, 1),
        FSharpLiteralType.VerbatimString => (2, 1),
        FSharpLiteralType.VerbatimByteArray => (2, 2),
        FSharpLiteralType.TripleQuoteString => (3, 3),
        FSharpLiteralType.ByteArray => (1, 2),

        FSharpLiteralType.InterpolatedString => (2, 1),
        FSharpLiteralType.InterpolatedStringStart => (2, 1),
        FSharpLiteralType.InterpolatedStringMiddle => (1, 1),
        FSharpLiteralType.InterpolatedStringEnd => (1, 1),

        FSharpLiteralType.VerbatimInterpolatedString => (3, 1),
        FSharpLiteralType.VerbatimInterpolatedStringStart => (3, 1),
        FSharpLiteralType.VerbatimInterpolatedStringMiddle => (1, 1),
        FSharpLiteralType.VerbatimInterpolatedStringEnd => (1, 1),

        FSharpLiteralType.TripleQuoteInterpolatedString => (4, 3),
        FSharpLiteralType.TripleQuoteInterpolatedStringStart => (4, 1),
        FSharpLiteralType.TripleQuoteInterpolatedStringMiddle => (1, 1),
        FSharpLiteralType.TripleQuoteInterpolatedStringEnd => (1, 3),

        FSharpLiteralType.RawInterpolatedString => (dollarCount + 3, 3),
        FSharpLiteralType.RawInterpolatedStringStart => (dollarCount + 3, dollarCount),
        FSharpLiteralType.RawInterpolatedStringMiddle => (dollarCount, dollarCount),
        FSharpLiteralType.RawInterpolatedStringEnd => (dollarCount, 3),

        _ => throw new ArgumentOutOfRangeException(nameof(literalType), literalType, null)
      };

    [NotNull]
    public static FSharpStringLexerBase CreateLexer([NotNull] this TokenNodeType tokenType, [NotNull] IBuffer buffer,
      int dollarCount)
    {
      var literalType = tokenType.GetLiteralType();
      var (start, end) = tokenType.GetBorderLengths(dollarCount);

      switch (literalType)
      {
        case FSharpLiteralType.Character:
        case FSharpLiteralType.RegularString:
        case FSharpLiteralType.ByteArray:
          return new RegularStringLexer(buffer, start, end);

        case FSharpLiteralType.VerbatimString:
        case FSharpLiteralType.VerbatimByteArray:
          return new VerbatimStringLexer(buffer, start, end);

        case FSharpLiteralType.TripleQuoteString:
          return new TripleQuoteStringLexer(buffer, start, end);

        case FSharpLiteralType.InterpolatedString:
        case FSharpLiteralType.InterpolatedStringStart:
        case FSharpLiteralType.InterpolatedStringMiddle:
        case FSharpLiteralType.InterpolatedStringEnd:
          return new RegularInterpolatedStringLexer(buffer, start, end);

        case FSharpLiteralType.VerbatimInterpolatedString:
        case FSharpLiteralType.VerbatimInterpolatedStringStart:
        case FSharpLiteralType.VerbatimInterpolatedStringMiddle:
        case FSharpLiteralType.VerbatimInterpolatedStringEnd:
          return new VerbatimInterpolatedStringLexer(buffer, start, end);

        case FSharpLiteralType.TripleQuoteInterpolatedString:
        case FSharpLiteralType.TripleQuoteInterpolatedStringStart:
        case FSharpLiteralType.TripleQuoteInterpolatedStringMiddle:
        case FSharpLiteralType.TripleQuoteInterpolatedStringEnd:
          return new TripleQuoteInterpolatedStringLexer(buffer, start, end, true);

        case FSharpLiteralType.RawInterpolatedString:
        case FSharpLiteralType.RawInterpolatedStringStart:
        case FSharpLiteralType.RawInterpolatedStringMiddle:
        case FSharpLiteralType.RawInterpolatedStringEnd:
          return new TripleQuoteInterpolatedStringLexer(buffer, start, end, false);

        default:
          throw new ArgumentOutOfRangeException(nameof(literalType), literalType, null);
      }
    }
  }
}
