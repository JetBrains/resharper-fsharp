namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.Rider.Backend.Features.AI.Chat.Summarization
open JetBrains.ReSharper.Psi.Files
open JetBrains.DocumentModel

[<Language(typeof<FSharpLanguage>)>]
type FSharpFileSummarizer() =
    let summarizeFSharpFile(fsharpFile: IFSharpFile): string =
        let parseAndCheckResults = fsharpFile.CheckerService.ParseAndCheckFile(fsharpFile.GetSourceFile(), "ML Summarizer", true)
        let sourceTextOption =
            match parseAndCheckResults with
            | None -> None
            | Some value -> value.CheckResults.GenerateSignature()

        let res =
            match sourceTextOption with
            | None -> ""
            | Some value -> value.ToString()
        res

    let processFile(file: IPsiSourceFile, language: FSharpLanguage): string =
        let psiFile = file.GetPrimaryPsiFile()
        match psiFile with
        | :? IFSharpFile as fsharpFile -> summarizeFSharpFile(fsharpFile)
        | _ -> ""

    interface IRiderFileSummarizer with
        member this.GetSummary(file: IPsiSourceFile) =
            file.GetPsiServices().Files.AssertAllDocumentAreCommitted()
            let fsharpLang = FSharpLanguage.Instance
            match fsharpLang with
            | null -> ""
            | language -> processFile(file, language)
