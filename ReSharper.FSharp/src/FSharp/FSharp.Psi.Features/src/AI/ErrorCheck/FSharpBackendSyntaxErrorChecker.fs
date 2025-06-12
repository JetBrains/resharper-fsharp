module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AI.FSharpBackendSyntaxErrorChecker

open System.Collections.Generic
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpRangeUtil
open JetBrains.ReSharper.Plugins.FSharp.Util.CommonUtil
open FSharp.Compiler.Diagnostics
open JetBrains.Application.Parts
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.AI
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing
open JetBrains.Rider.Model
open JetBrains.Util

// TODO: probably might be moved to AI zone once API stabilizes
[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type FSharpBackendSyntaxErrorChecker(checkerService: FcsCheckerService, documentFactory: IInMemoryDocumentFactory) =    
    let convertDiagnostic(document: IDocument) (diagnostic: FSharpDiagnostic) :BackendError =
                let startOffset = getDocumentOffset document (docCoords diagnostic.StartLine diagnostic.StartColumn)
                BackendError(startOffset.Offset, diagnostic.Message)

    interface ICustomSyntaxErrorChecker with
        member this.IsAvailable(language) = language :? FSharpLanguage
        
        member this.CheckSyntaxErrors(lifetime, psiModule, modifiedContent)=
            let document = documentFactory.CreateSimpleDocumentFromText(modifiedContent, "F# Sandbox File for Syntax Check")
            let parsingResult = checkerService.ParseFile(FSharpParser.SandBoxPath, document, sandboxParsingOptions, true)
            match parsingResult with
            | None -> EmptyList<BackendError>.InstanceList
            | Some result ->
                result.Diagnostics
                |> Array.map (convertDiagnostic document)
                |> fun errors -> List<BackendError>(errors)
