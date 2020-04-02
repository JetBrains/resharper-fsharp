namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open JetBrains.Application.Settings
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open FSharp.Compiler.SourceCodeServices

type InferredTypeHintHighlightingProcess(logger: ILogger, fsFile, settings: IContextBoundSettingsStore, daemonProcess: IDaemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    override x.Execute(committer) =
        let consumer = new FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        fsFile.ProcessThisAndDescendants(Processor(x, consumer))
        committer.Invoke(DaemonStageResult(consumer.Highlightings))

    override x.VisitLocalReferencePat(localRefPat, consumer) =
        match localRefPat.IgnoreParentParens().Parent with
        | :? ITypedPat -> ()
        | :? ILocalBinding as localBinding when localBinding.ReturnTypeInfo <> null -> ()
        | _ ->

        match localRefPat.Identifier.As<FSharpIdentifierToken>() with
        | null -> ()
        | token ->

        match box (localRefPat.GetFSharpSymbolUse()) with
        | null -> ()
        | symbolUse ->

        let symbolUse = symbolUse :?> FSharpSymbolUse
        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            let typeNameStr =
                symbolUse.DisplayContext.WithShortTypeNames(true)
                |> mfv.FullType.Format

            let range = localRefPat.GetNavigationRange().EndOffsetRange()

            // todo: TypeNameHintHighlighting can be used when RIDER-39605 is resolved
            consumer.AddHighlighting(TypeHintHighlighting(typeNameStr, range))
        | _ -> ()

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type InferredTypeHintStage(logger: ILogger) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT &&
        base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        if not (settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowInferredTypes)) then null else
        InferredTypeHintHighlightingProcess(logger, fsFile, settings, daemonProcess) :> _
