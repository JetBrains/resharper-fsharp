namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Daemon.SyntaxHighlighting
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

type DeadCodeHighlightStageProcess(fsFile: IFSharpFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let [<Literal>] highlightingId = DefaultLanguageAttributeIds.PREPROCESSOR_INACTIVE_BRANCH
    let [<Literal>] tooltip = "Inactive Preprocessor Branch"

    override x.Execute(committer) =
        let highlightings = LocalList<HighlightingInfo>()
        for token in fsFile.Tokens() do
            match token with
            | :? FSharpDeadCodeToken ->
                let range = token.GetNavigationRange()
                let highlighting = ReSharperSyntaxHighlighting(highlightingId, tooltip, range)
                highlightings.Add(HighlightingInfo(range, highlighting))

            | _ -> ()
            x.SeldomInterruptChecker.CheckForInterrupt()
        committer.Invoke(DaemonStageResult(highlightings.ReadOnlyList()))

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type DeadCodeHighlightStage() =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile, _, daemonProcess) =
        DeadCodeHighlightStageProcess(fsFile, daemonProcess) :> _
