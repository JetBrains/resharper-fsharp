namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open System.Collections.Generic
open System.Linq
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type GenerateMissingRecordFieldsFix(recordExpr: IRecordExpr) =
    inherit FSharpQuickFixBase()

    let maxBindingsAmountOnSingleLine = 4

    let addSemicolon (binding: IRecordFieldBinding) =
        if isNull binding.Semicolon then
            match binding.Expression with
            | null -> failwith "Could not get expr"
            | expr -> ModificationUtil.AddChildAfter(expr, FSharpTokenType.SEMICOLON.CreateLeafElement()) |> ignore

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

        let fieldNames = typeElement.GetRecordFieldNames()
        let existingBindings = recordExpr.FieldBindings

        let fieldsToAdd = HashSet(fieldNames)
        for binding in existingBindings do
            fieldsToAdd.Remove(binding.ReferenceName.ShortName) |> ignore

        let fsFile = recordExpr.FSharpFile
        let elementFactory = fsFile.CreateElementFactory()

        use writeCookie = WriteLockCookie.Create(recordExpr.IsPhysical())
        use enableFormatter = FSharpExperimentalFeatureCookie.Create(ExperimentalFeature.Formatter)

        let isSingleLine = recordExpr.IsSingleLine

        let generateSingleLine =
            existingBindings.Count > 1 && fieldNames.Count <= maxBindingsAmountOnSingleLine && isSingleLine

        if isSingleLine && not generateSingleLine && existingBindings.Count > 0 then
            ToMultilineRecord.Execute(recordExpr)

        let areBindingsOrdered = x.AreBindingsOrdered existingBindings (fieldNames |> Array.ofSeq)

        let generatedBindings: seq<IRecordFieldBinding> =
            if areBindingsOrdered && not existingBindings.IsEmpty then
                x.HandleOrdered (existingBindings.ToArray()) (fieldNames.ToArray()) generateSingleLine elementFactory
            else
                x.HandleUnordered (existingBindings.ToArray()) fieldsToAdd generateSingleLine elementFactory

        let existingBindings = recordExpr.FieldBindings

        if generateSingleLine then
            let lastBinding = existingBindings.Last()
            ModificationUtil.DeleteChild(lastBinding.Semicolon)

            for binding in existingBindings do
                if binding.NextSibling :? IRecordFieldBinding then
                    ModificationUtil.AddChildAfter(binding, Whitespace()) |> ignore

        if recordExpr.LeftBrace.NextSibling :? IRecordFieldBindingList then
            ModificationUtil.AddChildAfter(recordExpr.LeftBrace, Whitespace()) |> ignore

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

            hotspotSession.Execute())

    member private x.HandleOrdered (existingBindings: IRecordFieldBinding[]) (declaredFields: string[]) =
        let indexedBindings = x.CreateOrderedIndexedBindings existingBindings declaredFields

        x.Handle indexedBindings declaredFields

    member private x.HandleUnordered (existingBindings: IRecordFieldBinding[]) (fieldsToAdd: HashSet<string>) =
        let declaredFieldsCount = existingBindings.Length + fieldsToAdd.Count
        let indexedBindings = x.CreateUnorderedIndexedBindings existingBindings declaredFieldsCount
        let declaredFields =
            seq {
                yield! existingBindings |> Seq.map (fun binding -> binding.ReferenceName.ShortName )
                yield! fieldsToAdd
            }
            |> Array.ofSeq

        x.Handle indexedBindings declaredFields

    member private x.Handle (indexedBindings: IRecordFieldBinding[]) (declaredFields: string[])
        (generateSingleLine: bool) (elementFactory: IFSharpElementFactory): seq<IRecordFieldBinding> =

        let generatedBindings = LinkedList<IRecordFieldBinding>()

        for fieldIndex in [0..(declaredFields.Length - 1)] do
            let declaredField = declaredFields[fieldIndex]
            let createdBinding = indexedBindings[fieldIndex]

            if isNull createdBinding then
                let binding = elementFactory.CreateRecordFieldBinding(declaredField, generateSingleLine)

                let actualBinding =
                    if fieldIndex = 0 then
                        if isNull recordExpr.FieldBindingList then
                            let bindingList = RecordFieldBindingListNavigator.GetByFieldBinding(binding)
                            let actualList = ModificationUtil.AddChildAfter(recordExpr.LeftBrace, bindingList)
                            actualList.FieldBindings.First()
                        else
                            let anchor = recordExpr.FieldBindingList.FieldBindings.First()
                            ModificationUtil.AddChildBefore(anchor, binding)
                    else
                        let anchor: ITreeNode =
                            let indexedBinding = indexedBindings[fieldIndex - 1]
                            if generateSingleLine then
                                indexedBinding
                            else
                                x.SkipComments indexedBinding

                        let resultingNode =
                            // Nodes after block comments are not automatically moved to the new line, fixing it
                            if (not generateSingleLine) && anchor.GetTokenType() == FSharpTokenType.BLOCK_COMMENT then
                                let newLineNode = NewLine(binding.GetLineEnding())
                                let insertedNewLine = ModificationUtil.AddChildAfter(anchor, newLineNode)
                                ModificationUtil.AddChildAfter(insertedNewLine, binding)
                            else
                                ModificationUtil.AddChildAfter(anchor, binding)

                        resultingNode

                indexedBindings[fieldIndex] <- actualBinding
                generatedBindings.AddLast(actualBinding) |> ignore
            else
                if generateSingleLine && isNull createdBinding.Semicolon then
                    addSemicolon createdBinding

        generatedBindings

    member private x.SkipComments (node: ITreeNode): ITreeNode =
        let mutable current = node
        let mutable found = false

        while not found do
            let next = current.NextSibling
            if next == null then found <- true

            else if next.GetTokenType() == FSharpTokenType.WHITESPACE then
                let nextNext = next.NextSibling
                if nextNext == null then found <- true

                // Whitespace + Comment: set comment as current
                else if nextNext.IsCommentToken() then
                    current <- nextNext

                else found <- true

            else found <- true

        current

    member private x.AreBindingsOrdered (bindings: TreeNodeCollection<IRecordFieldBinding>)
        (declaredFields: string array) =
        if declaredFields.Length <= 1 then true else

        let mutable declaredFieldIndex = 0
        let mutable bindingIndex = 0
        let mutable ordered = true

        while bindingIndex < bindings.Count && ordered do
            while declaredFieldIndex < declaredFields.Length &&
                  declaredFields[declaredFieldIndex] != bindings[bindingIndex].ReferenceName.ShortName do
                declaredFieldIndex <- declaredFieldIndex + 1

            if declaredFieldIndex >= declaredFields.Length then
                ordered <- false

            bindingIndex <- bindingIndex + 1
            declaredFieldIndex <- declaredFieldIndex + 1

        ordered

    member private x.CreateOrderedIndexedBindings (bindings: IRecordFieldBinding[])
        (declaredFields: string[]): IRecordFieldBinding[] =

        let bindingsIndexed = Array.init declaredFields.Length (fun _ -> null)

        let mutable declaredFieldIndex = 0
        let mutable bindingIndex = 0

        while bindingIndex < bindings.Length do
            while declaredFieldIndex < declaredFields.Length &&
                  declaredFields[declaredFieldIndex] <> bindings[bindingIndex].ReferenceName.ShortName do
                declaredFieldIndex <- declaredFieldIndex + 1

            bindingsIndexed[declaredFieldIndex] <- bindings[bindingIndex]

            bindingIndex <- bindingIndex + 1
            declaredFieldIndex <- declaredFieldIndex + 1

        bindingsIndexed

    member private x.CreateUnorderedIndexedBindings (bindings: IRecordFieldBinding[])
        (declaredFieldsCount: int): IRecordFieldBinding[] =

        let bindingsIndexed = Array.init declaredFieldsCount (fun i ->
            if i < bindings.Length then bindings[i] else null)

        bindingsIndexed
