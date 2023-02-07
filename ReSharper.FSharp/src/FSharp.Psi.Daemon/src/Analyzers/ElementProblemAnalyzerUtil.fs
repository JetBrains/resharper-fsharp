﻿[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers.Util

open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util
open JetBrains.Util.dataStructures

let allowHighPrecedenceAppParensKey = Key<Boxed<bool>>("AllowHighPrecedenceAppParens")

type ElementProblemAnalyzerData with
    member this.LanguageLevel =
        let languageLevel = this.GetData(FSharpLanguageLevel.key)
        if isNotNull languageLevel then languageLevel.Value else

        let languageLevel = Boxed(FSharpLanguageLevel.ofTreeNode this.File)
        this.PutData(FSharpLanguageLevel.key, languageLevel)

        languageLevel.Value

    member this.IsFSharp47Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp47

    member this.IsFSharp50Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp50

    member this.IsFSharp60Supported =
        this.LanguageLevel >= FSharpLanguageLevel.FSharp60

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
