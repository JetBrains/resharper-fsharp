module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.MatchTree

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpPatternUtil.ParentTraversal
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.Util

// todo: isInstPat

[<RequireQualifiedAccess>]
type MatchType =
    | Bool of fcsType: FSharpType
    | Enum of enumEntity: FSharpEntity * fields: (FSharpField * ConstantValue) array
    | Tuple of isStruct: bool * types: MatchType array
    | Record of record: FcsEntityInstance
    | Union of union: FcsEntityInstance
    | UnionCase of case: FSharpUnionCase * union: FcsEntityInstance
    | List of fcsType: FSharpType
    | Other of fcsType: FSharpType
    | Error

    member this.FcsType =
        match this with
        | Bool fcsType
        | List fcsType
        | Other fcsType -> Some fcsType
        | Record fcsEntityInstance
        | Union fcsEntityInstance -> Some fcsEntityInstance.FcsType
        | Enum(fcsEntity, _) -> Some(fcsEntity.AsType())
        | _ -> None

    member this.SupportsNullTest: bool =
        match this with
        | Other fcsType ->
            match tryGetAbbreviatedTypeEntity fcsType with
            | Some fcsEntity -> not fcsEntity.IsFSharp && not fcsEntity.IsValueType
            | _ -> false
        | _ -> false


[<RequireQualifiedAccess>]
type MatchTest =
    | Discard of skipInBind: bool
    | Named of name: string option
    | Value of value: ConstantValue
    | Tuple of isStruct: bool
    | TupleItem of index: int
    | Union of index: int
    | UnionCase
    | Field of index: int * name: string
    | EmptyList
    | ListCons
    | ListConsItem of isHead: bool
    | And
    | Or
    | Error
    | As
    | ActivePatternCase of index: int * group: FSharpActivePatternGroup
    | Record
    | Null

and MatchValue =
    { Type: MatchType
      Path: MatchTest list }

and MatchNode =
    { Value: MatchValue
      mutable Pattern: Pattern }

    static member Create(value, pattern) =
        { Value = value
          Pattern = pattern }

    override this.ToString() =
        match this.Pattern, this.Value.Type with
        | ((MatchTest.Discard _ | MatchTest.Named None), _), _ -> "_"
        | (MatchTest.Named(Some name), _), _ -> name
        | (MatchTest.Value value, _), MatchType.Bool _ -> string value.BoolValue

        | (MatchTest.Tuple _, nodes), _ ->
            let items =
                nodes
                |> List.map string
                |> String.concat ", "
            $"({items})"

        | (MatchTest.Union index, [{Pattern = (MatchTest.Discard _, _)}]), MatchType.Union unionInstance ->
            $"{unionInstance.Entity.UnionCases[index].DisplayName} _"

        | (MatchTest.Union _, [node]), _ -> string node

        | (MatchTest.UnionCase, nodes), MatchType.UnionCase(case, _) ->
            let items =
                nodes
                |> List.map string
                |> String.concat ", "
            $"{case.DisplayName}({items})"

        | (MatchTest.As, [node1; node2]), _ ->
            $"{string node1} as {string node2}"

        | (MatchTest.ActivePatternCase(index, group), nodes), _ ->
            let nodes = 
                nodes
                |> List.tryExactlyOne
                |> Option.map string
                |> Option.defaultWith (fun _ ->
                    let items =
                        nodes
                        |> List.map string
                        |> String.concat ", "
                    $"{(group.Names[index])}({items})"
                )

            $"{(group.Names[index])} {nodes}"

        | (MatchTest.Error, _), _ -> "error"

        | (MatchTest.EmptyList, _), _ -> "[]"

        | (MatchTest.ListCons, [node1; node2]), _ ->
            $"({string node1} :: {string node2})"

        | (MatchTest.Or, [node1; node2]), _ ->
            $"({string node1}) | ({string node2})"

        | (MatchTest.Null, _), _ -> "null"

        | _ -> "other case"

and Pattern = MatchTest * MatchNode list

[<RequireQualifiedAccess>]
type Deconstruction =
    | Discard of skipInBind: bool
    | Named of name: string
    | ActivePattern of activePattern: FSharpActivePatternGroup
    | InnerPatterns

type Deconstructions = OneToListMap<MatchTest list, Deconstruction>


module MatchType =
    let getFieldConstantValue (context: ITreeNode) (fcsField: FSharpField) =
        fcsField.LiteralValue
        |> Option.map (fun value ->
            ConstantValue.Create(value, fcsField.FieldType.MapType(context))
        )

    let rec ofFcsType (context: ITreeNode) (fcsType: FSharpType) =
        if isNull fcsType then MatchType.Error else

        if fcsType.IsStructTupleType then createTupleType context true fcsType.GenericArguments else
        if fcsType.IsTupleType then createTupleType context false fcsType.GenericArguments else

        if not fcsType.ErasedType.HasTypeDefinition then MatchType.Other fcsType else

        let fcsEntity = fcsType.ErasedType.TypeDefinition

        if fcsEntity.IsEnum then
            let fieldLiterals =
                fcsEntity.FSharpFields
                |> Seq.tail
                |> Seq.map (fun fcsField -> fcsField, getFieldConstantValue context fcsField)
                |> Seq.choose (function
                    | fcsField, Some constantValue when not (constantValue.IsBadValue()) -> Some(fcsField, constantValue)
                    | _ -> None
                )
                |> Seq.distinctBy snd
                |> Array.ofSeq
            MatchType.Enum(fcsEntity, fieldLiterals) else

        if fcsEntity.IsArrayType then MatchType.Other fcsType else

        // todo: list
        let entityFqn = fcsEntity.BasicQualifiedName
        match entityFqn with
        | "System.Boolean" -> MatchType.Bool fcsType
        | "Microsoft.FSharp.Collections.FSharpList`1" -> MatchType.List fcsType
        | _ ->

        if fcsEntity.IsFSharpUnion then MatchType.Union(FcsEntityInstance.create fcsType) else
        if fcsEntity.IsFSharpRecord then MatchType.Record(FcsEntityInstance.create fcsType) else

        MatchType.Other fcsType

    and createTupleType context isStruct types =
        let types =
            types
            |> Seq.map (ofFcsType context)
            |> Array.ofSeq
        MatchType.Tuple(isStruct, types)

    let getValues (context: ITreeNode) (matchType: MatchType) =
        match matchType with
        | MatchType.Bool _ ->
            [let psiModule = context.GetPsiModule()
             ConstantValue.Bool(true, psiModule)
             ConstantValue.Bool(false, psiModule)]
        | _ -> failwith "todo1"

    let getListNodeType context isHead value =
        match isHead, value.Type with
        | true, MatchType.List itemFcsType ->
            let abbreviatedType = getAbbreviatedType itemFcsType
            ofFcsType context abbreviatedType.GenericArguments[0]

        | _ -> value.Type


module MatchTest =
    let rec ignores (node: MatchNode) =
        match node.Pattern with
        | MatchTest.Discard _, _
        | MatchTest.Named _, _ -> true

        | MatchTest.Tuple _, nodes ->
            nodes |> List.forall ignores

        | _ -> false

    let rec matches (node: MatchNode) (existingNode: MatchNode) : bool =
        match existingNode.Pattern, node.Pattern with
        | (MatchTest.Discard _, _), _ -> true
        | (MatchTest.Named _, _), _ -> true
        | (MatchTest.Value existingValue, _), (MatchTest.Value value, _) -> existingValue.Equals(value)

        | (MatchTest.ActivePatternCase(_, existingGroup), [activePatternPatNode]), _ when
                existingGroup.Names.Count = 1 && existingGroup.IsTotal ->
            ignores activePatternPatNode

        | (MatchTest.ActivePatternCase(existingIndex, existingGroup), _), (MatchTest.ActivePatternCase(index, group), _) ->
            existingIndex = index &&
            existingGroup.Name = group.Name &&
            existingGroup.DeclaringEntity = group.DeclaringEntity

        | (MatchTest.Union existingIndex, [existingCaseNode]), (MatchTest.Union index, []) ->
            existingIndex = index && ignores existingCaseNode

        // todo: add test with different unions
        | (MatchTest.Union existingIndex, [node1]), (MatchTest.Union index, [node2]) ->
            existingIndex = index &&
            matches node2 node1

        | (MatchTest.UnionCase, [{ Pattern = MatchTest.Discard _, _ }]), (MatchTest.UnionCase, _) -> true

        | (MatchTest.UnionCase, fields1), (MatchTest.UnionCase, fields2) ->
            List.forall2 matches fields2 fields1

        | (MatchTest.Field _, [node1]), (MatchTest.Field _, [node2]) ->
            matches node2 node1

        // todo: add test with different lengths
        | (MatchTest.Tuple isStruct1, nodes1), (MatchTest.Tuple isStruct2, nodes2) ->
            (isStruct2 || not isStruct1) &&
            List.forall2 matches nodes2 nodes1

        | (MatchTest.EmptyList, _), (MatchTest.EmptyList, _) -> true

        | (MatchTest.ListCons, existingNodes), (MatchTest.ListCons, []) ->
            List.forall ignores existingNodes

        | (MatchTest.ListCons, nodes1), (MatchTest.ListCons, nodes2) ->
            List.forall2 matches nodes2 nodes1

        | (MatchTest.Or, nodes), _ -> List.exists (matches node) nodes

        | (MatchTest.As, nodes), _ -> List.forall (matches node) nodes

        | (MatchTest.Record, fields1), (MatchTest.Record, fields2) ->
            List.forall2 matches fields2 fields1

        | (MatchTest.Null, []), (MatchTest.Null, []) -> true

        | _ -> false

    let rec initialPattern (deconstructions: Deconstructions) (context: ITreeNode) isGenerating (value: MatchValue) =
        let makeInitialFieldPatterns substitution path (fields: FSharpField seq) =
            fields
            |> Seq.mapi (fun i field ->
                let test = MatchTest.Field(i, field.DisplayNameCore)
                let path = test :: path

                let fieldFcsType = field.FieldType.Instantiate(substitution)
                let fieldType = MatchType.ofFcsType context fieldFcsType
                let itemValue = { Type = fieldType; Path = path }
                let matchPattern = initialPattern deconstructions context isGenerating itemValue
                MatchNode.Create(itemValue, matchPattern)
            )
            |> List.ofSeq

        let values = deconstructions.GetValuesSafe(value.Path)

        let deconstruction =
            let values = 
                if not isGenerating then
                    values |> Seq.filter (function Deconstruction.ActivePattern _ -> false | _ -> true)
                else
                    values

            let mutable named = None
            values
            |> Seq.tryFind (fun d ->
                match d with
                | Deconstruction.Discard _ -> false
                | Deconstruction.Named _ ->
                    named <- Some d
                    false
                | _ -> true
            )
            |> Option.orElse named
            |> Option.orElseWith (fun _ -> Seq.tryHead values)

        match deconstruction with
        | Some(Deconstruction.Discard skipInBind) -> MatchTest.Discard skipInBind, []
        | Some(Deconstruction.Named name) -> MatchTest.Named(Some name), []
        | Some(Deconstruction.ActivePattern group) -> MatchTest.ActivePatternCase(0, group), []

        | Some(Deconstruction.InnerPatterns) ->
            match value.Type with
            | MatchType.Bool _ ->
                MatchTest.Value(ConstantValue.Bool(true, context.GetPsiModule())), []

            | MatchType.Enum(_, fields) ->
                MatchTest.Value(snd fields[0]), []

            | MatchType.Tuple(isStruct, types) ->
                let nodes =
                    types
                    |> Seq.mapi (fun i itemType ->
                        let path = MatchTest.TupleItem i :: value.Path
                        let itemValue = { Type = itemType; Path = path }
                        let matchPattern = initialPattern deconstructions context isGenerating itemValue
                        MatchNode.Create(itemValue, matchPattern)
                    )
                    |> List.ofSeq
                MatchTest.Tuple isStruct, nodes

            | MatchType.Union unionEntityInstance ->
                let unionCase = unionEntityInstance.Entity.UnionCases[0]

                let test = MatchTest.Union 0
                let unionPath = test :: value.Path
                let caseMatchType = MatchType.UnionCase(unionCase, unionEntityInstance)
                let caseValue = { Type = caseMatchType; Path = unionPath }

                let casePattern =
                    if unionCase.HasFields then
                        initialPattern deconstructions context isGenerating caseValue
                    else
                        MatchTest.Discard false, []

                test, [MatchNode.Create(caseValue, casePattern)]

            | MatchType.List _ ->
                if isGenerating then MatchTest.EmptyList, [] else

                let createListItemNode isHead =
                    let listTest = MatchTest.ListCons
                    let listPath = listTest :: value.Path

                    let listItemTest = MatchTest.ListConsItem isHead
                    let listItemPath = listItemTest :: listPath

                    let nodeType = MatchType.getListNodeType context isHead value
                    let nodeValue = { Type = nodeType; Path = listItemPath }
                    let nodePattern = initialPattern deconstructions context isGenerating nodeValue
                    MatchNode.Create(nodeValue, nodePattern)

                let headNode = createListItemNode true
                let tailNode = createListItemNode false

                MatchTest.ListCons, [headNode; tailNode]

            | MatchType.Record recordInstance ->
                let test = MatchTest.Record
                let recordPath = test :: value.Path
                let fields = recordInstance.Entity.FSharpFields
                let fieldNodes = makeInitialFieldPatterns recordInstance.Substitution recordPath fields
                test, fieldNodes

            | MatchType.Other _ as matchType when matchType.SupportsNullTest ->
                MatchTest.Null, []

            | _ ->
                MatchTest.Named None, []

        | _ ->
            match value.Type with
            | MatchType.UnionCase(unionCase, unionEntityInstance) ->
                let fields = unionCase.Fields
                if fields.Count = 0 then MatchTest.Discard false, [] else

                let isSingleField = fields.Count = 1

                let caseTest = MatchTest.UnionCase
                let casePath = caseTest :: value.Path

                let fieldNodes =
                    fields
                    |> Seq.mapi (fun i field ->
                        let fieldTest = MatchTest.Field(i, field.DisplayNameCore)
                        let fieldPath = fieldTest :: casePath
                        let fieldFcsType = field.FieldType.Instantiate(unionEntityInstance.Substitution)
                        let fieldType = MatchType.ofFcsType context fieldFcsType
                        let fieldValue = { Type = fieldType; Path = fieldPath }
                        let fieldPattern =
                            if deconstructions.ContainsKey(fieldPath) then
                                initialPattern deconstructions context isGenerating fieldValue
                            else
                                let defaultItemName = if isSingleField then "Item" else $"Item{i + 1}"
                                let name = if field.Name <> defaultItemName then Some field.Name else None
                                MatchTest.Named name, []

                        MatchNode.Create(fieldValue, fieldPattern)
                    )
                    |> List.ofSeq
                MatchTest.UnionCase, fieldNodes

            | _ ->
                MatchTest.Named None, []

module MatchNode =
    type BindContext =
        { Context: ITreeNode
          Factory: IFSharpElementFactory
          IdentifierPattern: IReferencePat
          CachedPatterns: IDictionary<string, IFSharpPattern>
          SeenTypes: ISet<FSharpEntity> }

        static member Create(context: ITreeNode) =
            let factory = context.CreateElementFactory()
            { Context = context
              Factory = factory
              IdentifierPattern = factory.CreatePattern("a", false) :?> IReferencePat
              CachedPatterns = Dictionary()
              SeenTypes = HashSet() }


    let bind (context: BindContext) (usedNames: ISet<string>) (oldPat: IFSharpPattern) (node: MatchNode) =
        let createIdentifierPattern name =
            let pat = context.IdentifierPattern.Copy()
            pat.ReferenceName.SetName(name) |> ignore
            pat

        let createPattern text =
            context.CachedPatterns
                .GetOrCreateValue(text, fun text -> context.Factory.CreatePattern(text, false))
                .Copy((*context.Context*))

        let markSeenType (fcsEntity: FSharpEntity) =
            context.SeenTypes.Add(fcsEntity) |> ignore

        let rec bind (context: ITreeNode) (usedNames: ISet<string>) (oldPat: IFSharpPattern) (node: MatchNode) =
            let replaceWithPattern existingPattern (newPat: IFSharpPattern) =
                let newPat = ModificationUtil.ReplaceChild(existingPattern, newPat)
                ParenPatUtil.addParensIfNeeded newPat

            let replaceTuplePat existingPattern nodes =
                let tupleItemsText = nodes |> List.map (fun _ -> "_") |> String.concat ", "
                let tuplePat = createPattern tupleItemsText
                let tuplePat = replaceWithPattern existingPattern tuplePat :?> ITuplePat

                Seq.iter2 (bind context usedNames) tuplePat.Patterns nodes

            let replaceNamedPattern node name =
                let names =
                    // todo: ignore `value` name in option/voption
                    let namesCollection = FSharpNamingService.createEmptyNamesCollection context
                    match name with
                    | Some name ->
                        FSharpNamingService.addNames name oldPat namesCollection
                    | _ ->
                        match node.Value.Type.FcsType with
                        | Some fcsType -> FSharpNamingService.addNamesForType (fcsType.MapType(oldPat)) namesCollection
                        | _ -> namesCollection
                    |> FSharpNamingService.prepareNamesCollection usedNames oldPat

                let name =
                    if names.Count <> 0 then
                        let name = names[0]
                        let name = FSharpNamingService.mangleNameIfNecessary name
                        usedNames.Add(name.RemoveBackticks()) |> ignore
                        name
                    else
                        "_"

                createIdentifierPattern name |> replaceWithPattern oldPat |> ignore

            match node.Pattern with
            | MatchTest.Named name, _ ->
                replaceNamedPattern node name

            | MatchTest.ListCons, _ ->
                replaceNamedPattern node None

            | MatchTest.ActivePatternCase(index, group), _ ->
                let text = FSharpNamingService.mangleNameIfNecessary group.Names[index]
                createPattern text |> replaceWithPattern oldPat |> ignore

            | MatchTest.Tuple _, nodes ->
                replaceTuplePat oldPat nodes

            | MatchTest.Union _, [{ Value = { Type = MatchType.UnionCase(unionCase, _) } } as casNode] ->
                match node.Value.Type with
                | MatchType.Union unionInstance ->
                    markSeenType unionInstance.Entity
                | _ -> ()

                let patText = if not unionCase.HasFields then unionCase.Name else $"{unionCase.Name} _"
                let pat = createPattern patText |> replaceWithPattern oldPat :?> IReferenceNameOwnerPat
                FSharpBindUtil.bindFcsSymbolToReference context pat.ReferenceName.Reference unionCase "get pattern"

                if not unionCase.HasFields then () else

                let unionCasePat = pat :?> IParametersOwnerPat
                let paramsPat = unionCasePat.Parameters[0]

                match casNode.Pattern with
                | MatchTest.UnionCase, nodes ->
                    match nodes with
                    | [] -> ()
                    | [node] -> bind context usedNames paramsPat node
                    | nodes -> replaceTuplePat paramsPat nodes
                | _ -> ()

            | MatchTest.Value value, _ when value.IsBoolean() ->
                match value.BoolValue with
                | true -> createPattern "true" |> replaceWithPattern oldPat |> ignore
                | false -> createPattern "false" |> replaceWithPattern oldPat |> ignore

            | MatchTest.Value constantValue, _ ->
                match node.Value.Type with
                | MatchType.Enum(_, fields) ->
                    let field, _ = fields |> Array.find (snd >> ((=) constantValue))
                    let patText = field.DisplayNameCore
                    let pat = createPattern patText |> replaceWithPattern oldPat :?> IReferenceNameOwnerPat
                    FSharpBindUtil.bindFcsSymbolToReference pat pat.ReferenceName.Reference field "get pattern"
                    match node.Value.Type with
                    | MatchType.Enum(enumEntity, _) ->
                        markSeenType enumEntity
                    | _ -> ()

                | valueType -> failwith $"Unexpected value type: {valueType}"

            | MatchTest.EmptyList, _ ->
                createPattern "[]" |> replaceWithPattern oldPat |> ignore

            | MatchTest.Record, nodes ->
                let fieldNodes =
                    nodes
                    |> List.filter (fun node ->
                        match node.Pattern with
                        | MatchTest.Discard true, _ -> false
                        | _ -> true)

                let recordFieldsText =
                    fieldNodes
                    |> List.map (fun node ->
                        match node.Value.Path with
                        | MatchTest.Field(_, name) :: _ -> $"{name} = _"
                        | _ -> failwith $"Unexpected node: {node}"
                    )
                    |> String.concat "; "

                let recordPat = createPattern $"{{ {recordFieldsText} }}"
                let recordPat = replaceWithPattern oldPat recordPat :?> IRecordPat
                let fieldPats = recordPat.FieldPatterns |> Seq.map (fun p -> p.As<IFieldPat>().Pattern)

                Seq.iter2 (bind context usedNames) fieldPats fieldNodes

            | MatchTest.Null, [] ->
                createPattern "null" |> replaceWithPattern oldPat |> ignore

            | _ -> ()

        bind context.Context usedNames oldPat node

    let rec duplicate (node: MatchNode) : MatchNode =
        let test, nodes = node.Pattern
        let nodes = nodes |> List.map duplicate
        let pattern = test, nodes
        MatchNode.Create(node.Value, pattern)

    let tryRejectDiscardPattern (rejected: List<MatchNode>) wholeNode (node: MatchNode) (context: ITreeNode) =
        let nodePattern = node.Pattern

        let valueType = node.Value.Type

        let ignoresNestedValues (types: IList<_>) (nodes: MatchNode list) =
            types.Count = nodes.Length &&
            nodes |> List.forall MatchTest.ignores

        let allRejected =
            match valueType, nodePattern with
            | MatchType.Tuple(_, types), (_, nodes) ->
                ignoresNestedValues types nodes &&
                rejected |> Seq.exists (MatchTest.matches wholeNode)

            | MatchType.UnionCase(unionCase, _), (_, nodes) ->
                let fields = unionCase.Fields

                fields.Count > 0 &&
                ignoresNestedValues fields nodes &&
                rejected |> Seq.exists (MatchTest.matches wholeNode)

            | MatchType.Record recordInstance, (MatchTest.Record, nodes) ->
                let fields = recordInstance.Entity.FSharpFields

                ignoresNestedValues fields nodes &&
                rejected |> Seq.exists (MatchTest.matches wholeNode)

            | _ ->

            let tests =
                match valueType with
                | MatchType.Bool _ ->
                    [ MatchTest.Value(ConstantValue.Bool(true, context.GetPsiModule()))
                      MatchTest.Value(ConstantValue.Bool(false, context.GetPsiModule())) ]

                | MatchType.Enum(_, fields) ->
                    fields
                    |> Array.map (snd >> MatchTest.Value)
                    |> Array.toList

                | MatchType.Union unionInstance ->
                    unionInstance.Entity.UnionCases
                    |> Seq.mapi (fun i _ -> MatchTest.Union i)
                    |> Seq.toList

                | MatchType.List _ ->
                    [ MatchTest.EmptyList
                      MatchTest.ListCons ]

                | _ -> []

            not tests.IsEmpty &&
            tests
            |> List.forall (fun test ->
               node.Pattern <- test, []
               rejected |> Seq.exists (MatchTest.matches wholeNode)
            )

        if allRejected then
            node.Pattern <- MatchTest.Discard false, []

            let duplicated = duplicate wholeNode
            rejected.Add(duplicated)

        else
            node.Pattern <- nodePattern

    let tryRecordRejectedDiscard (rejected: List<MatchNode>) (node: MatchNode) (context: ITreeNode) =
        // todo: don't duplicate unless there's some last case node
        let node = duplicate node
        let wholeNode = node

        let rec loop (node: MatchNode) =
            match node.Pattern with
            | MatchTest.Value value, _ ->
                match node.Value.Type with
                | MatchType.Bool _ ->
                    match value.BoolValue with
                    | false -> tryRejectDiscardPattern rejected wholeNode node context
                    | _ -> ()

                | MatchType.Enum(_, fields) ->
                    let index = fields |> Array.findIndex (snd >> (=) value)
                    if index = fields.Length - 1 then
                        tryRejectDiscardPattern rejected wholeNode node context

                | _ -> failwith "todo"

            | MatchTest.Tuple _, nodes ->
                nodes |> List.iter loop
                tryRejectDiscardPattern rejected wholeNode node context

            | MatchTest.Union caseIndex, [caseNode] ->
                match node.Value.Type with
                | MatchType.Union unionInstance ->
                    loop caseNode
                    if caseIndex = unionInstance.Entity.UnionCases.Count - 1 then
                        tryRejectDiscardPattern rejected wholeNode node context

                | _ -> failwith "todo"

            | MatchTest.ListCons, nodes
            | MatchTest.UnionCase, nodes
            | MatchTest.Record, nodes ->
                nodes |> List.iter loop
                tryRejectDiscardPattern rejected wholeNode node context

            | MatchTest.Field _, [node] ->
                loop node

            | _ -> ()

        loop node

    /// Return true if successfully incremented the value
    let rec increment (deconstructions: Deconstructions) (context: ITreeNode) isGenerating (node: MatchNode) =
        match node.Pattern with
        | MatchTest.Value value, _ ->
            match node.Value.Type with
            | MatchType.Bool _ ->
                match value.BoolValue with
                | true ->
                    node.Pattern <- MatchTest.Value(ConstantValue.Bool(false, context.GetPsiModule())), []
                    true
                | _ ->
                    false

            | MatchType.Enum(_, fields) ->
                let index = fields |> Array.findIndex (snd >> (=) value)
                let nextValue =
                    fields
                    |> Array.skip index
                    |> Array.tail
                    |> Array.tryHead

                match nextValue with
                | Some(_, value) ->
                    node.Pattern <- MatchTest.Value(value), []
                    true
                | _ ->
                    false

            | _ -> failwith "todo"

        | MatchTest.Tuple _, nodes ->
            let changedIndex =
                nodes
                |> List.tryFindIndexBack (increment deconstructions context isGenerating)

            match changedIndex with
            | None ->
                false

            | Some index ->
                nodes
                |> List.skip (index + 1)
                |> List.iter (fun node ->
                    node.Pattern <- MatchTest.initialPattern deconstructions context isGenerating node.Value
                )
                true

        | MatchTest.Discard _, _
        | MatchTest.Named _, _ ->
            false

        | MatchTest.ActivePatternCase(index, group), _ ->
            if index < group.Names.Count - 1 then
                node.Pattern <- MatchTest.ActivePatternCase(index + 1, group), []
                true
            else
                false

        | MatchTest.Union index, [caseNode] ->
            increment deconstructions context isGenerating caseNode ||

            match node.Value.Type with
            | MatchType.Union unionEntityInstance ->
                let unionCases = unionEntityInstance.Entity.UnionCases
                if index < unionCases.Count - 1 then
                    let newIndex = index + 1
                    let unionTest = MatchTest.Union newIndex
                    let unionPath = unionTest :: node.Value.Path
                    let caseMatchType = MatchType.UnionCase(unionCases[newIndex], unionEntityInstance)
                    let caseValue = { Type = caseMatchType; Path = unionPath }
                    let casePattern = MatchTest.initialPattern deconstructions context isGenerating caseValue

                    node.Pattern <- unionTest, [MatchNode.Create(caseValue, casePattern)]
                    true
                else
                    false

            | _ ->
                false

        | MatchTest.UnionCase, nodes
        | MatchTest.Record, nodes ->
            match List.tryFindIndexBack (increment deconstructions context isGenerating) nodes with
            | None -> false
            | Some index ->
                match MatchTest.initialPattern deconstructions context isGenerating node.Value with
                | MatchTest.UnionCase, newNodes
                | MatchTest.Record, newNodes ->
                    (nodes, newNodes)
                    ||> List.zip
                    |> List.skip (index + 1)
                    |> List.iter (fun (node, newNode) ->
                        node.Pattern <- newNode.Pattern
                    )
                    true

                | _ ->
                    false

        | MatchTest.Field _, [node] ->
            increment deconstructions context isGenerating node

        | MatchTest.EmptyList, _ ->
            if not isGenerating then false else

            let createListItemNode isHead =
                let nodeTest = MatchTest.ListCons
                let nodePath = nodeTest :: node.Value.Path

                let nodeType =
                    match isHead, node.Value.Type with
                    | true, MatchType.List itemFcsType -> MatchType.ofFcsType context itemFcsType
                    | _ -> node.Value.Type

                let nodeValue = { Type = nodeType; Path = nodePath }
                let nodePattern = MatchTest.initialPattern deconstructions context isGenerating nodeValue
                MatchNode.Create(nodeValue, nodePattern)

            let headNode = createListItemNode true
            let tailNode = createListItemNode false

            node.Pattern <- MatchTest.ListCons, [headNode; tailNode]
            true

        | MatchTest.ListCons, nodes ->
            // todo: increment inner pattern during generation
            if isGenerating then false else

            match List.tryFindIndexBack (increment deconstructions context isGenerating) nodes with
            | None ->
                node.Pattern <- MatchTest.EmptyList, []

            | Some index ->
                match index, node.Pattern with
                | 0, (_, [_; tail]) ->
                    tail.Pattern <- MatchTest.initialPattern deconstructions context isGenerating tail.Value
                | _ -> ()

            true

        | MatchTest.Null, [] ->
            node.Pattern <- MatchTest.Named None, []
            true

        | nodePattern -> failwith $"Unexpected pattern: {nodePattern}"

    let incrementAndTryReject (deconstructions: Deconstructions) (rejected: List<MatchNode>) (context: ITreeNode)
            (node: MatchNode) =
        tryRecordRejectedDiscard rejected node context
        increment deconstructions context false node


let getMatchExprMatchType (matchExpr: IMatchLikeExpr) : MatchType =
    match matchExpr with
    | :? IMatchExpr as matchExpr ->
        let expr = matchExpr.Expression
        if isNull expr then MatchType.Error else

        match expr with
        | :? ITupleExpr as tupleExpr ->
            let types =
                [| for expr in tupleExpr.Expressions -> expr.TryGetFcsType() |]
                |> Array.map (MatchType.ofFcsType matchExpr)
            MatchType.Tuple(tupleExpr.IsStruct, types)
        | _ ->
            let fcsType = expr.TryGetFcsType()
            MatchType.ofFcsType matchExpr fcsType

    | :? IMatchLambdaExpr as matchLambdaExpr ->
        // todo: fix substitution in FCS
        let startOffset = matchLambdaExpr.GetDocumentStartOffset()
        let fcsType = matchLambdaExpr.TryGetFcsType(DocumentRange(&startOffset))
        MatchType.ofFcsType matchExpr fcsType

    | _ ->
        MatchType.Error

let rec getMatchPattern (deconstructions: Deconstructions) (value: MatchValue) skipOnNull (pat: IFSharpPattern) =
    let addDeconstruction path deconstruction =
        deconstructions.Add(path, deconstruction)

    let getUnionCaseIndex (union: FcsEntityInstance) (unionCase: FSharpUnionCase) =
        let equals (t1: FSharpType) (t2: FSharpType) =
            let t1 = t1.ErasedType
            let t2 = t2.ErasedType

            t1.HasTypeDefinition = t2.HasTypeDefinition &&
            t1.TypeDefinition = t2.TypeDefinition

        if isNull unionCase || not (equals unionCase.ReturnType union.FcsType) then None else
        union.Entity.UnionCases |> Seq.tryFindIndex (fun uc -> uc.XmlDocSig = unionCase.XmlDocSig)

    let addTupleItemDeconstructions parentPath testCtor count =
        for i in 0 .. count do
            let itemPath = testCtor i :: parentPath
            addDeconstruction itemPath Deconstruction.InnerPatterns

    let makeFieldNode discardOnError parentPath substitution index (field: FSharpField) pat =
        let test = MatchTest.Field(index, field.DisplayNameCore)
        let path = test :: parentPath
        let fieldFcsType = field.FieldType.Instantiate(substitution)
        let fieldType = MatchType.ofFcsType pat fieldFcsType
        let itemValue = { Type = fieldType; Path = path }
        let matchPattern = getMatchPattern deconstructions itemValue discardOnError pat
        MatchNode.Create(itemValue, matchPattern)

    let makeNamedFieldPatternNodes parentPath substitution (fields: IList<FSharpField>) (fieldPatterns: IFieldPat seq) =
        let matchedPatterns = Array.create fields.Count null
        fieldPatterns
        |> Seq.iter (fun pat ->
            match pat.Reference.GetFcsSymbol() with
            | :? FSharpField as fcsField ->
                fields
                |> Seq.tryFindIndex ((=) fcsField)
                |> Option.iter (fun index ->
                    if isNull matchedPatterns[index] then
                        matchedPatterns[index] <- pat.Pattern)
            | _ -> ()
        )

        let fieldNodes =
            (fields, matchedPatterns)
            ||> Seq.mapi2 (makeFieldNode true parentPath substitution)
            |> List.ofSeq

        fieldNodes

    let makeListItemNode value isHead pat path =
        let nodeTest = MatchTest.ListConsItem isHead
        let nodePath = nodeTest :: path

        let nodeType = MatchType.getListNodeType pat isHead value
        let nodeValue = { Type = nodeType; Path = nodePath }
        let nodePattern = getMatchPattern deconstructions nodeValue skipOnNull pat
        MatchNode.Create(nodeValue, nodePattern)

    match pat.IgnoreInnerParens(), value.Type with
    | :? IWildPat, _ ->
        addDeconstruction value.Path (Deconstruction.Discard false)
        MatchTest.Discard false, []

    | :? ITuplePat as tuplePat, MatchType.Tuple(_, types) ->
        addDeconstruction value.Path Deconstruction.InnerPatterns

        let pats = tuplePat.Patterns
        if pats.Count <> types.Length then
            addTupleItemDeconstructions value.Path MatchTest.TupleItem types.Length
            MatchTest.Error, [] else

        let itemNodes =
            (types, pats)
            ||> Seq.mapi2 (fun i itemType itemPat ->
                let itemTest = MatchTest.TupleItem i
                let itemPath = itemTest :: value.Path
                let itemValue = { Type = itemType; Path = itemPath }
                let matchPattern = getMatchPattern deconstructions itemValue false itemPat
                MatchNode.Create(itemValue, matchPattern)
            )
            |> List.ofSeq

        MatchTest.Tuple tuplePat.IsStruct, itemNodes

    | :? IAsPat as asPat, _ ->
        let leftPat = getMatchPattern deconstructions value skipOnNull asPat.LeftPattern
        let rightPat = getMatchPattern deconstructions value skipOnNull asPat.RightPattern

        // todo: value type may change
        // match obj with
        // | :? Union as (Union.CaseA _) -> ()

        let leftNode = MatchNode.Create(value, leftPat)
        let rightNode = MatchNode.Create(value, rightPat)
        MatchTest.As, [leftNode; rightNode]

    | _, MatchType.Tuple(_, types) ->
        addDeconstruction value.Path Deconstruction.InnerPatterns
        addTupleItemDeconstructions value.Path MatchTest.TupleItem types.Length

        MatchTest.Error, []

    | :? IConstPat as constPat, _ ->
        // todo: add test for bad value
        addDeconstruction value.Path Deconstruction.InnerPatterns
        MatchTest.Value constPat.ConstantValue, []

    | :? IReferencePat as refPat, _ ->
        let constantValue = refPat.ConstantValue
        if not (constantValue.IsErrorOrNonCompileTimeConstantValue()) then
            addDeconstruction value.Path Deconstruction.InnerPatterns
            MatchTest.Value constantValue, []

        elif refPat.IsDeclaration then
            let name = refPat.SourceName
            addDeconstruction value.Path (Deconstruction.Named name)
            MatchTest.Named(Some name), []

        else
            match refPat.Reference.GetFcsSymbol() with
            | :? FSharpActivePatternCase as case ->
                addDeconstruction value.Path (Deconstruction.ActivePattern case.Group)
                MatchTest.ActivePatternCase(case.Index, case.Group), []

            | :? FSharpUnionCase as unionCase ->
                match value.Type with
                | MatchType.Union unionEntityInstance ->
                    match getUnionCaseIndex unionEntityInstance unionCase with
                    | None -> MatchTest.Error, []
                    | Some index ->

                    addDeconstruction value.Path Deconstruction.InnerPatterns

                    let test = MatchTest.Union index
                    let path = test :: value.Path
                    let caseMatchType = MatchType.UnionCase(unionCase, unionEntityInstance)
                    let caseValue = { Type = caseMatchType; Path = path }

                    test, [MatchNode.Create(caseValue, (MatchTest.Discard false, []))]

                | _ ->
                    MatchTest.Error, []

            | _ ->
                MatchTest.Error, []

    | :? IParametersOwnerPat as paramOwnerPat, _ when
            isNotNull (paramOwnerPat.Reference.GetFcsSymbol().As<FSharpActivePatternCase>()) ->
        let case = paramOwnerPat.Reference.GetFcsSymbol().As<FSharpActivePatternCase>()
        addDeconstruction value.Path (Deconstruction.ActivePattern case.Group)

        let patternNodes = 
            paramOwnerPat.ParametersEnumerable
            |> Seq.tryLast
            |> Option.map (getMatchPattern deconstructions value skipOnNull)
            |> Option.map (fun pattern -> MatchNode.Create(value, pattern))
            |> Option.toList

        MatchTest.ActivePatternCase(case.Index, case.Group), patternNodes

    | :? IParametersOwnerPat as paramOwnerPat, MatchType.Union unionEntityInstance ->
        let unionCase = paramOwnerPat.Reference.GetFcsSymbol().As<FSharpUnionCase>()
        match getUnionCaseIndex unionEntityInstance unionCase with
        | None -> MatchTest.Error, []
        | Some index ->

        addDeconstruction value.Path Deconstruction.InnerPatterns

        let unionTest = MatchTest.Union index
        let unionPath = unionTest :: value.Path
        let caseMatchType = MatchType.UnionCase(unionCase, unionEntityInstance)
        let caseValue = { Type = caseMatchType; Path = unionPath }
        let caseTest = MatchTest.UnionCase
        let casePath = caseTest :: caseValue.Path

        // todo: matching over case with no fields

        let substitution = unionEntityInstance.Substitution
        let makeSingleFieldNode pat =
            addDeconstruction casePath Deconstruction.InnerPatterns
            let fieldNode = makeFieldNode false casePath substitution 0 unionCase.Fields[0] pat

            if unionCase.Fields.Count <> 1 then MatchTest.Error, [] else
            MatchTest.UnionCase, [fieldNode]

        let innerPatterns =
            match paramOwnerPat.Parameters.SingleItem with
            | :? IWildPat -> MatchTest.Discard false, []

            | :? IParenPat as parenPat ->
                match parenPat.Pattern with
                | :? ITuplePat as tuplePat when not tuplePat.IsStruct ->
                    addDeconstruction casePath Deconstruction.InnerPatterns

                    let innerPatterns = tuplePat.Patterns
                    let caseFields = unionCase.Fields

                    let singleField = caseFields.SingleItem()
                    if isNotNull singleField && singleField.FieldType.Instantiate(substitution).IsTupleType then
                        // let fieldNode = makeFieldNode false casePath substitution 0 singleField tuplePat 
                        // caseTest, [fieldNode]
                        makeSingleFieldNode tuplePat
                    else
                        if innerPatterns.Count <> caseFields.Count then
                            let n = min innerPatterns.Count caseFields.Count
                            (Seq.take n caseFields, Seq.take n innerPatterns)
                            ||> Seq.mapi2 (makeFieldNode false casePath substitution)
                            |> List.ofSeq
                            |> ignore
                            MatchTest.Error, [] else

                        let fieldNodes = Seq.mapi2 (makeFieldNode false casePath substitution) caseFields innerPatterns |> List.ofSeq
                        caseTest, fieldNodes

                | pat -> makeSingleFieldNode pat

            | :? INamedUnionCaseFieldsPat as namedPatListPat ->
                addDeconstruction casePath Deconstruction.InnerPatterns

                let fieldPatterns = Array.create unionCase.Fields.Count null
                namedPatListPat.FieldPatterns
                |> Seq.iter (fun pat ->
                    match pat.Reference.GetFcsSymbol() with
                    | :? FSharpField as fcsField ->
                        unionCase.Fields
                        |> Seq.tryFindIndex ((=) fcsField)
                        |> Option.iter (fun index ->
                            if isNull fieldPatterns[index] then
                                fieldPatterns[index] <- pat.Pattern)
                    | _ -> ()
                )

                let fieldNodes =
                    unionCase.Fields
                    |> Seq.mapi (fun i fcsField -> makeFieldNode true casePath substitution i fcsField fieldPatterns[i])
                    |> List.ofSeq

                MatchTest.UnionCase, fieldNodes

            | pat -> makeSingleFieldNode pat

        MatchTest.Union index, [MatchNode.Create(caseValue, innerPatterns)]

    | :? IListPat as listPat, MatchType.List _ ->
        addDeconstruction value.Path Deconstruction.InnerPatterns

        let listItemPats = listPat.Patterns
        if Seq.isEmpty listItemPats then MatchTest.EmptyList, [] else

        let listTest = MatchTest.ListCons
        let listNode = MatchNode.Create(value, (listTest, []))

        let listPath = listTest :: value.Path
        addDeconstruction listPath Deconstruction.InnerPatterns

        let lastNode =
            (listNode, listItemPats)
            ||> Seq.fold (fun node pat ->
                addDeconstruction node.Value.Path Deconstruction.InnerPatterns

                let listPath = listTest :: node.Value.Path
                let headNode = makeListItemNode value true pat listPath

                let tailNode =
                    let tailPath = MatchTest.ListConsItem false :: listPath
                    let nodeValue = { Type = node.Value.Type; Path = tailPath }
                    let nodePattern = MatchTest.Discard false, []
                    MatchNode.Create(nodeValue, nodePattern)

                node.Pattern <- listTest, [headNode; tailNode]
                tailNode
            )

        lastNode.Pattern <- MatchTest.EmptyList, []

        listNode.Pattern

    | :? IListConsPat as listConsPat, MatchType.List _ ->
        addDeconstruction value.Path Deconstruction.InnerPatterns

        let listTest = MatchTest.ListCons
        let listPath = listTest :: value.Path
        addDeconstruction listPath Deconstruction.InnerPatterns

        let headNode = makeListItemNode value true listConsPat.HeadPattern listPath
        let tailNode = makeListItemNode value false listConsPat.TailPattern listPath

        listTest, [headNode; tailNode]

    | :? IAndsPat as andsPat, _ ->
        let nodes =
            andsPat.Patterns
            |> Seq.map (fun pat -> MatchNode.Create(value, getMatchPattern deconstructions value false pat))
            |> List.ofSeq
        MatchTest.And, nodes

    | :? IOrPat as orPat, _ ->
        let nodes =
            orPat.Patterns
            |> Seq.map (fun pat -> MatchNode.Create(value, getMatchPattern deconstructions value false pat))
            |> List.ofSeq
        MatchTest.Or, nodes

    | :? IRecordPat as recordPat, MatchType.Record recordEntityInstance ->
        addDeconstruction value.Path Deconstruction.InnerPatterns

        let test = MatchTest.Record
        let recordPath = test :: value.Path

        addDeconstruction recordPath Deconstruction.InnerPatterns

        let fields = recordEntityInstance.Entity.FSharpFields
        let substitution = recordEntityInstance.Substitution

        let fieldNodes = makeNamedFieldPatternNodes recordPath substitution fields recordPat.FieldPatterns
        test, fieldNodes

    | :? IFieldPat as fieldPat, _ ->
        getMatchPattern deconstructions value skipOnNull fieldPat.Pattern

    | :? INullPat, _ ->
        MatchTest.Null, []

    | null, _ when skipOnNull ->
        addDeconstruction value.Path (Deconstruction.Discard true)
        MatchTest.Discard true, []

    | _ -> MatchTest.Error, []

let ofMatchClause (value: MatchValue) (matchClause: IMatchClause) =
    let deconstructions = OneToListMap()
    getMatchPattern deconstructions value false matchClause.Pattern

let getMatchValue (matchExpr: IMatchLikeExpr) =
    let matchType = getMatchExprMatchType matchExpr
    { Type = matchType; Path = [] }

let ofMatchExpr (matchExpr: IMatchLikeExpr) =
    let matchValue = getMatchValue matchExpr

    let matchNodes = List()
    let deconstructions = OneToListMap()

    for clause in matchExpr.ClausesEnumerable do
        if isNull clause.Pattern then () else

        let pattern = getMatchPattern deconstructions matchValue false clause.Pattern
        if isNull clause.WhenExpressionClause then
            matchNodes.Add(MatchNode.Create(matchValue, pattern))

    matchValue, matchNodes, deconstructions

let generateClauses (matchExpr: IMatchLikeExpr) value nodes deconstructions =
    let factory = matchExpr.CreateElementFactory()

    let lineEnding = matchExpr.GetLineEnding()
    let unitExpr = factory.CreateExpr("()")
    let sandBoxMatchExpr = factory.CreateMatchExpr(unitExpr)
    ModificationUtil.DeleteChildRange(TreeRange(sandBoxMatchExpr.WithKeyword.NextSibling, sandBoxMatchExpr.LastChild))

    let bindContext = MatchNode.BindContext.Create(matchExpr)

    let createMatchClause =
        let matchClause = factory.CreateMatchClause()
        fun () ->
            matchClause.Copy()

    let tryAddClause (node: MatchNode) =
        if nodes |> Seq.exists (MatchTest.matches node) then () else

        let matchClause = ModificationUtil.AddChildAfter(sandBoxMatchExpr.LastChild, createMatchClause ())
        matchClause.AddLineBreakBefore() |> ignore
        let usedNames = HashSet()
        MatchNode.bind bindContext usedNames matchClause.Pattern node

    let matchPattern = MatchTest.initialPattern deconstructions matchExpr true value
    let node = MatchNode.Create(value, matchPattern)

    let firstClause = Seq.tryHead matchExpr.ClausesEnumerable
    let lastClause = Seq.tryLast matchExpr.ClausesEnumerable

    lastClause |> Option.iter MatchExprUtil.addIndent

    tryAddClause node
    while MatchNode.increment deconstructions matchExpr true node do
        tryAddClause node

    moveCommentsAndWhitespaceInside matchExpr

    let tempMatchClause = ModificationUtil.AddChild(matchExpr, bindContext.Factory.CreateMatchClause())
    tempMatchClause.AddLineBreakBefore() |> ignore

    for fcsEntity in bindContext.SeenTypes do
        let case: FSharpSymbol option = 
            if fcsEntity.IsFSharpUnion then
                fcsEntity.UnionCases
                |> Seq.tryHead
                |> Option.map (fun case -> case :> FSharpSymbol)
            elif fcsEntity.IsEnum then
                fcsEntity.FSharpFields
                |> Seq.tryFind (fun fcsField -> fcsField.LiteralValue.IsSome)
                |> Option.map (fun case -> case :> FSharpSymbol)
            else
                None

        case
        |> Option.iter (fun fcsSymbol ->
            FSharpBindUtil.bindFcsSymbol tempMatchClause.Pattern fcsSymbol "get pattern" |> ignore
        )

    let templClauseRange = TreeRange(tempMatchClause)
    for clause in sandBoxMatchExpr.ClausesEnumerable do
        let matchClause = ModificationUtil.AddChild(matchExpr, clause)
        matchClause.AddLineBreakBefore() |> ignore

    ModificationUtil.DeleteChild(tempMatchClause)


let markTopLevelDeconstructions (deconstructions: Deconstructions) (value: MatchValue) =
    deconstructions.Add(value.Path, Deconstruction.InnerPatterns)

    match value.Type with
    | MatchType.Tuple(_, matchTypes) ->
        matchTypes
        |> Array.iteri (fun i _ ->
            let itemPath = MatchTest.TupleItem i :: value.Path
            deconstructions.Add(itemPath, Deconstruction.InnerPatterns)
        )

    | _ ->
        ()

let rec tryNavigatePatternPath (path: PatternParentTraverseStep list) (node: MatchNode) =
    let rec tryGetListItem node index =
        if index < 0 then None else
        if index = 0 then Some node else

        match node.Pattern with
        | MatchTest.ListCons, [_; tail] -> tryGetListItem tail (index - 1)
        | _ -> None

    match node.Pattern, path with
    | _, [] -> Some node

    | (MatchTest.Union _, [{ Pattern = (MatchTest.UnionCase, fieldNodes) }]), PatternParentTraverseStep.ParameterOwner _ :: path ->
        match fieldNodes, path with
        | fieldNode :: _, [] -> Some fieldNode
        | [node], _ -> tryNavigatePatternPath path node 
        | _ -> None

    | (MatchTest.Tuple false, nodes), PatternParentTraverseStep.Tuple(index, _) :: path
    | (MatchTest.Or, nodes), PatternParentTraverseStep.Or(index, _) :: path
    | (MatchTest.And, nodes), PatternParentTraverseStep.And(index, _) :: path
    | (MatchTest.ListCons, nodes), PatternParentTraverseStep.ListCons(index, _) :: path ->
        List.tryItem index nodes |> Option.bind (tryNavigatePatternPath path)

    | (MatchTest.ListCons, _), PatternParentTraverseStep.List(index, _) :: path ->
        tryGetListItem node index |> Option.bind (tryNavigatePatternPath path)

    // | (MatchTest.Record, fieldNodes), PatternParentTraverseStep.Record _ :: PatternParentTraverseStep.Field(name, _) :: _ ->
    //     let matchNodeOption =
    //         fieldNodes |> List.tryFind (fun node ->
    //             match node.Pattern with
    //             | MatchTest.Named(Some testName), _ -> testName = name
    //             | _ -> false
    //         )
    //     None

    | _ -> None
    
