using Fantomas;
using FSharp.Compiler.SourceCodeServices;
using FSharp.Compiler.Text;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Server;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host
{
  internal class FantomasCodeFormatter
  {
    private readonly FSharpChecker myChecker =
      FSharpChecker.Create(null, null, null, null, null, null, null, null, null);

    private static readonly FormatConfig.FormatConfig DefaultFormatConfig = FormatConfig.FormatConfig.Default;
    private static readonly FSharpDiagnosticOptions DefaultDiagnosticOptions = FSharpDiagnosticOptions.Default;

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
      new FSharpParsingOptions(new[] { options.LastSourceFile },
        ListModule.OfArray(options.ConditionalCompilationDefines), DefaultDiagnosticOptions,
        false, options.LightSyntax, false, options.IsExe);

    private static FormatConfig.FormatConfig Convert(RdFantomasFormatConfig config) =>
      new FormatConfig.FormatConfig(config.IndentSize, config.MaxLineLength, config.SemicolonAtEndOfLine,
        config.SpaceBeforeParameter, config.SpaceBeforeLowercaseInvocation, config.SpaceBeforeUppercaseInvocation,
        config.SpaceBeforeClassConstructor, config.SpaceBeforeMember, config.SpaceBeforeColon, config.SpaceAfterComma,
        config.SpaceBeforeSemicolon, config.SpaceAfterSemicolon, config.IndentOnTryWith, config.SpaceAroundDelimiter,
        config.MaxIfThenElseShortWidth, config.MaxInfixOperatorExpression, config.MaxRecordWidth,
        DefaultFormatConfig.MaxRecordNumberOfItems, DefaultFormatConfig.RecordMultilineFormatter,
        config.MaxArrayOrListWidth, DefaultFormatConfig.MaxArrayOrListNumberOfItems,
        DefaultFormatConfig.ArrayOrListMultilineFormatter, config.MaxValueBindingWidth,
        config.MaxFunctionBindingWidth, DefaultFormatConfig.MaxDotGetExpressionWidth,
        config.MultilineBlockBracketsOnSameColumn, config.NewlineBetweenTypeDefinitionAndMembers,
        config.KeepIfThenInSameLine, config.MaxElmishWidth, config.SingleArgumentWebMode,
        config.AlignFunctionSignatureToIndentation, config.AlternativeLongMemberDefinitions,
        DefaultFormatConfig.MultiLineLambdaClosingNewline, DefaultFormatConfig.DisableElmishSyntax,
        DefaultFormatConfig.EndOfLine, DefaultFormatConfig.KeepIndentInBranch,
        DefaultFormatConfig.BlankLinesAroundNestedMultilineExpressions,
        DefaultFormatConfig.BarBeforeDiscriminatedUnionDeclaration, DefaultFormatConfig.StrictMode);
  }
}
