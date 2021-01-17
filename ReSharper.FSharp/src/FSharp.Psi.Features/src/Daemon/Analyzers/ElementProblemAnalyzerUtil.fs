[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers.Util

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.Util.dataStructures

type ElementProblemAnalyzerData with
    member this.LanguageLevel =
        let languageLevel = this.GetData(FSharpLanguageLevel.key)
        if isNotNull languageLevel then languageLevel.Value else

        let languageLevel = Boxed<FSharpLanguageLevel>(FSharpLanguageLevel.ofTreeNode this.File)
        this.PutData(FSharpLanguageLevel.key, languageLevel)

        languageLevel.Value

    member this.IsFSharp47Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp47

    member this.IsFSharp50Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp50
