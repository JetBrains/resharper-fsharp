module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FSharpHighlightingAttributeIds

open JetBrains.TextControl.DocumentMarkup

let [<Literal>] Keyword = "ReSharper F# Keyword"
let [<Literal>] String = "ReSharper F# String"
let [<Literal>] Number = "ReSharper F# Number"
let [<Literal>] LineComment = "ReSharper F# Line Comment"
let [<Literal>] BlockComment = "ReSharper F# Block Comment"

let [<Literal>] EscapeCharacter1 = "ReSharper F# Escape Character 1"
let [<Literal>] EscapeCharacter2 = "ReSharper F# Escape Character 2"

let [<Literal>] PreprocessorKeyword = "ReSharper F# Preprocessor Keyword"
let [<Literal>] PreprocessorInactiveBranch = "ReSharper F# Preprocessor Inactive Branch"

let [<Literal>] Module = "ReSharper F# Module Identifier"
let [<Literal>] Namespace = "ReSharper F# Namespace Identifier"

let [<Literal>] Class = "ReSharper F# Class Identifier"
let [<Literal>] StaticClass = "ReSharper F# Static Class Identifier"
let [<Literal>] Interface = "ReSharper F# Interface Identifier"
let [<Literal>] Delegate = "ReSharper F# Delegate Identifier"
let [<Literal>] Struct = "ReSharper F# Struct Identifier"
let [<Literal>] Enum = "ReSharper F# Enum Identifier"
let [<Literal>] TypeParameter = "ReSharper F# Type Parameter Identifier"

let [<Literal>] Union = "ReSharper F# Union Identifier"
let [<Literal>] StructUnion = "ReSharper F# Struct Union Identifier"
let [<Literal>] Record = "ReSharper F# Record Identifier"
let [<Literal>] StructRecord = "ReSharper F# Struct Record Identifier"

let [<Literal>] ClassExtension = "ReSharper F# Class Extension Identifier"
let [<Literal>] StructExtension = "ReSharper F# Struct Extension Identifier"

let [<Literal>] Value = "ReSharper F# Value Identifier"
let [<Literal>] MutableValue = "ReSharper F# Value Identifier"
let [<Literal>] Parameter = "ReSharper F# Parameter Identifier"
let [<Literal>] Literal = "ReSharper F# Literal Identifier"

let [<Literal>] Operator = "ReSharper F# Operator Identifier"
let [<Literal>] ActivePatternCase = "ReSharper F# Active Pattern Case Identifier"

let [<Literal>] Field = "ReSharper F# Field Identifier"
let [<Literal>] Property = "ReSharper F# Property Identifier"
let [<Literal>] Event = "ReSharper F# Event Identifier"

let [<Literal>] Method = "ReSharper F# Method Identifier"
let [<Literal>] ExtensionMethod = "ReSharper F# Extension Method Identifier"
let [<Literal>] ExtensionProperty = "ReSharper F# Extension Property Identifier"


type FSharpSettingsNamesProvider() =
    inherit PrefixBasedSettingsNamesProvider("ReSharper F#", "FSHARP")

//[<assembly:
//    RegisterHighlighterGroup(
//        GroupId = "F#",
//        PresentableName = "F#",
//        Priority = HighlighterGroupPriority.KEY_LANGUAGE_SETTINGS,
//        RiderNamesProviderType = typeof<FSharpSettingsNamesProvider>)>]
//do
//    ()