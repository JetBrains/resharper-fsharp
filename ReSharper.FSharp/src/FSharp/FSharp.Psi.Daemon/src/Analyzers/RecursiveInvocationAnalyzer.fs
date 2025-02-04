namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

module private FSharpRecursionGutterHighlighting =
    let [<Literal>] NonTailRecursionMessage = "Recursion in non-tail position"
    let [<Literal>] RecursiveNameUsageMessage = "Recursive name usage"
    let [<Literal>] PartialRecursionMessage = "Recursive partial application"
    let [<Literal>] RecursionMessage = "Recursion in tail position"

[<StaticSeverityHighlighting(
    Severity.INFO, typeof<HighlightingGroupIds.GutterMarks>,
    OverlapResolve = OverlapResolveKind.NONE,
    ShowToolTipInStatusBar = false)>]
type FSharpRecursionGutterHighlighting private (treeNode: ITreeNode, message, highlightingId) =
    member this.ToolTip = message

    interface IHighlighting with
        member this.CalculateRange() = treeNode.GetDocumentRange()
        member this.IsValid() = treeNode.IsValid()
        member this.ToolTip = this.ToolTip
        member this.ErrorStripeToolTip = this.ToolTip

    interface ICustomAttributeIdHighlighting with
        member this.AttributeId = highlightingId

    static member CreateNonTailRecursion(treeNode) =
        FSharpRecursionGutterHighlighting(
            treeNode,
            FSharpRecursionGutterHighlighting.NonTailRecursionMessage,
            FSharpHighlightingAttributeIds.NonTailRecursion
        )

    static member CreatePartialRecursion(treeNode) =
        FSharpRecursionGutterHighlighting(
            treeNode,
            FSharpRecursionGutterHighlighting.PartialRecursionMessage,
            FSharpHighlightingAttributeIds.PartialRecursion
        )

    static member CreateRecursiveNameReference(treeNode) =
        FSharpRecursionGutterHighlighting(
            treeNode,
            FSharpRecursionGutterHighlighting.RecursiveNameUsageMessage,
            FSharpHighlightingAttributeIds.PartialRecursion
        )

    static member CreateRecursion(treeNode) =
        FSharpRecursionGutterHighlighting(
            treeNode,
            FSharpRecursionGutterHighlighting.RecursionMessage,
            FSharpHighlightingAttributeIds.Recursion
        )

[<Struct>]
type RecursiveInvocationAnalyzerContext =
    { Consumer: IHighlightingConsumer
      DeclaredElement: IDeclaredElement
      Name: string }

    static member Create(element, consumer, name) =
        { Consumer = consumer
          DeclaredElement = element
          Name = name }

[<ElementProblemAnalyzer([| typeof<IParameterOwnerMemberDeclaration> |],
                         HighlightingTypes = [| typeof<FSharpRecursionGutterHighlighting> |])>]
type RecursiveInvocationAnalyzer() =
    inherit ElementProblemAnalyzer<IParameterOwnerMemberDeclaration>()

    let processor =
        { new IRecursiveElementProcessor<RecursiveInvocationAnalyzerContext> with
            member this.InteriorShouldBeProcessed(element, context) =
                match element with
                | :? IBinding as binding -> binding.ParametersDeclarationsEnumerable |> Seq.isEmpty
                | :? IMemberDeclaration -> false
                | _ -> true

            member this.ProcessBeforeInterior(element, context) =
                let refExpr = element.As<IReferenceExpr>()
                if isNull refExpr || refExpr.ShortName <> context.Name then () else

                let reference = refExpr.Reference
                let mfv = reference.GetFcsSymbol().As<FSharpMemberOrFunctionOrValue>()
                if isNull mfv then () else

                let resolveResult = reference.Resolve()
                if resolveResult.DeclaredElement <> context.DeclaredElement then () else

                let highlighting =
                    if not (FSharpResolveUtil.isInvocation mfv refExpr) then
                        if getPrefixAppExprArgs refExpr |> Seq.isEmpty then
                            FSharpRecursionGutterHighlighting.CreateRecursiveNameReference(refExpr)
                        else
                            FSharpRecursionGutterHighlighting.CreatePartialRecursion(refExpr)
                    else
                        let appExpr = getOutermostPrefixAppExpr refExpr
                        if FSharpResolveUtil.isInTailRecursivePosition context.DeclaredElement appExpr then
                            FSharpRecursionGutterHighlighting.CreateRecursion(refExpr) else

                        FSharpRecursionGutterHighlighting.CreateNonTailRecursion(refExpr)

                context.Consumer.AddHighlighting(highlighting)

            member this.IsProcessingFinished(context) = false
            member this.ProcessAfterInterior(element, context) = () }

    override this.Run(decl, _, consumer) =
        if decl.ParametersDeclarationsEnumerable |> Seq.isEmpty then () else

        let element = decl.DeclaredElement
        if isNull element then () else

        let name = element.GetSourceName()
        let context = RecursiveInvocationAnalyzerContext.Create(element, consumer, name)

        decl.ProcessDescendants(processor, context)

    interface IConditionalElementProblemAnalyzer with
        member this.ShouldRun(_, data) =
            data.GetDaemonProcessKind() = DaemonProcessKind.VISIBLE_DOCUMENT
