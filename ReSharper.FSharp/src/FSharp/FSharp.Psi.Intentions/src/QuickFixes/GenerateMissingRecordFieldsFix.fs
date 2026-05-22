namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Linq
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.BulbActions
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

    override x.ExecutePsiTransaction(_, _) =
        let typeElement = recordExpr.Reference.Resolve().DeclaredElement :?> ITypeElement
        Assertion.Assert(typeElement.IsFSharpRecord(), "Expecting record type")

        let generatedBindings = RecordExprUtil.generateBindings typeElement recordExpr

        let hotspotInfos =
            generatedBindings.ToArray()
            |> Array.map (fun binding ->
                let templateField = TemplateField(binding.ReferenceName.ShortName, SimpleHotspotExpression(null), 0)
                HotspotInfo(templateField, binding.Expression.GetDocumentRange(), KeepExistingText = true))

        let endCaretPosition = recordExpr.RightBrace.GetDocumentEndOffset()
        BulbActionCommands.ShowHotspotSession(hotspotInfos, endCaretPosition)
