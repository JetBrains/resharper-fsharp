using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fantomas;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Diagnostics;
using JetBrains.Extension;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Server;
using JetBrains.Util;
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

      var options = assemblyToSearch.GetType(searchedType).NotNull($"{searchedType} must exist");
      var defaultValue = options.GetProperty("Default")?.GetValue(null).NotNull();

      return defaultValue;
    }

    private readonly FSharpChecker myChecker = GetFSharpChecker();
    private static readonly dynamic DefaultDiagnosticOptions = GetDiagnosticOptions();
    private static readonly FormatConfig DefaultFormatConfig = FormatConfig.Default;
    private static readonly Type FormatConfigType = typeof(FormatConfig);

    public static readonly (string Name, object Value)[] FormatConfigFields =
      FSharpType.GetRecordFields(FormatConfigType, null)
        .Select(x => x.Name)
        .Zip(FSharpValue.GetRecordFields(DefaultFormatConfig, null), (name, value) => (name, value))
        .ToArray();

    private static readonly Dictionary<string, UnionCaseInfo> FormatConfigDUs =
      FormatConfigFields
        .Select(t => t.Value.GetType())
        .Where(t => FSharpType.IsUnion(t, null))
        .Distinct(t => t.FullName)
        .SelectMany(t => FSharpType.GetUnionCases(t, null))
        .ToDictionary(t => t.Name);

    public string FormatSelection(RdFantomasFormatSelectionArgs args)
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

    public string FormatDocument(RdFantomasFormatDocumentArgs args) =>
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
        FormatConfigFields
          .Zip(riderFormatConfigValues,
            (field, valueData) =>
              (field.Name, Value: valueData == ""
                ? field.Value
                : field.Value switch
                {
                  int _ => int.Parse(valueData),
                  bool _ => bool.Parse(valueData),
                  { } => ConvertEnumValue(valueData)
                }))
          .ToDictionary(x => x.Name, x => x.Value);

      var formatConfigValues =
        FormatConfigFields
          .Select(field => riderFormatConfigDict.TryGetValue(field.Name, out var value) ? value : field.Value)
          .ToArray();
      return FSharpValue.MakeRecord(FormatConfigType, formatConfigValues, null) as FormatConfig;
    }

    // TODO: alternatively, we can reuse the logic from
    // https://github.com/fsprojects/fantomas/blob/master/src/Fantomas.Extras/EditorConfig.fs
    // such as `parseOptionsFromEditorConfig`,
    // or take the OfConfigString methods of discriminated unions as a contract
    // https://github.com/fsprojects/fantomas/blob/master/src/Fantomas/FormatConfig.fs
    private static object ConvertEnumValue(string setting)
    {
      var camelCaseSetting = StringUtil.MakeUpperCamelCaseName(setting);

      return FormatConfigDUs.TryGetValue(camelCaseSetting, out var unionCase)
        ? FSharpValue.MakeUnion(unionCase, null, FSharpOption<BindingFlags>.None)
        : throw new ArgumentOutOfRangeException($"Unknown Fantomas FormatSetting {setting}");
    }
  }
}
