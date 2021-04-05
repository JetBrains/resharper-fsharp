using Fantomas;
using FSharp.Compiler;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Diagnostics;
using FSharp.Compiler.SourceCodeServices;
using FSharp.Compiler.Text;
using JetBrains.Rider.FSharp.ExternalFormatter.Client;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;

namespace FSharp.ExternalFormatter
{
  internal interface ICodeFormatterProvider
  {
    string FormatSelection(RdFormatSelectionArgs args);
    string FormatDocument(RdFormatDocumentArgs args);
  }

  internal class BundledCodeFormatter : ICodeFormatterProvider
  {
    private readonly FSharpChecker myChecker =
      FSharpChecker.Create(null, null, null, null, null, null, null, null, null, null);

    private readonly FormatConfig.FormatConfig myDefaultFormatConfig = FormatConfig.FormatConfig.Default;

    public string FormatSelection(RdFormatSelectionArgs args) =>
      FSharpAsync.StartAsTask(
          CodeFormatter.FormatSelectionAsync(
            args.FileName,
            Convert(args.Range),
            SourceOrigin.SourceOrigin.NewSourceString(args.Source),
            Convert(args.FormatConfig),
            Convert(args.ParsingOptions),
            myChecker), null, null)
        .Result;

    public string FormatDocument(RdFormatDocumentArgs args) =>
      FSharpAsync.StartAsTask(
          CodeFormatter.FormatDocumentAsync(
            args.FileName,
            SourceOrigin.SourceOrigin.NewSourceString(args.Source),
            Convert(args.FormatConfig),
            Convert(args.ParsingOptions),
            myChecker), null, null)
        .Result;

    private static Range Convert(RdRange range) =>
      CodeFormatter.MakeRange(range.FileName, range.StartLine, range.StartCol, range.EndLine, range.EndCol);

    private static FSharpParsingOptions Convert(RdParsingOptions options) =>
      new FSharpParsingOptions(
        options.SourceFiles,
        FSharpList<string>.Empty,
        FSharpDiagnosticOptions.Default,
        false,
        options.LightSyntax,
        false,
        false);

    private FormatConfig.FormatConfig Convert(RdFormatConfig config) =>
      new FormatConfig.FormatConfig(
        config.IndentSize,
        config.MaxLineLength,
        myDefaultFormatConfig.SemicolonAtEndOfLine,
        config.SpaceBeforeParameter,
        config.SpaceBeforeLowercaseInvocation,
        config.SpaceBeforeUppercaseInvocation,
        config.SpaceBeforeClassConstructor,
        config.SpaceBeforeMember,
        config.SpaceBeforeColon,
        config.SpaceAfterComma,
        config.SpaceBeforeSemicolon,
        config.SpaceAfterSemicolon,
        config.IndentOnTryWith,
        config.SpaceAroundDelimiter,
        config.MaxIfThenElseShortWidth,
        config.MaxInfixOperatorExpression,
        config.MaxRecordWidth,
        myDefaultFormatConfig.MaxRecordNumberOfItems,
        myDefaultFormatConfig.RecordMultilineFormatter,
        config.MaxArrayOrListWidth,
        myDefaultFormatConfig.MaxArrayOrListNumberOfItems,
        myDefaultFormatConfig.ArrayOrListMultilineFormatter,
        config.MaxValueBindingWidth,
        config.MaxFunctionBindingWidth,
        myDefaultFormatConfig.MaxDotGetExpressionWidth,
        config.MultilineBlockBracketsOnSameColumn,
        config.NewlineBetweenTypeDefinitionAndMembers,
        config.KeepIfThenInSameLine,
        config.MaxElmishWidth,
        config.SingleArgumentWebMode,
        config.AlignFunctionSignatureToIndentation,
        config.AlternativeLongMemberDefinitions,
        myDefaultFormatConfig.MultiLineLambdaClosingNewline,
        myDefaultFormatConfig.DisableElmishSyntax, myDefaultFormatConfig.EndOfLine,
        myDefaultFormatConfig.StrictMode);
  }
}
