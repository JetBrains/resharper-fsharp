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

    let maxItemsCountOnSingleLine = 4

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
            existingBindings.Count > 1 && fieldNames.Count <= maxItemsCountOnSingleLine && isSingleLine

        if isSingleLine && not generateSingleLine && existingBindings.Count > 0 then
            ToMultilineRecord.Execute(recordExpr)

        let areBindingsOrdered = x.AreBindingsOrdered existingBindings (fieldNames |> Array.ofSeq)

        if generateSingleLine && not existingBindings.IsEmpty then
            addSemicolon (existingBindings.Last())

        let generatedBindings =
            if areBindingsOrdered && not existingBindings.IsEmpty then
                x.HandleOrdered generateSingleLine (existingBindings.ToArray()) (fieldNames.ToArray()) elementFactory
            else
                x.HandleUnordered generateSingleLine existingBindings fieldsToAdd elementFactory

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

    member private x.HandleOrdered
        (generateSingleLine: bool) (existingBindings: IRecordFieldBinding[])
        (declaredFields: string[]) (elementFactory: IFSharpElementFactory): seq<IRecordFieldBinding> =

        let generatedBindings = LinkedList<IRecordFieldBinding>()
        let indexedBindings = x.CreatedIndexedBindings existingBindings declaredFields

        for fieldIndex in [0..(declaredFields.Length - 1)] do
            let declaredField = declaredFields[fieldIndex]
            let createdBinding = indexedBindings[fieldIndex]

            if isNull createdBinding then
                let binding = elementFactory.CreateRecordFieldBinding(declaredField, generateSingleLine)

                let actualBinding =
                    if fieldIndex = 0 then
                        ModificationUtil.AddChildBefore(recordExpr.FieldBindingList.FieldBindings.First(), binding)
                    else
                        let anchor = indexedBindings[fieldIndex - 1]
                        ModificationUtil.AddChildAfter(anchor, binding)

                indexedBindings[fieldIndex] <- actualBinding
                generatedBindings.AddLast(actualBinding) |> ignore
            else
                if generateSingleLine && isNull createdBinding.Semicolon then
                    addSemicolon createdBinding

        generatedBindings

    member private x.HandleUnordered
        (generateSingleLine: bool) (existingBindings: TreeNodeCollection<IRecordFieldBinding>)
        (fieldsToAdd: HashSet<string>) (elementFactory: IFSharpElementFactory) : seq<IRecordFieldBinding> =

        let generatedBindings = LinkedList<IRecordFieldBinding>()

        let anchorBindingList =
            match existingBindings.LastOrDefault() with
            | null ->
                let firstField = fieldsToAdd.First()
                fieldsToAdd.Remove(firstField) |> ignore
                let binding = elementFactory.CreateRecordFieldBinding(firstField, generateSingleLine)
                let bindingList = RecordFieldBindingListNavigator.GetByFieldBinding(binding)
                let actualList = ModificationUtil.AddChildAfter(recordExpr.LeftBrace, bindingList)
                generatedBindings.AddLast(actualList.FieldBindings.First()) |> ignore
                actualList
            | binding -> RecordFieldBindingListNavigator.GetByFieldBinding(binding)

        for name in fieldsToAdd do
            let binding = elementFactory.CreateRecordFieldBinding(name, generateSingleLine)
            generatedBindings.AddLast(ModificationUtil.AddChild(anchorBindingList, binding)) |> ignore

        generatedBindings

    member private x.AreBindingsOrdered (bindings: TreeNodeCollection<IRecordFieldBinding>)
        (declaredFields: string array) =
        if declaredFields.Length <= 1 then true else

        let mutable declaredFieldIndex = 0
        let mutable bindingIndex = 0
        let mutable ordered = true

        while bindingIndex < bindings.Count && ordered do
            while declaredFieldIndex < declaredFields.Length && declaredFields[declaredFieldIndex] != bindings[bindingIndex].ReferenceName.ShortName do
                declaredFieldIndex <- declaredFieldIndex + 1

            if declaredFieldIndex >= declaredFields.Length then
                ordered <- false

            bindingIndex <- bindingIndex + 1
            declaredFieldIndex <- declaredFieldIndex + 1

        ordered

    member private x.CreatedIndexedBindings (bindings: IRecordFieldBinding[])
        (declaredFields: string[]): IRecordFieldBinding[] =

        let bindingsIndexed = Array.init declaredFields.Length (fun _ -> null)

        let mutable declaredFieldIndex = 0
        let mutable bindingIndex = 0

        while bindingIndex < bindings.Length do
            while declaredFieldIndex < declaredFields.Length && declaredFields[declaredFieldIndex] <> bindings[bindingIndex].ReferenceName.ShortName do
                declaredFieldIndex <- declaredFieldIndex + 1

            bindingsIndexed[declaredFieldIndex] <- bindings[bindingIndex]

            bindingIndex <- bindingIndex + 1
            declaredFieldIndex <- declaredFieldIndex + 1

        bindingsIndexed
