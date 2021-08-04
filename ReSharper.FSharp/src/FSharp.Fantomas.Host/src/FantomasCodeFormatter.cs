using System;
using System.Linq;
using Fantomas;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Diagnostics;
using JetBrains.Extension;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Server;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
using FormatConfig = Fantomas.FormatConfig.FormatConfig;
using FSharpType = Microsoft.FSharp.Reflection.FSharpType;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host
{
  // TODO: collect used Fantomas versions
  internal class FantomasCodeFormatter
  {
    private static FSharpChecker GetFSharpChecker()
    {
      var method = typeof(FSharpChecker).GetMethod("Create");
      Assertion.AssertNotNull(method, "FSharpChecker must contain static .Create method");

      var values = method
        .GetParameters()
        .Select(t => t.ParameterType.GetDefaultValue())
        .ToArray();
      return method.Invoke(null, values) as FSharpChecker;
    }

    private static dynamic GetDiagnosticOptions()
    {
      var assemblyToSearch = typeof(FSharpParsingOptions).Assembly;
      var searchedType = Version.Parse(CodeFormatter.GetVersion()) < Version.Parse("4.5")
        ? "FSharp.Compiler.ErrorLogger+FSharpErrorSeverityOptions"
        : "FSharp.Compiler.SourceCodeServices.FSharpDiagnosticOptions";

      var options = assemblyToSearch.GetType(searchedType);
      Assertion.AssertNotNull(options, $"{searchedType} must exist");

      var defaultValue = options.GetProperty("Default")?.GetValue(null);
      Assertion.AssertNotNull(defaultValue, "Default != null");

      return defaultValue;
    }

    private readonly FSharpChecker myChecker = GetFSharpChecker();
    private static readonly dynamic DefaultDiagnosticOptions = GetDiagnosticOptions();
    private static readonly FormatConfig DefaultFormatConfig = FormatConfig.Default;
    private static readonly Type FormatConfigType = typeof(FormatConfig);

    private static readonly Type MultilineFormatterType =
      FormatConfigType.Assembly.GetType("Fantomas.FormatConfig+MultilineFormatterType");

    private static bool IsMultilineFormatterType(object obj) => obj.GetType().Name == "MultilineFormatterType";

    private static readonly (string Name, object Value)[] FormatConfigFields =
      FSharpType.GetRecordFields(FormatConfigType, null)
        .Select(x => x.Name)
        .Zip(FSharpValue.GetRecordFields(DefaultFormatConfig, null), (name, value) => (name, value))
        .ToArray();

    public static readonly (string Name, object Value)[] EditorConfigFields =
      FormatConfigFields.Where(t => t.Value is int || t.Value is bool || IsMultilineFormatterType(t.Value)).ToArray();

    public string FormatSelection(RdFormatSelectionArgs args)
    {
      var rdRange = args.Range;

      var range =
        CodeFormatter.MakeRange(rdRange.FileName, rdRange.StartLine, rdRange.StartCol, rdRange.EndLine, rdRange.EndCol);
      return FSharpAsync.StartAsTask(
          CodeFormatter.FormatSelectionAsync(args.FileName, range,
            SourceOrigin.SourceOrigin.NewSourceString(args.Source), Convert(args.FormatConfig),
            Convert(args.ParsingOptions), myChecker), null, null)
        .Result.Replace("\r\n", args.NewLineText);
    }

    public string FormatDocument(RdFormatDocumentArgs args) =>
      FSharpAsync.StartAsTask(
          CodeFormatter.FormatDocumentAsync(args.FileName, SourceOrigin.SourceOrigin.NewSourceString(args.Source),
            Convert(args.FormatConfig), Convert(args.ParsingOptions), myChecker), null, null)
        .Result.Replace("\r\n", args.NewLineText);

    private static FSharpParsingOptions Convert(RdFcsParsingOptions options) =>
      new FSharpParsingOptions(new[] { options.LastSourceFile },
        ListModule.OfArray(options.ConditionalCompilationDefines), DefaultDiagnosticOptions,
        false, options.LightSyntax ?? FSharpOption<bool>.None, false, options.IsExe);

    private static FormatConfig Convert(string[] riderFormatConfigValues)
    {
      var riderFormatConfigDict =
        EditorConfigFields
          .Zip(riderFormatConfigValues,
            (field, valueData) =>
              (field.Name, Value: valueData == ""
                ? field.Value
                : field.Value switch
                {
                  int _ => int.Parse(valueData),
                  bool _ => bool.Parse(valueData),
                  { } x when IsMultilineFormatterType(x) => Convert(valueData),
                  { } x => throw new InvalidOperationException($"Unexpected FormatConfig field {field.Name} = '{x}'")
                }))
          .ToDictionary(x => x.Name, x => x.Value);

      var formatConfigValues =
        FormatConfigFields
          .Select(field => riderFormatConfigDict.TryGetValue(field.Name, out var value) ? value : field.Value)
          .ToArray();
      return FSharpValue.MakeRecord(FormatConfigType, formatConfigValues, null) as FormatConfig;
    }

    private static object Convert(string setting) => setting switch
    {
      "character_width" => Enum.Parse(MultilineFormatterType, "CharacterWidth"),
      "number_of_items" => Enum.Parse(MultilineFormatterType, "NumberOfItems"),
      _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, null)
    };
  }
}
