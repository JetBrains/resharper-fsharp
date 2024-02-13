namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open System.Linq
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

type GenerateMissingRecordFieldsFix(recordExpr: IRecordExpr) =
    inherit FSharpQuickFixBase()

    new (error: FieldRequiresAssignmentError) =
        GenerateMissingRecordFieldsFix(error.Expr)

    new (error: EmptyRecordInvalidError) =
        GenerateMissingRecordFieldsFix(error.Expr)

    override x.Text = "Generate missing fields"

    override x.IsAvailable _ =
        if not (isValid recordExpr) then false else
        if isNull recordExpr.LeftBrace || isNull recordExpr.RightBrace then false else

        match recordExpr.Reference with
        | null -> failwith "Could not get reference"
        | reference ->

        let declaredElement = reference.Resolve().DeclaredElement
        isNotNull declaredElement

    override x.ExecutePsiTransaction(solution, _) =
        let typeElement = recordExpr.Reference.Resolve().DeclaredElement :?> ITypeElement
        Assertion.Assert(typeElement.IsRecord(), "Expecting record type")

        let generatedBindings = RecordExprUtil.generateBindings typeElement recordExpr

        Action<_>(fun textControl ->
            let templatesManager = LiveTemplatesManager.Instance
            let endCaretPosition = recordExpr.RightBrace.GetDocumentEndOffset()

            let hotspotInfos =
                generatedBindings.ToArray()
                |> Array.map (fun binding ->
                    let templateField = TemplateField(binding.ReferenceName.ShortName, SimpleHotspotExpression(null), 0)
                    HotspotInfo(templateField, binding.Expression.GetDocumentRange(), KeepExistingText = true))

            let hotspotSession =
                templatesManager.CreateHotspotSessionAtopExistingText(
                    solution, endCaretPosition, textControl,
                    LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfos)

            hotspotSession.ExecuteAndForget())
