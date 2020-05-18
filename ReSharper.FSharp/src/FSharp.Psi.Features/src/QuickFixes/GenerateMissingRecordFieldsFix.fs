namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open System.Collections.Generic
open System.Linq
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util.PsiUtil
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil

type GenerateMissingRecordFieldsFix(recordExpr: IRecordExpr) =
    inherit QuickFixBase()

    let addSemicolon (binding: IRecordExprBinding) =
        if isNull binding.Semicolon then
            match binding.Expression with
            | null -> failwith "Could not get expr"
            | expr -> ModificationUtil.AddChildAfter(expr, FSharpTokenType.SEMICOLON.CreateLeafElement()) |> ignore

    let getReturnRecordFieldNames (recordExpr : IRecordExpr) : Option<seq<string>> =
        let maybeBinding = (getReturnExpressionOwner recordExpr).As<IFunctionDeclaration>() |> Option.ofObj
        maybeBinding
        |> Option.map(fun funcDeclaration ->
            match funcDeclaration.DeclaredElement.As<IFSharpMember>() with
            | null -> failwith "Expected IFSharpMember"
            | fsharpMember ->
            fsharpMember.Mfv.ReturnParameter.Type)
        |> Option.bind(fun element ->
            if element.HasTypeDefinition && element.TypeDefinition.IsFSharpRecord then
                element.TypeDefinition.FSharpFields |> Seq.map(fun x -> x.Name) |> Some
            else
                None)
    
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
        
        let declaredElement = match reference.Resolve() with | null -> null | symbolRef -> symbolRef.DeclaredElement
        isNotNull declaredElement || (getReturnRecordFieldNames recordExpr).IsSome

    override x.ExecutePsiTransaction(solution, _) =
        let typeElement =
            match recordExpr.Reference.Resolve() with
            | null -> null
            | symbolRef -> symbolRef.DeclaredElement :?> ITypeElement
        
        let fieldNames =     
            if isNull typeElement then
                // If the ITypeElement isn't resolvable (as is the case with functions), then get the field names from the FCS.
                getReturnRecordFieldNames recordExpr
                |> Option.defaultWith(fun _ -> failwith "Expected return type as per IsAvailable")
            else
                Assertion.Assert(typeElement.IsRecord(), "Expecting record type")
                typeElement.GetRecordFieldNames() :> seq<string>

        let existingBindings = recordExpr.ExprBindings

        let fieldsToAdd = HashSet(fieldNames)
        for binding in existingBindings do
            fieldsToAdd.Remove(binding.ReferenceName.ShortName) |> ignore

        let fsFile = recordExpr.FSharpFile
        let lineEnding = fsFile.GetLineEnding()
        let elementFactory = fsFile.CreateElementFactory()

        use writeCookie = WriteLockCookie.Create(recordExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let mutable anchor: ITreeNode =
            match existingBindings.LastOrDefault() with
            | null ->
                let lBrace = recordExpr.LeftBrace
                let rBrace = recordExpr.RightBrace

                if lBrace.NextSibling == rBrace then
                    // Empty braces: {}
                    ModificationUtil.AddChildAfter(lBrace, Whitespace()) :> _
                else
                    match lBrace.NextSibling with
                    | Whitespace node when node.GetTextLength() = 1 -> node
                    | nextSibling ->

                    // Some space inside braces: {   }
                    let existingSpace = TreeRange(nextSibling, rBrace.PrevSibling)
                    ModificationUtil.ReplaceChildRange(existingSpace, TreeRange(Whitespace())).First

            | binding -> binding :> _

        let isSingleLine = recordExpr.IsSingleLine

        let generateSingleLine =
            existingBindings.Count > 1 && fieldNames |> Seq.length <= 4 && isSingleLine

        if isSingleLine && not generateSingleLine && existingBindings.Count > 0 then
            ToMultilineRecord.Execute(recordExpr)

        if generateSingleLine && not existingBindings.IsEmpty then
            addSemicolon (existingBindings.Last())

        let generatedBindings = List<IRecordExprBinding>()

        let indent =
            match anchor with
            | :? IRecordExprBinding -> anchor.Indent
            | _ -> anchor.Indent + 1

        for name in fieldsToAdd do
            if anchor :? IRecordExprBinding then
                if generateSingleLine then
                    anchor <- ModificationUtil.AddChildAfter(anchor, Whitespace())
                else
                    anchor <- ModificationUtil.AddChildAfter(anchor, NewLine(lineEnding))
                    anchor <- ModificationUtil.AddChildAfter(anchor, Whitespace(indent))

            let binding = elementFactory.CreateRecordExprBinding(name, generateSingleLine)
            anchor <- ModificationUtil.AddChildAfter(anchor, binding)
            generatedBindings.Add(anchor :?> _)

        let lastBinding = generatedBindings.Last()

        if generateSingleLine then
            ModificationUtil.DeleteChild(lastBinding.Semicolon)

            for binding in existingBindings do
                if binding.NextSibling :? IRecordExprBinding then
                    ModificationUtil.AddChildAfter(binding, Whitespace()) |> ignore

            if recordExpr.LeftBrace.NextSibling :? IRecordExprBinding then
                ModificationUtil.AddChildAfter(recordExpr.LeftBrace, Whitespace()) |> ignore

        if lastBinding.NextSibling.GetTokenType() == FSharpTokenType.RBRACE then
            ModificationUtil.AddChildAfter(lastBinding, Whitespace()) |> ignore

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

            hotspotSession.Execute() |> ignore)
