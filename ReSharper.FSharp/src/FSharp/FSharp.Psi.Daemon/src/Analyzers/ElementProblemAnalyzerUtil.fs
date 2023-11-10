[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers.Util

open System
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util
open JetBrains.Util.dataStructures

let allowHighPrecedenceAppParensKey = Key<Boxed<bool>>("AllowHighPrecedenceAppParens")
let fsCoreVersionKey = Key<Version>("FSharpCoreVersionKey")

type ElementProblemAnalyzerData with
    member this.LanguageLevel =
        let languageLevel = 
            this.GetOrCreateDataUnderLock(FSharpLanguageLevel.key, fun _ ->
                Boxed(FSharpLanguageLevel.ofTreeNode this.File)
            )
        languageLevel.Value

    member this.FSharpCoreVersion =
        this.GetOrCreateDataUnderLock(fsCoreVersionKey, fun _ ->
            let moduleReferences =
                let psiModule = this.File.GetPsiModule()
                match psiModule with
                | :? FSharpScriptPsiModule ->
                    let psiModules = psiModule.GetPsiServices().Modules
                    psiModules.GetModuleReferences(psiModule) |> Seq.map (fun reference -> reference.Module)
                | _ ->
                    psiModule |> getReferencedModules

            moduleReferences
            |> Seq.tryPick (fun psiModule ->
                match psiModule with
                | :? IAssemblyPsiModule as assemblyPsiModule when assemblyPsiModule.Name = "FSharp.Core" ->
                    Some assemblyPsiModule.Assembly.AssemblyName.Version
                | _ -> None
            )
            |> Option.defaultValue (Version())
        )

    member this.IsFSharp47Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp47

    member this.IsFSharp50Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp50

    member this.IsFSharp60Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp60

    member this.IsFSharp70Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp70

    member this.IsFSharp80Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp80

    member this.IsFSharpExperimentalSupported =
        this.LanguageLevel >= FSharpLanguageLevel.Preview

    member this.ParseAndCheckResults =
        let results = this.GetData(parseAndCheckResultsKey)
        if Option.isSome results then results else

        let fsFile = this.File.As<IFSharpFile>().NotNull()
        let results = fsFile.GetParseAndCheckResults(false, "ElementProblemAnalyzerData")
        this.PutData(parseAndCheckResultsKey, results)

        results

    member this.AllowHighPrecedenceAppParens =
        let value = this.GetData(allowHighPrecedenceAppParensKey)
        if isNotNull value then value.Value else

        let value = allowHighPrecedenceAppParens this.File
        this.PutData(allowHighPrecedenceAppParensKey, Boxed(value))

        value
