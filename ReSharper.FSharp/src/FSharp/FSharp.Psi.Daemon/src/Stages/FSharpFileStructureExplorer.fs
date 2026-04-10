namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util    

type FSharpFileStructure(fsFile: IFSharpFile) =
    inherit FileStructureBase(fsFile)

    do
        for comment in fsFile.Descendants<FSharpComment>() do
            base.ProcessComment(comment, comment.CommentText)

        base.CloseAllRanges(fsFile)


[<FileStructureExplorer>]
type FSharpFileStructureExplorer() =
    interface IFileStructureExplorer with
        member this.ExploreFile(file, _, _) =
            let fsFile = file.As<IFSharpFile>()
            if isNull fsFile then null else

            PsiFileCachedDataUtil.InvalidateAllData(fsFile)
            FSharpFileStructure(fsFile)
