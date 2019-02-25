namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System.Collections.Generic
open JetBrains.Application
open JetBrains.Lifetimes
open JetBrains.ReSharper.Daemon.UsageChecking
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Actions
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

type UnusedOpensStageProcess(fsFile: IFSharpFile, checkResults, daemonProcess: IDaemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let document = fsFile.GetSourceFile().Document
    let lines = Dictionary<int, string>()

    let getLine line =
        let line = line - 1
        use cookie = ReadLockCookie.Create()
        lines.GetOrCreateValue(line, fun () -> document.GetLineText(docLine line))

    override x.Execute(committer) =
        let highlightings = List()
        let interruptChecker = daemonProcess.CreateInterruptChecker()
        for range in UnusedOpens.getUnusedOpens(checkResults, getLine).RunAsTask(interruptChecker) do
            x.SeldomInterruptChecker.CheckForInterrupt()
            match fsFile.FindTokenAt(document.GetTreeStartOffset(range)) with
            | null -> ()
            | token ->

            match token.GetContainingNode<IOpenStatement>() with
            | null -> ()
            | openDirective ->

            let range = openDirective.GetHighlightingRange()
            highlightings.Add(HighlightingInfo(range, UnusedOpenWarningHighlighting(openDirective)))
        committer.Invoke(DaemonStageResult(highlightings))


[<DaemonStage(StagesBefore = [| typeof<HighlightIdentifiersStage> |], StagesAfter = [| typeof<CollectUsagesStage> |])>]
type UnusedOpensStage(daemonProcess, errors) =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile: IFSharpFile, _, daemonProcess: IDaemonProcess) =
        match fsFile.GetParseAndCheckResults(false) with
        | Some results -> UnusedOpensStageProcess(fsFile, results.CheckResults, daemonProcess) :> _
        | _ -> null


// todo: use ReSharper ErrorsGen
[<StaticSeverityHighlighting(Severity.WARNING, HighlightingGroupIds.IdentifierHighlightingsGroup,
                             AttributeId = HighlightingAttributeIds.DEADCODE_ATTRIBUTE,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type UnusedOpenWarningHighlighting(openStatement: IOpenStatement) =
    let [<Literal>] message = "Open directive is not required by the code and can be safely removed"

    member x.OpenStatement = openStatement
    static member val HighlightingId = "RedundantOpen"

    interface IHighlighting with
        member x.ToolTip = message
        member x.ErrorStripeToolTip = message
        member x.CalculateRange() = openStatement.GetHighlightingRange()
        member x.IsValid() = isNull openStatement || openStatement.IsValid()


type RemoveUnusedOpensFix(warning: UnusedOpenWarningHighlighting) =
    inherit QuickFixBase()

    let [<Literal>] actionText = "Remove unused opens"

    override x.Text = actionText
    override x.IsAvailable(_) = warning.OpenStatement.IsValid()
    override x.ExecutePsiTransaction(_,_) = null

    interface IHighlightingsSetScopedAction with
        member x.ScopedText = actionText
        member x.FileCollectorInfo = FileCollectorInfo.WithoutCaretFix

        member x.ExecuteAction(hls, _, _) =
            use writeLock = WriteLockCookie.Create(true)
            for hl in hls do
                match hl.Highlighting with
                | :? UnusedOpenWarningHighlighting as hl ->
                    let openStatement = hl.OpenStatement

                    let mutable first = openStatement :> ITreeNode
                    while isNotNull first.PrevSibling &&
                            first.PrevSibling.GetTokenType() == FSharpTokenType.WHITESPACE do
                        first <- first.PrevSibling

                    let mutable last = openStatement :> ITreeNode
                    while isNotNull last.NextSibling &&
                            let tokenType = last.NextSibling.GetTokenType()
                            tokenType == FSharpTokenType.WHITESPACE || tokenType == FSharpTokenType.SEMICOLON do
                        last <- last.NextSibling

                    if isNotNull last.NextSibling &&
                            last.NextSibling.GetTokenType() == FSharpTokenType.NEW_LINE then
                        last <- last.NextSibling

                    LowLevelModificationUtil.DeleteChildRange(first, last)
                | _ -> ()
            null


// todo: use ReSharper ErrorsGen
[<ShellComponent>]
type UnusedOpensQuickFixRegistrarComponent(table: IQuickFixes) =
    do
         table.RegisterQuickFix<UnusedOpenWarningHighlighting>(Lifetime.Eternal, (fun hl -> RemoveUnusedOpensFix(hl) :> _), typeof<RemoveUnusedOpensFix>)