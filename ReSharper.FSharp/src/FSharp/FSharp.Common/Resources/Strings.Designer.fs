﻿// <auto-generated/>
namespace JetBrains.ReSharper.Plugins.FSharp.Resources

open System
open JetBrains.Application.I18n
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.Util
open JetBrains.Util.Logging
open JetBrains.Application.I18n.Plurals

[<global.System.Diagnostics.DebuggerNonUserCode>]
[<global.System.Runtime.CompilerServices.CompilerGenerated>]
type public Strings() =
    static let logger = Logger.GetLogger("JetBrains.ReSharper.Plugins.FSharp.Resources.Strings")

    static let mutable resourceManager = null

    static do
        CultureContextComponent.Instance.Change.Advise(Lifetime.Eternal, fun (args:PropertyChangedEventArgs<CultureContextComponent>) ->
            let instance = if args.HasNew then args.New else null
            if instance <> null then
                resourceManager <- Lazy<JetResourceManager>(fun _ ->
                    instance.CreateResourceManager("JetBrains.ReSharper.Plugins.FSharp.Resources.Strings",
                                typeof<Strings>.Assembly))
            else
                resourceManager <- null)

    [<global.System.ComponentModel.EditorBrowsable(global.System.ComponentModel.EditorBrowsableState.Advanced)>]
    static member ResourceManager: JetResourceManager =
        match resourceManager with
            | null -> ErrorJetResourceManager.Instance
            | _ -> resourceManager.Value

    static member Choice(format: string, [<ParamArray>] args: Object array): string =
        match Strings.ResourceManager.ChoiceFormatter with
            | null -> "???"
            | formatter -> String.Format(formatter, format, args)

    static member FSharpTypeHints_TopLevelMembers_Description = Strings.ResourceManager.GetString("FSharpTypeHints_TopLevelMembers_Description")
    static member FSharpTypeHints_LocalBindings_Description = Strings.ResourceManager.GetString("FSharpTypeHints_LocalBindings_Description")
    static member FSharpTypeHints_ShowPipeReturnTypes_Description = Strings.ResourceManager.GetString("FSharpTypeHints_ShowPipeReturnTypes_Description")
    static member FSharpTypeHints_HideSameLinePipe_Description = Strings.ResourceManager.GetString("FSharpTypeHints_HideSameLinePipe_Description")