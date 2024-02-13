module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.RecordExprUtil

open System.Collections.Generic
open System.Linq
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

let [<Literal>] MaxBindingsAmountOnSingleLine = 4

let toMultiline (recordExpr: IRecordExpr) =
    let lineEnding = recordExpr.FSharpFile.GetLineEnding()

    use writeCookie = WriteLockCookie.Create(recordExpr.IsPhysical())

    let bindings = recordExpr.FieldBindings
    let firstBinding = bindings[0]

    for binding in bindings do
        if binding != firstBinding then
            match binding.PrevSibling with
            | Whitespace node -> ModificationUtil.ReplaceChild(node, NewLine(lineEnding)) |> ignore
            | node -> ModificationUtil.AddChildAfter(node, NewLine(lineEnding)) |> ignore

            ModificationUtil.AddChildBefore(binding, Whitespace(firstBinding.Indent)) |> ignore

        match binding.Semicolon with
        | null -> ()
        | semicolon -> ModificationUtil.DeleteChild(semicolon)

let private addSemicolon (binding: IRecordFieldBinding) =
    if isNull binding.Semicolon then
        match binding.Expression with
        | null -> failwith "Could not get expr"
        | expr -> ModificationUtil.AddChildAfter(expr, FSharpTokenType.SEMICOLON.CreateLeafElement()) |> ignore

let private areBindingsOrdered (bindings: TreeNodeCollection<IRecordFieldBinding>)
    (declaredFields: IList<string>): bool =
    if bindings.Count <= 1 then true else

    let mutable declaredFieldIndex = 0
    let mutable bindingIndex = 0
    let mutable ordered = true

    while bindingIndex < bindings.Count && ordered do
        while declaredFieldIndex < declaredFields.Count &&
              declaredFields[declaredFieldIndex] != bindings[bindingIndex].ReferenceName.ShortName do
            declaredFieldIndex <- declaredFieldIndex + 1

        if declaredFieldIndex >= declaredFields.Count then
            ordered <- false

        bindingIndex <- bindingIndex + 1
        declaredFieldIndex <- declaredFieldIndex + 1

    ordered

let private createOrderedIndexedBindings (bindings: TreeNodeCollection<IRecordFieldBinding>)
    (declaredFields: IList<string>): IRecordFieldBinding[] =

    let bindingsIndexed = Array.init declaredFields.Count (fun _ -> null)

    let mutable declaredFieldIndex = 0
    let mutable bindingIndex = 0

    while bindingIndex < bindings.Count do
        while declaredFieldIndex < declaredFields.Count &&
              declaredFields[declaredFieldIndex] <> bindings[bindingIndex].ReferenceName.ShortName do
            declaredFieldIndex <- declaredFieldIndex + 1

        bindingsIndexed[declaredFieldIndex] <- bindings[bindingIndex]

        bindingIndex <- bindingIndex + 1
        declaredFieldIndex <- declaredFieldIndex + 1

    bindingsIndexed

let private createUnorderedIndexedBindings (bindings: TreeNodeCollection<IRecordFieldBinding>)
    (declaredFieldsCount: int): IRecordFieldBinding[] =

    let bindingsIndexed = Array.init declaredFieldsCount (fun i ->
        if i < bindings.Count then bindings[i] else null)

    bindingsIndexed

let private generateBindingsImpl (recordExpr: IRecordExpr) (indexedBindings: IRecordFieldBinding[]) (declaredFields: IList<string>)
        (generateSingleLine: bool) (elementFactory: IFSharpElementFactory) : seq<IRecordFieldBinding> =

    let generatedBindings = LinkedList<IRecordFieldBinding>()

    for fieldIndex in [0..(declaredFields.Count - 1)] do
        let declaredField = declaredFields[fieldIndex]
        let createdBinding = indexedBindings[fieldIndex]

        if isNull createdBinding then
            let binding = elementFactory.CreateRecordFieldBinding([|declaredField|], generateSingleLine)

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
                            getLastMatchingNodeAfter isInlineSpaceOrComment indexedBinding

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

let private generateOrderedBindings recordExpr (existingBindings: TreeNodeCollection<IRecordFieldBinding>) (declaredFields: IList<string>) =
    let indexedBindings = createOrderedIndexedBindings existingBindings declaredFields
    generateBindingsImpl recordExpr indexedBindings declaredFields

let private generateUnorderedBindings recordExpr (existingBindings: TreeNodeCollection<IRecordFieldBinding>) (fieldsToAdd: HashSet<string>) =
    let declaredFieldsCount = existingBindings.Count + fieldsToAdd.Count
    let indexedBindings = createUnorderedIndexedBindings existingBindings declaredFieldsCount
    let declaredFields =
        [| yield! existingBindings |> Seq.map (fun binding -> binding.ReferenceName.ShortName )
           yield! fieldsToAdd |]

    generateBindingsImpl recordExpr indexedBindings declaredFields

let generateBindings (recordTypeElement: ITypeElement) (recordExpr: IRecordExpr) : IRecordFieldBinding seq =
    Assertion.Assert(recordTypeElement.IsRecord(), "Expecting record type")

    let fieldNames = recordTypeElement.GetRecordFieldNames()
    let existingBindings = recordExpr.FieldBindings

    let fieldsToAdd = HashSet(fieldNames)
    for binding in existingBindings do
        fieldsToAdd.Remove(binding.ReferenceName.ShortName) |> ignore

    let fsFile = recordExpr.FSharpFile
    let elementFactory = fsFile.CreateElementFactory()

    use writeCookie = WriteLockCookie.Create(recordExpr.IsPhysical())
    use disableFormatter = new DisableCodeFormatter()

    let isSingleLine = recordExpr.IsSingleLine

    let generateSingleLine =
        isSingleLine &&
        existingBindings.Count > 1 &&
        fieldNames.Count <= MaxBindingsAmountOnSingleLine

    if isSingleLine && not generateSingleLine && existingBindings.Count > 0 then
        toMultiline recordExpr

    let areBindingsOrdered = areBindingsOrdered existingBindings fieldNames

    let generatedBindings: IRecordFieldBinding seq =
        if areBindingsOrdered && not existingBindings.IsEmpty then
            generateOrderedBindings recordExpr existingBindings fieldNames generateSingleLine elementFactory
        else
            generateUnorderedBindings recordExpr existingBindings fieldsToAdd generateSingleLine elementFactory

    let existingBindings = recordExpr.FieldBindings

    if recordExpr.LeftBrace.NextSibling :? IRecordFieldBindingList then
        ModificationUtil.AddChildAfter(recordExpr.LeftBrace, Whitespace()) |> ignore

    if generateSingleLine then
        let lastBinding = existingBindings.Last()
        ModificationUtil.DeleteChild(lastBinding.Semicolon)

        for binding in existingBindings do
            if binding.NextSibling :? IRecordFieldBinding then
                ModificationUtil.AddChildAfter(binding, Whitespace()) |> ignore
    else
        let mutable isFirstBinding = true
        for binding in generatedBindings do
            if isFirstBinding && generatedBindings.First() == existingBindings.FirstOrDefault() then
                isFirstBinding <- false
            else
                let nodeBeforeSpace = skipMatchingNodesBefore isInlineSpaceOrComment binding
                if getTokenType nodeBeforeSpace != FSharpTokenType.NEW_LINE then
                    addNodesBefore binding [
                        NewLine(binding.GetLineEnding())
                        Whitespace(existingBindings[0].Indent)
                    ] |> ignore

                if getTokenType binding.PrevSibling == FSharpTokenType.NEW_LINE then
                    ModificationUtil.AddChildBefore(binding, Whitespace(existingBindings[0].Indent)) |> ignore

            let nextMeaningfulSibling = binding.GetNextMeaningfulSibling()
            if nextMeaningfulSibling :? IRecordFieldBinding &&
                    getTokenType binding.NextSibling != FSharpTokenType.NEW_LINE then
                addNodesAfter binding [
                    NewLine(binding.GetLineEnding())
                    Whitespace(existingBindings[0].Indent)
                ] |> ignore

    let rightBrace = recordExpr.RightBrace

    match rightBrace.PrevSibling with
    | :? IRecordFieldBindingList ->
        ModificationUtil.AddChildBefore(rightBrace, Whitespace()) |> ignore
    | :? Whitespace as ws when ws.GetTextLength() > 1 ->
        if skipMatchingNodesBefore isInlineSpace rightBrace :? IRecordFieldBindingList then
            let first = getFirstMatchingNodeBefore isInlineSpace rightBrace
            replaceRangeWithNode first rightBrace.PrevSibling (Whitespace())
    | _ -> ()

    generatedBindings

let showHotspotsForGeneratedBindings recordExpr bindings =
    ()