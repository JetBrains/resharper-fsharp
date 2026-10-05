module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.RecordExprUtil

open System.Collections.Generic
open System.Linq
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

let [<Literal>] MaxBindingsAmountOnSingleLine = 4

// todo: drop the sibling case when FCS reports the block separator of a spread
let private getSemicolon (recordMember: IRecordExprMember): ITreeNode =
    match recordMember.Semicolon with
    | null ->
        match recordMember.NextSibling with
        | node when getTokenType node == FSharpTokenType.SEMICOLON -> node
        | _ -> null
    | semicolon -> semicolon

let toMultiline (recordExpr: IRecordExpr) =
    let lineEnding = recordExpr.FSharpFile.GetLineEnding()

    use writeCookie = WriteLockCookie.Create(recordExpr.IsPhysical())

    let recordMembers = recordExpr.Members
    let firstMember = recordMembers[0]

    for recordMember in recordMembers do
        match getSemicolon recordMember with
        | null -> ()
        | semicolon -> ModificationUtil.DeleteChild(semicolon)

    for recordMember in recordMembers do
        if recordMember != firstMember then
            match recordMember.PrevSibling with
            | Whitespace node -> ModificationUtil.ReplaceChild(node, NewLine(lineEnding)) |> ignore
            | node when getTokenType node == FSharpTokenType.NEW_LINE -> ()
            | node -> ModificationUtil.AddChildAfter(node, NewLine(lineEnding)) |> ignore

            ModificationUtil.AddChildBefore(recordMember, Whitespace(firstMember.Indent)) |> ignore

let getSpreadFieldNames (spread: IExprTypeSpread) =
    match spread.Expression with
    | null -> Seq.empty
    | expr ->

    match expr.TryGetFcsType() |> Option.ofObj |> Option.bind tryGetAbbreviatedTypeEntity with
    | Some fcsEntity when fcsEntity.IsFSharpRecord -> fcsEntity.FSharpFields |> Seq.map (fun field -> field.Name)
    | _ -> Seq.empty

let private addSemicolon (recordMember: IRecordExprMember) =
    if isNull (getSemicolon recordMember) then
        match recordMember.Expression with
        | null -> failwith "Could not get expr"
        | expr -> ModificationUtil.AddChildAfter(expr, FSharpTokenType.SEMICOLON.CreateLeafElement()) |> ignore

let private areBindingsOrdered (bindings: TreeNodeCollection<IRecordFieldBinding>)
        (declaredFields: IList<string>): bool =

    let fieldIndices: IDictionary<string, int> =
        declaredFields
        |> Seq.mapi (fun i field -> field, i)
        |> dict

    let tryGetIndex (fieldIndices: IDictionary<string, int>) (binding: IRecordFieldBinding) =
        tryGetValue binding.ReferenceName.ShortName fieldIndices

    let indices = bindings |> Seq.map (tryGetIndex fieldIndices) |> Seq.toList
    indices |> List.forall Option.isSome &&
    indices |> Seq.choose id |> Seq.pairwise |> Seq.forall (fun (i1, i2) -> i1 <= i2)

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

let private getLastLeadingSpread (recordExpr: IRecordExpr) =
    recordExpr.Members
    |> Seq.takeWhile (fun recordMember -> recordMember :? IExprTypeSpread)
    |> Seq.tryLast
    |> Option.toObj

let private addBindingAfter (previousMember: IRecordExprMember) (binding: IRecordFieldBinding) generateSingleLine =
    if generateSingleLine then
        addSemicolon previousMember

    let anchor: ITreeNode =
        if generateSingleLine then
            previousMember
        else
            getLastMatchingNodeAfter isInlineSpaceOrComment previousMember

    let addedBinding =
        // Nodes after block comments are not automatically moved to the new line, fixing it
        if (not generateSingleLine) && anchor.GetTokenType() == FSharpTokenType.BLOCK_COMMENT then
            let newLineNode = NewLine(binding.GetLineEnding())
            let insertedNewLine = ModificationUtil.AddChildAfter(anchor, newLineNode)
            ModificationUtil.AddChildAfter(insertedNewLine, binding)
        else
            ModificationUtil.AddChildAfter(anchor, binding)

    if not generateSingleLine && previousMember :? IExprTypeSpread &&
            getTokenType addedBinding.PrevSibling != FSharpTokenType.NEW_LINE then
        match addedBinding.PrevSibling with
        | Whitespace node -> ModificationUtil.ReplaceChild(node, NewLine(addedBinding.GetLineEnding())) |> ignore
        | _ -> ModificationUtil.AddChildBefore(addedBinding, NewLine(addedBinding.GetLineEnding())) |> ignore

        ModificationUtil.AddChildBefore(addedBinding, Whitespace(previousMember.Indent)) |> ignore

    addedBinding

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
                    if isNull recordExpr.MemberList then
                        let bindingList = RecordMemberBindingListNavigator.GetByMember(binding)
                        let actualList = ModificationUtil.AddChildAfter(recordExpr.LeftBrace, bindingList)
                        actualList.Members.First() :?> IRecordFieldBinding
                    else
                        match getLastLeadingSpread recordExpr with
                        | null ->
                            let anchor = recordExpr.MemberList.Members.First()
                            ModificationUtil.AddChildBefore(anchor, binding)
                        | spread ->
                            addBindingAfter spread binding generateSingleLine
                else
                    addBindingAfter indexedBindings[fieldIndex - 1] binding generateSingleLine

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
    Assertion.Assert(recordTypeElement.IsFSharpRecord(), "Expecting record type")

    let spreads = recordExpr.TypeSpreads

    let spreadFieldNames = HashSet<string>()
    for spread in spreads do
        spreadFieldNames.UnionWith(getSpreadFieldNames spread)

    let fieldNames =
        recordTypeElement.GetRecordFieldNames()
        |> Seq.filter (spreadFieldNames.Contains >> not)
        |> Array.ofSeq

    let existingBindings = recordExpr.FieldBindings

    let fieldsToAdd = HashSet(fieldNames)
    for binding in existingBindings do
        fieldsToAdd.Remove(binding.ReferenceName.ShortName) |> ignore

    let fsFile = recordExpr.FSharpFile
    let elementFactory = fsFile.CreateElementFactory()

    use writeCookie = WriteLockCookie.Create(recordExpr.IsPhysical())

    let isSingleLine = recordExpr.IsSingleLine
    let memberCount = recordExpr.Members.Count

    let generateSingleLine =
        isSingleLine &&
        memberCount > 1 &&
        fieldNames.Length + spreads.Count <= MaxBindingsAmountOnSingleLine

    if isSingleLine && not generateSingleLine && memberCount > 0 then
        toMultiline recordExpr

    let areBindingsOrdered = areBindingsOrdered existingBindings fieldNames

    let generatedBindings: IRecordFieldBinding seq =
        if areBindingsOrdered && not existingBindings.IsEmpty then
            generateOrderedBindings recordExpr existingBindings fieldNames generateSingleLine elementFactory
        else
            generateUnorderedBindings recordExpr existingBindings fieldsToAdd generateSingleLine elementFactory

    let recordMembers = recordExpr.Members

    if generateSingleLine then
        match getSemicolon (recordMembers.Last()) with
        | null -> ()
        | semicolon -> ModificationUtil.DeleteChild(semicolon)

        for recordMember in recordMembers do
            if recordMember.NextSibling :? IRecordExprMember then
                ModificationUtil.AddChildAfter(recordMember, Whitespace()) |> ignore
    else
        let mutable isFirstBinding = true
        for binding in generatedBindings do
            if isFirstBinding && generatedBindings.First() == recordMembers.FirstOrDefault() then
                isFirstBinding <- false
            else
                if getTokenType binding.PrevSibling == FSharpTokenType.NEW_LINE then
                    ModificationUtil.AddChildBefore(binding, Whitespace(recordMembers[0].Indent)) |> ignore

            let nextMeaningfulSibling = binding.GetNextMeaningfulSibling()
            if nextMeaningfulSibling :? IRecordExprMember &&
                    getTokenType binding.NextSibling != FSharpTokenType.NEW_LINE then
                addNodesAfter binding [
                    NewLine(binding.GetLineEnding())
                    Whitespace(recordMembers[0].Indent)
                ] |> ignore

    let rightBrace = recordExpr.RightBrace

    match rightBrace.PrevSibling with
    | :? IRecordMemberBindingList ->
        ModificationUtil.AddChildBefore(rightBrace, Whitespace()) |> ignore
    | :? Whitespace as ws when ws.GetTextLength() > 1 ->
        if skipMatchingNodesBefore isInlineSpace rightBrace :? IRecordMemberBindingList then
            let first = getFirstMatchingNodeBefore isInlineSpace rightBrace
            replaceRangeWithNode first rightBrace.PrevSibling (Whitespace())
    | _ -> ()

    generatedBindings
