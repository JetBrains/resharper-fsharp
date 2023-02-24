using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Extension;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Server;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
using FSharpType = Microsoft.FSharp.Reflection.FSharpType;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host
{
  // TODO: collect used Fantomas versions
  internal static class FantomasCodeFormatter
  {
    private static readonly Assembly FantomasAssembly = FantomasAssemblyResolver.LoadFantomasAssembly();
    public static readonly Version CurrentVersion = FantomasAssembly.GetName().Version;
    private static readonly string FantomasAssemblyName = FantomasAssembly.GetName().Name;

    private static readonly Version Version45 = Version.Parse("4.5");
    private static readonly Version Version46 = Version.Parse("4.6");
    private static readonly Version Version60 = Version.Parse("6.0");

    private static Type GetCodeFormatter() =>
      FantomasAssembly
        .GetType($"{FantomasAssemblyName}.CodeFormatter")
        .NotNull("CodeFormatter must exist");

    private static object GetFSharpChecker()
    {
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Version) return null;

      var searchedType = CurrentVersion < Version46
        ? "FSharp.Compiler.SourceCodeServices.FSharpChecker"
        : "FSharp.Compiler.CodeAnalysis.FSharpChecker";

      var qualifiedName = Assembly.CreateQualifiedName("FSharp.Compiler.Service", searchedType);
      var type = Type.GetType(qualifiedName).NotNull($"{qualifiedName} must exist");
      var method = type.GetMethod("Create").NotNull("FSharpChecker must contain static .Create method");

      var values = method
        .GetParameters()
        .Select(t => t.ParameterType.GetDefaultValue())
        .ToArray();

      return method.Invoke(null, values);
    }

    private static MethodInfo GetSourceOriginStringConstructor()
    {
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Version) return null;

      return FantomasAssembly
        .GetType($"{FantomasAssemblyName}.SourceOrigin")
        .NotNull($"SourceOrigin must exist")
        .GetNestedType("SourceOrigin")
        .NotNull($"SourceOrigin.SourceOrigin must exist")
        .GetMethod("NewSourceString")
        .NotNull($"SourceOrigin.SourceOrigin must contain static .NewSourceString method");
    }

    private static object GetDiagnosticOptions()
    {
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Version) return null;

      var assemblyToSearch = FSharpParsingOptionsType.Assembly;
      var optionsTypeName = CurrentVersion switch
      {
        { } v when v < Version45 => "FSharp.Compiler.ErrorLogger+FSharpErrorSeverityOptions",
        { } v when v < Version46 => "FSharp.Compiler.SourceCodeServices.FSharpDiagnosticOptions",
        _ => "FSharp.Compiler.Diagnostics.FSharpDiagnosticOptions",
      };

      var optionsType = assemblyToSearch.GetType(optionsTypeName).NotNull($"{optionsTypeName} must exist");
      var defaultValue = optionsType.GetProperty("Default")?.GetValue(null).NotNull();

      return defaultValue;
    }

    private static object GetDefaultFormatConfig() =>
      FormatConfigType
        .GetProperty("Default")
        .NotNull("FormatConfig must contain static .Default property")
        .GetValue(null)
        .NotNull();

    private static Type GetFSharpParsingOptions()
    {
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Version) return null;

      var searchedType = CurrentVersion < Version46
        ? "FSharp.Compiler.SourceCodeServices.FSharpParsingOptions"
        : "FSharp.Compiler.CodeAnalysis.FSharpParsingOptions";

      var qualifiedName = Assembly.CreateQualifiedName("FSharp.Compiler.Service", searchedType);
      return Type.GetType(qualifiedName).NotNull($"{qualifiedName} must exist");
    }

    private static Type GetFormatConfigType()
    {
      var formatConfig = FantomasAssembly
        .GetType($"{FantomasAssemblyName}.FormatConfig")
        .NotNull("FormatConfig must exist");

      return CurrentVersion >= Version60
        ? formatConfig
        : formatConfig
          .GetNestedType("FormatConfig")
          .NotNull();
    }

    private static readonly Type CodeFormatterType = GetCodeFormatter();
    private static readonly Type FSharpParsingOptionsType = GetFSharpParsingOptions();
    private static readonly Type FormatConfigType = GetFormatConfigType();

    private static readonly object DefaultDiagnosticOptions = GetDiagnosticOptions();
    private static readonly object DefaultFormatConfig = GetDefaultFormatConfig();
    private static readonly object Checker = GetFSharpChecker();

    private static readonly ConstructorInfo
      CreateFSharpParsingOptions = FSharpParsingOptionsType?.GetConstructors().Single();

    private static readonly MethodInfo MakeRangeMethod = CodeFormatterType.GetMethod("MakeRange");
    private static readonly MethodInfo MakePositionMethod = CodeFormatterType.GetMethod("MakePosition");
    private static readonly MethodInfo SourceOriginConstructor = GetSourceOriginStringConstructor();

    private static readonly MethodInfo CreateOptionMethod =
      typeof(FSharpOption<>)
        .MakeGenericType(FormatConfigType)
        .GetMethod("Some")
        .NotNull("FSharpOption.Some");

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

    public static string FormatSelection(RdFantomasFormatSelectionArgs args)
    {
      var rdRange = args.Range;
      var range =
        MakeRangeMethod.Invoke(null,
          new object[] { rdRange.FileName, rdRange.StartLine, rdRange.StartCol, rdRange.EndLine, rdRange.EndCol });

      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Version)
        return FSharpAsync.StartAsTask(
            CodeFormatterType.InvokeMember("FormatSelectionAsync",
              BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, Type.DefaultBinder, null, new[]
              {
                args.FileName.EndsWith(".fsi"), // isSignature
                args.Source,
                range,
                ConvertToFormatConfig(args.FormatConfig)
              }) as dynamic, null, null) // FSharpAsync<Tuple<string, Range>>
          .Result.Item1.Replace("\r\n", args.NewLineText);

      return FSharpAsync.StartAsTask(
          CodeFormatterType.InvokeMember("FormatSelectionAsync",
            BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, Type.DefaultBinder, null, new[]
            {
              args.FileName, range,
              SourceOriginConstructor.Invoke(null, new object[] { args.Source }),
              ConvertToFormatConfig(args.FormatConfig),
              CreateFSharpParsingOptions.Invoke(GetParsingOptions(args.ParsingOptions).ToArray()),
              Checker
            }) as FSharpAsync<string>, null, null)
        .Result.Replace("\r\n", args.NewLineText);
    }

    public static RdFormatResult FormatDocument(RdFantomasFormatDocumentArgs args)
    {
      var formatDocumentOptions = GetFormatDocumentOptions(args);
      var formatDocumentAsync = CodeFormatterType.InvokeMember("FormatDocumentAsync",
        BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, Type.DefaultBinder, null,
        formatDocumentOptions);
      var formatResult = FSharpAsync.StartAsTask((dynamic)formatDocumentAsync, null, null).Result;

      if (CurrentVersion < Version60)
        return new RdFormatResult(formatResult.Replace("\r\n", args.NewLineText), null);

      var formattedCode = formatResult.Code;
      var newCursorPosition = formatResult.Cursor == null
        ? null
        : new RdFcsPos(formatResult.Cursor.Value.Line - 1, formatResult.Cursor.Value.Column);
      return new RdFormatResult(formattedCode.Replace("\r\n", args.NewLineText), newCursorPosition);
    }

    private static object[] GetFormatDocumentOptions(RdFantomasFormatDocumentArgs args)
    {
      if (CurrentVersion >= Version60)
      {
        var cursorPosition = args.CursorPosition is { } pos
          ? MakePositionMethod.Invoke(null, new object[] { pos.Row + 1, pos.Column })
          : null;

        return new[]
        {
          args.FileName.EndsWith(".fsi"), // isSignature
          args.Source,
          ConvertToFormatConfig(args.FormatConfig),
          cursorPosition
        };
      }

      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Version)
        return new[]
        {
          args.FileName.EndsWith(".fsi"), // isSignature
          args.Source,
          ConvertToFormatConfig(args.FormatConfig)
        };

      return new[]
      {
        args.FileName,
        SourceOriginConstructor.Invoke(null, new object[] { args.Source }),
        ConvertToFormatConfig(args.FormatConfig),
        CreateFSharpParsingOptions.Invoke(GetParsingOptions(args.ParsingOptions).ToArray()),
        Checker
      };
    }

    private static IEnumerable<object> GetParsingOptions(RdFcsParsingOptions options)
    {
      yield return new[] { options.LastSourceFile };
      yield return ListModule.OfArray(options.ConditionalCompilationDefines);
      yield return DefaultDiagnosticOptions;
      if (CurrentVersion >= Version46) yield return options.LangVersion;
      yield return false; // isInteractive
      yield return options.LightSyntax ?? FSharpOption<bool>.None;
      yield return false; // compilingFsLib
      yield return options.IsExe;
    }

    private static object ConvertToFormatConfig(string[] riderFormatConfigValues)
    {
      var riderFormatConfigDict =
        FormatConfigFields
          .Zip(riderFormatConfigValues,
            (field, valueData) =>
              (field.Name, Value: valueData == ""
                ? field.Value
                : field.Value switch
                {
                  int => int.Parse(valueData),
                  bool => bool.Parse(valueData),
                  { } => ConvertEnumValue(valueData)
                }))
          .ToDictionary(x => x.Name, x => x.Value);

      var formatConfigValues =
        FormatConfigFields
          .Select(field => riderFormatConfigDict.TryGetValue(field.Name, out var value) ? value : field.Value)
          .ToArray();

      var formatConfig = FSharpValue.MakeRecord(FormatConfigType, formatConfigValues, null);

      if (CurrentVersion >= Version60) return formatConfig;
      return CurrentVersion >= FantomasProtocolConstants.Fantomas5Version
        ? CreateOptionMethod.Invoke(null, new[] { formatConfig }) //FSharpOption<FormatConfig>
        : formatConfig;
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
