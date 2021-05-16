using Fantomas;
using FSharp.Compiler;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Server;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Range = FSharp.Compiler.Range.range;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host
{
  internal class FantomasCodeFormatter
  {
    private readonly FSharpChecker myChecker =
      FSharpChecker.Create(null, null, null, null, null, null, null, null);

    private readonly FormatConfig.FormatConfig myDefaultFormatConfig = FormatConfig.FormatConfig.Default;

    public string FormatSelection(RdFormatSelectionArgs args) =>
      FSharpAsync.StartAsTask(
          CodeFormatter.FormatSelectionAsync(args.FileName, Convert(args.Range),
            SourceOrigin.SourceOrigin.NewSourceString(args.Source), Convert(args.FormatConfig),
            Convert(args.ParsingOptions), myChecker), null, null)
        .Result.Replace("\r\n", args.NewLineText);

    public string FormatDocument(RdFormatDocumentArgs args) =>
      FSharpAsync.StartAsTask(
          CodeFormatter.FormatDocumentAsync(args.FileName, SourceOrigin.SourceOrigin.NewSourceString(args.Source),
            Convert(args.FormatConfig), Convert(args.ParsingOptions), myChecker), null, null)
        .Result.Replace("\r\n", args.NewLineText);

    private static Range Convert(RdFcsRange range) =>
      CodeFormatter.MakeRange(range.FileName, range.StartLine, range.StartCol, range.EndLine, range.EndCol);

    private static FSharpParsingOptions Convert(RdFcsParsingOptions options) =>
      new FSharpParsingOptions(new[] {options.LastSourceFile},
        ListModule.OfArray(options.ConditionalCompilationDefines), ErrorLogger.FSharpErrorSeverityOptions.Default,
        false, options.LightSyntax, false, options.IsExe);

    private FormatConfig.FormatConfig Convert(RdFantomasFormatConfig config) =>
      new FormatConfig.FormatConfig(config.IndentSize, config.MaxLineLength, config.SemicolonAtEndOfLine,
        config.SpaceBeforeParameter, config.SpaceBeforeLowercaseInvocation, config.SpaceBeforeUppercaseInvocation,
        config.SpaceBeforeClassConstructor, config.SpaceBeforeMember, config.SpaceBeforeColon, config.SpaceAfterComma,
        config.SpaceBeforeSemicolon, config.SpaceAfterSemicolon, config.IndentOnTryWith, config.SpaceAroundDelimiter,
        config.MaxIfThenElseShortWidth, config.MaxInfixOperatorExpression, config.MaxRecordWidth,
        myDefaultFormatConfig.MaxRecordNumberOfItems, myDefaultFormatConfig.RecordMultilineFormatter,
        config.MaxArrayOrListWidth, myDefaultFormatConfig.MaxArrayOrListNumberOfItems,
        myDefaultFormatConfig.ArrayOrListMultilineFormatter, config.MaxValueBindingWidth,
        config.MaxFunctionBindingWidth, myDefaultFormatConfig.MaxDotGetExpressionWidth,
        config.MultilineBlockBracketsOnSameColumn, config.NewlineBetweenTypeDefinitionAndMembers,
        config.KeepIfThenInSameLine, config.MaxElmishWidth, config.SingleArgumentWebMode,
        config.AlignFunctionSignatureToIndentation, config.AlternativeLongMemberDefinitions,
        myDefaultFormatConfig.MultiLineLambdaClosingNewline, myDefaultFormatConfig.DisableElmishSyntax,
        myDefaultFormatConfig.EndOfLine, myDefaultFormatConfig.StrictMode);
  }
}
