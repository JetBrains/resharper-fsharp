namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

open System.Drawing
open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.Application.UI.Controls.BulbMenu.Items
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes
open JetBrains.ReSharper.Feature.Services.Resources
open JetBrains.TextControl.DocumentMarkup
open JetBrains.Util

[<RequireQualifiedAccess>]
module FSharpHighlightingAttributeIds =
    let [<Literal>] GroupId = "F#"
    let [<Literal>] LayerSyntaxPlusOne = 3001 // HighlighterLayer.ADDITIONAL_SYNTAX + 1

    let [<Literal>] Keyword = "ReSharper F# Keyword"
    let [<Literal>] String = "ReSharper F# String"
    let [<Literal>] Number = "ReSharper F# Number"
    let [<Literal>] LineComment = "ReSharper F# Line Comment"
    let [<Literal>] BlockComment = "ReSharper F# Block Comment"

    let [<Literal>] EscapeCharacter1 = "ReSharper F# Escape Character 1"
    let [<Literal>] EscapeCharacter2 = "ReSharper F# Escape Character 2"

    let [<Literal>] PreprocessorKeyword = "ReSharper F# Preprocessor Keyword"
    let [<Literal>] PreprocessorInactiveBranch = "ReSharper F# Preprocessor Inactive Branch"

    let [<Literal>] Namespace = "ReSharper F# Namespace Identifier"
    let [<Literal>] Module = "ReSharper F# Module Identifier"

    let [<Literal>] Class = "ReSharper F# Class Identifier"
    let [<Literal>] StaticClass = "ReSharper F# Static Class Identifier"
    let [<Literal>] Interface = "ReSharper F# Interface Identifier"
    let [<Literal>] Delegate = "ReSharper F# Delegate Identifier"
    let [<Literal>] Struct = "ReSharper F# Struct Identifier"
    let [<Literal>] Enum = "ReSharper F# Enum Identifier"
    let [<Literal>] TypeParameter = "ReSharper F# Type Parameter Identifier"

    let [<Literal>] Union = "ReSharper F# Union Identifier"
    let [<Literal>] StructUnion = "ReSharper F# Struct Union Identifier" // todo: add setting
    let [<Literal>] UnionCase = "ReSharper F# Union Case Identifier"
    let [<Literal>] StructUnionCase = "ReSharper F# Struct Union Case Identifier" // todo: add setting

    let [<Literal>] Record = "ReSharper F# Record Identifier"
    let [<Literal>] StructRecord = "ReSharper F# Struct Record Identifier"

    let [<Literal>] ClassExtension = "ReSharper F# Class Extension Identifier" // todo: add setting
    let [<Literal>] StructExtension = "ReSharper F# Struct Extension Identifier" // todo: add setting
    let [<Literal>] InterfaceExtension = "ReSharper F# Struct Extension Identifier" // todo: add setting

    let [<Literal>] Value = "ReSharper F# Value Identifier"
    let [<Literal>] MutableValue = "ReSharper F# Mutable Value Identifier"
    let [<Literal>] Function = "ReSharper F# Function Identifier"
    let [<Literal>] MutableFunction = "ReSharper F# Mutable Function Identifier"

    let [<Literal>] Parameter = "ReSharper F# Parameter Identifier" // todo: add setting
    let [<Literal>] Literal = "ReSharper F# Literal Identifier"

    let [<Literal>] Operator = "ReSharper F# Operator Identifier"
    let [<Literal>] ActivePatternCase = "ReSharper F# Active Pattern Case Identifier"

    let [<Literal>] Field = "ReSharper F# Field Identifier"
    let [<Literal>] Property = "ReSharper F# Property Identifier"
    let [<Literal>] Event = "ReSharper F# Event Identifier"

    let [<Literal>] Method = "ReSharper F# Method Identifier"
    let [<Literal>] ExtensionMethod = "ReSharper F# Extension Method Identifier"
    let [<Literal>] ExtensionProperty = "ReSharper F# Extension Property Identifier"

    let [<Literal>] ComputationExpression = "ReSharper F# Computation Expression Identifier"
    let [<Literal>] UnitOfMeasure = "ReSharper F# Unit Of Measure Identifier"

    let [<Literal>] NonTailRecursion = "Non-tail recursion"
    let [<Literal>] PartialRecursion = "Partial recursion"
    let [<Literal>] Recursion = "Recursion"


type FSharpSettingsNamesProvider() =
    inherit PrefixBasedSettingsNamesProvider("ReSharper F#", "FSHARP")


type FSharpRecursionGutterMarkTypeBase(icon) =
    inherit IconGutterMarkType(icon)

    override this.Priority = BulbMenuAnchors.PermanentBackgroundItems
    override this.GetBulbMenuItems _ = EmptyList<BulbMenuItem>.Instance

type FSharpRecursionGutterMarkType() =
    inherit FSharpRecursionGutterMarkTypeBase(DaemonThemedIcons.Recursion.Id)

type FSharpPartialRecursionGutterMarkType() =
    inherit FSharpRecursionGutterMarkTypeBase(DaemonThemedIcons.RecursionInPartialCall.Id)

type FSharpNonTailRecursionGutterMarkType() =
    inherit FSharpRecursionGutterMarkTypeBase(DaemonThemedIcons.RecursionProblematic.Id)


// todo: replace explicit styles with fallback ids when highlighting registration refactoring is finished.

[<RegisterHighlighterGroup(
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      PresentableName = "F#",
      Priority = HighlighterGroupPriority.KEY_LANGUAGE_SETTINGS,
      RiderNamesProviderType = typeof<FSharpSettingsNamesProvider>);

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Keyword,
      FallbackAttributeId = DefaultLanguageAttributeIds.KEYWORD,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Syntax//Keyword",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "#0000E0", DarkForegroundColor = "#569CD6");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.String,
      FallbackAttributeId = DefaultLanguageAttributeIds.STRING,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Syntax//String",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "#A31515", DarkForegroundColor = "#D69D85");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Number,
      FallbackAttributeId = DefaultLanguageAttributeIds.NUMBER,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Syntax//Number",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "#000000", DarkForegroundColor = "#B5CEA8");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.LineComment,
      FallbackAttributeId = DefaultLanguageAttributeIds.LINE_COMMENT,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Comments//Line comment",
      Layer = HighlighterLayer.ADDITIONAL_SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "#007F00", DarkForegroundColor = "#57A64A");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.BlockComment,
      FallbackAttributeId = DefaultLanguageAttributeIds.BLOCK_COMMENT,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Comments//Block comment",
      Layer = HighlighterLayer.ADDITIONAL_SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "#007F00", DarkForegroundColor = "#57A64A");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.EscapeCharacter1,
      FallbackAttributeId = DefaultLanguageAttributeIds.STRING_ESCAPE_CHARACTER_1,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      Layer = enum FSharpHighlightingAttributeIds.LayerSyntaxPlusOne,
      EffectType = EffectType.TEXT, ForegroundColor = "#FF007F", DarkForegroundColor = "#E07A00");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.EscapeCharacter2,
      FallbackAttributeId = DefaultLanguageAttributeIds.STRING_ESCAPE_CHARACTER_2,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      Layer = enum FSharpHighlightingAttributeIds.LayerSyntaxPlusOne,
      EffectType = EffectType.TEXT, ForegroundColor = "#FF66B2", DarkForegroundColor = "#FF8D1C");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.PreprocessorKeyword,
      FallbackAttributeId = DefaultLanguageAttributeIds.PREPROCESSOR_KEYWORD,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Preprocessor//Keyword",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "#0000E0", DarkForegroundColor = "#569CD6");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.PreprocessorInactiveBranch,
      FallbackAttributeId = DefaultLanguageAttributeIds.PREPROCESSOR_INACTIVE_BRANCH,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Preprocessor//Inactive branch",
      Layer = HighlighterLayer.DEADCODE,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkGray");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Namespace,
      FallbackAttributeId = DefaultLanguageAttributeIds.NAMESPACE,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Modules and namespaces//Namespace",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Module,
      FallbackAttributeId = FSharpHighlightingAttributeIds.StaticClass,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Modules and namespaces//Module",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Class,
      FallbackAttributeId = DefaultLanguageAttributeIds.CLASS,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Class",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.StaticClass,
      FallbackAttributeId = FSharpHighlightingAttributeIds.Class,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Static class",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Interface,
      FallbackAttributeId = DefaultLanguageAttributeIds.INTERFACE,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Interface",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Delegate,
      FallbackAttributeId = DefaultLanguageAttributeIds.DELEGATE,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Delegate",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Struct,
      FallbackAttributeId = DefaultLanguageAttributeIds.STRUCT,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Struct",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Enum,
      FallbackAttributeId = DefaultLanguageAttributeIds.ENUM,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Enum",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.TypeParameter,
      FallbackAttributeId = DefaultLanguageAttributeIds.TYPE_PARAMETER,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Type parameter",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.UnitOfMeasure,
      FallbackAttributeId = FSharpHighlightingAttributeIds.TypeParameter,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Unit of measure",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Union,
      FallbackAttributeId = FSharpHighlightingAttributeIds.Enum,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Union",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.UnionCase,
      FallbackAttributeId = FSharpHighlightingAttributeIds.Class,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Union Case",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Record,
      FallbackAttributeId = FSharpHighlightingAttributeIds.Class,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Types//Record",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkBlue", DarkForegroundColor = "LightBlue");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Literal,
      FallbackAttributeId = DefaultLanguageAttributeIds.CONSTANT,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Values//Literal",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "Purple", DarkForegroundColor = "Violet",
      FontStyle = FontStyle.Bold);

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Event,
      FallbackAttributeId = DefaultLanguageAttributeIds.EVENT,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Members//Event",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "Magenta", DarkForegroundColor = "Plum");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Field,
      FallbackAttributeId = DefaultLanguageAttributeIds.FIELD,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Members//Field",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "Purple", DarkForegroundColor = "Violet");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Property,
      FallbackAttributeId = DefaultLanguageAttributeIds.PROPERTY,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Members//Property",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "Purple", DarkForegroundColor = "Violet");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Value,
      FallbackAttributeId = DefaultLanguageAttributeIds.LOCAL_VARIABLE,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Values//Value",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT);

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.MutableValue,
      FallbackAttributeId = DefaultLanguageAttributeIds.MUTABLE_LOCAL_VARIABLE,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Values//Mutable value",
      Layer = enum FSharpHighlightingAttributeIds.LayerSyntaxPlusOne,
      EffectType = EffectType.TEXT,
      FontStyle = FontStyle.Bold);

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Function,
      FallbackAttributeId = FSharpHighlightingAttributeIds.Value,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Values//Function",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT);

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.MutableFunction,
      FallbackAttributeId = FSharpHighlightingAttributeIds.MutableValue,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Values//Mutable function",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT,
      FontStyle = FontStyle.Bold);

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.ComputationExpression,
      FallbackAttributeId = FSharpHighlightingAttributeIds.Keyword,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Values//Computation expression",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "#0000E0", DarkForegroundColor = "#569CD6");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Method,
      FallbackAttributeId = DefaultLanguageAttributeIds.METHOD,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Members//Method",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkCyan:Maroon", DarkForegroundColor = "Cyan");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.Operator,
      FallbackAttributeId = DefaultLanguageAttributeIds.OVERLOADED_OPERATOR,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Values//Operator",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkCyan:Blue", DarkForegroundColor = "Cyan");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.ActivePatternCase,
      FallbackAttributeId = FSharpHighlightingAttributeIds.Method,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Values//Active pattern case",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkCyan:Blue", DarkForegroundColor = "Cyan");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.ExtensionMethod,
      FallbackAttributeId = FSharpHighlightingAttributeIds.Method,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Members//Extension method",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "DarkCyan:Maroon", DarkForegroundColor = "Cyan");

  RegisterHighlighter(
      FSharpHighlightingAttributeIds.ExtensionProperty,
      FallbackAttributeId = FSharpHighlightingAttributeIds.Property,
      GroupId = FSharpHighlightingAttributeIds.GroupId,
      RiderPresentableName = "Members//Extension property",
      Layer = HighlighterLayer.SYNTAX,
      EffectType = EffectType.TEXT, ForegroundColor = "Purple", DarkForegroundColor = "Violet");
  
  RegisterHighlighter(
    FSharpHighlightingAttributeIds.Recursion,
    Layer = HighlighterLayer.ADDITIONAL_SYNTAX,
    EffectType = EffectType.GUTTER_MARK,
    GutterMarkType = typeof<FSharpRecursionGutterMarkType>);

  RegisterHighlighter(
    FSharpHighlightingAttributeIds.NonTailRecursion,
    Layer = HighlighterLayer.ADDITIONAL_SYNTAX,
    EffectType = EffectType.GUTTER_MARK,
    GutterMarkType = typeof<FSharpNonTailRecursionGutterMarkType>);
  
  RegisterHighlighter(
    FSharpHighlightingAttributeIds.PartialRecursion,
    Layer = HighlighterLayer.ADDITIONAL_SYNTAX,
    EffectType = EffectType.GUTTER_MARK,
    GutterMarkType = typeof<FSharpPartialRecursionGutterMarkType>)>]
type FSharpHighlightingAttributeIds() = class end

module MissingAssemblyReferenceWorkaround =
    type T(p: FontStyle) =
        class end
