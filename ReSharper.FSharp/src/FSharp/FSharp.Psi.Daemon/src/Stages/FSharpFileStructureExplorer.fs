namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open System.Collections.Generic
open JetBrains.DocumentModel
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util

type DisableByNowarnCodeInspectionSection(warningId, range) =
    interface IDisableCodeInspectionSection with
        member this.WarningId = warningId
        member this.DisabledRange = range

        member this.StartTreeNode = null
        member this.EndTreeNode = null
        member this.RelatedSections = []
        member val Used = false with get, set
    

type FSharpFileStructure(fsFile: IFSharpFile) =
    inherit FileStructureBase(fsFile)

    let getCompilerIds (directive: IHashDirective) =
        directive.ArgsEnumerable
        |> Seq.collect (fun argToken -> 
            let fsString = argToken.As<FSharpString>()
            if isNull fsString then []: IReadOnlyCollection<_> else

            // todo: define common helper for extracting string token values
            let text = fsString.GetText()
            let text = text.Substring(1, text.Length - 2)

            FSharpCompilerWarningProcessor.parseCompilerIds text)

    do
        let inline makeNowarnRange (startRange: DocumentRange) offset = startRange.SetEndTo(&offset)
        let fileRange = fsFile.GetDocumentRange()
        let isFSharp10Supported = FSharpLanguageLevel.isFSharp100Supported fsFile
        let openedNowarns = Dictionary()

        for moduleDecl in fsFile.ModuleDeclarationsEnumerable do
            for moduleMember in moduleDecl.MembersEnumerable do
                match moduleMember with
                | :? INowarnDirective as nowarnDirective ->
                    let compilerIds = getCompilerIds nowarnDirective
                    for id in compilerIds do openedNowarns.TryAdd(id, fileRange) |> ignore

                | :? IWarnonDirective as warnonDirective when isFSharp10Supported ->
                    let compilerIds = getCompilerIds warnonDirective
                    for id in compilerIds do
                        match openedNowarns.TryGetValue(id) with
                        | true, nowarnRange ->
                            openedNowarns.Remove(id) |> ignore
                            let range = makeNowarnRange nowarnRange (warnonDirective.GetDocumentStartOffset())
                            base.WarningDisableRange.AddValue(id, DisableByNowarnCodeInspectionSection(id, range))
                        | _ -> ()
                
                | _ -> ()
        
        for KeyValue(id, nowarnRange) in openedNowarns do
            let range = makeNowarnRange nowarnRange fileRange.EndOffset
            base.WarningDisableRange.AddValue(id, DisableByNowarnCodeInspectionSection(id, range))

        for comment in fsFile.Descendants<FSharpComment>() do base.ProcessComment(comment, comment.CommentText)
        base.CloseAllRanges(fsFile)


[<FileStructureExplorer>]
type FSharpFileStructureExplorer() =
    interface IFileStructureExplorer with
        member this.ExploreFile(file, _, _) =
            let fsFile = file.As<IFSharpFile>()
            if isNull fsFile then null else

            PsiFileCachedDataUtil.InvalidateAllData(fsFile)
            FSharpFileStructure(fsFile)
