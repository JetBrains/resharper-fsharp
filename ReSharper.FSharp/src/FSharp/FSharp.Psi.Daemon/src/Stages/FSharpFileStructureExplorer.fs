namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.Daemon
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

    do
        for moduleDecl in fsFile.ModuleDeclarationsEnumerable do
            for moduleMember in moduleDecl.MembersEnumerable do
                let nowarnDirective = moduleMember.As<INowarnDirective>()
                if isNull nowarnDirective then () else

                for argToken in nowarnDirective.ArgsEnumerable do
                    let fsString = argToken.As<FSharpString>()
                    if isNull fsString then () else

                    // todo: define common helper for extracting string token values
                    let text = fsString.GetText()
                    let text = text.Substring(1, text.Length - 2)

                    let range = fsFile.GetDocumentRange()
                    for id in FSharpCompilerWarningProcessor.parseCompilerIds text do
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
