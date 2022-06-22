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
using NuGet.Versioning;
using FSharpType = Microsoft.FSharp.Reflection.FSharpType;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host
{
  // TODO: collect used Fantomas versions
  internal static class FantomasCodeFormatter
  {
    private static readonly (Assembly Assembly, NuGetVersion CurrentVersion) Fantomas =
      FantomasAssemblyResolver.LoadFantomasAssembly();

    private static readonly Assembly FantomasAssembly = Fantomas.Assembly;
    public static readonly NuGetVersion CurrentVersion = Fantomas.CurrentVersion;
    private static readonly string FantomasAssemblyName = FantomasAssembly.GetName().Name;

    private static readonly NuGetVersion Version45 = NuGetVersion.Parse("4.5");
    private static readonly NuGetVersion Version46 = NuGetVersion.Parse("4.6");

    private static Type GetCodeFormatter() =>
      FantomasAssembly
        .GetType($"{FantomasAssemblyName}.CodeFormatter")
        .NotNull("CodeFormatter must exist");

    private static object GetFSharpChecker()
    {
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Alpha3Version) return null;

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
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Alpha3Version) return null;

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
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Alpha3Version) return null;

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
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Alpha3Version) return null;

      var searchedType = CurrentVersion < Version46
        ? "FSharp.Compiler.SourceCodeServices.FSharpParsingOptions"
        : "FSharp.Compiler.CodeAnalysis.FSharpParsingOptions";

      var qualifiedName = Assembly.CreateQualifiedName("FSharp.Compiler.Service", searchedType);
      return Type.GetType(qualifiedName).NotNull($"{qualifiedName} must exist");
    }

    private static Type GetFormatConfigType() =>
      FantomasAssembly
        .GetType($"{FantomasAssemblyName}.FormatConfig")
        .NotNull("FormatConfig must exist")
        .GetNestedType("FormatConfig")
        .NotNull();

    private static readonly Type CodeFormatterType = GetCodeFormatter();
    private static readonly Type FSharpParsingOptionsType = GetFSharpParsingOptions();
    private static readonly Type FormatConfigType = GetFormatConfigType();

    private static readonly object DefaultDiagnosticOptions = GetDiagnosticOptions();
    private static readonly object DefaultFormatConfig = GetDefaultFormatConfig();
    private static readonly object Checker = GetFSharpChecker();

    private static readonly ConstructorInfo
      CreateFSharpParsingOptions = FSharpParsingOptionsType?.GetConstructors().Single();

    private static readonly MethodInfo FormatSelectionMethod = CodeFormatterType.GetMethod("FormatSelectionAsync");
    private static readonly MethodInfo FormatDocumentMethod = CodeFormatterType.GetMethod("FormatDocumentAsync");
    private static readonly MethodInfo MakeRangeMethod = CodeFormatterType.GetMethod("MakeRange");
    private static readonly MethodInfo SourceOriginConstructor = GetSourceOriginStringConstructor();

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
      // Fantomas 5 temporary does not support format selection
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Alpha3Version) throw new NotImplementedException();

      var rdRange = args.Range;
      var range =
        MakeRangeMethod.Invoke(null,
          new object[] { rdRange.FileName, rdRange.StartLine, rdRange.StartCol, rdRange.EndLine, rdRange.EndCol });

      return FSharpAsync.StartAsTask(
          FormatSelectionMethod.Invoke(null, new[]
          {
            args.FileName, range,
            SourceOriginConstructor.Invoke(null, new object[] { args.Source }),
            ConvertToFormatConfig(args.FormatConfig),
            CreateFSharpParsingOptions.Invoke(GetParsingOptions(args.ParsingOptions).ToArray()),
            Checker
          }) as FSharpAsync<string>, null, null)
        .Result.Replace("\r\n", args.NewLineText);
    }

    public static string FormatDocument(RdFantomasFormatDocumentArgs args) =>
      FSharpAsync.StartAsTask(
          FormatDocumentMethod.Invoke(null, GetFormatDocumentOptions(args).ToArray()) as FSharpAsync<string>,
          null, null)
        .Result.Replace("\r\n", args.NewLineText);

    private static IEnumerable<object> GetFormatDocumentOptions(RdFantomasFormatDocumentArgs args)
    {
      if (CurrentVersion >= FantomasProtocolConstants.Fantomas5Alpha3Version)
      {
        yield return args.FileName.EndsWith(".fsi"); // isSignature
        yield return args.Source;
        yield return ConvertToFormatConfig(args.FormatConfig);
      }
      else
      {
        yield return args.FileName;
        yield return SourceOriginConstructor.Invoke(null, new object[] { args.Source });
        yield return ConvertToFormatConfig(args.FormatConfig);
        yield return CreateFSharpParsingOptions.Invoke(GetParsingOptions(args.ParsingOptions).ToArray());
        yield return Checker;
      }
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

      return FSharpValue.MakeRecord(FormatConfigType, formatConfigValues, null);
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
