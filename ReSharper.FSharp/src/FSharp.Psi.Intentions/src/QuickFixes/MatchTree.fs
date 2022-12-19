module JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes.MatchTree

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

// todo: isInstPat

[<RequireQualifiedAccess>]
type MatchType =
    | Bool of fcsType: FSharpType
    | Enum of enumEntity: FSharpEntity * fields: (FSharpField * ConstantValue) array
    | Tuple of isStruct: bool * types: MatchType array
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
        | Union fcsEntityInstance -> Some fcsEntityInstance.FcsType
        | Enum(fcsEntity, _) -> Some(fcsEntity.AsType())
        | _ -> None


[<RequireQualifiedAccess>]
type MatchTest =
    | Discard
    | Named of name: string option
    | Value of ConstantValue
    | Tuple of isStruct: bool
    | TupleItem of index: int
    | Union of index: int
    | UnionCase
    | UnionCaseField of index: int
    | List of isEmpty: bool
    | And
    | Or
    | Error
    | ActivePatternCase of index: int * group: FSharpActivePatternGroup

and MatchValue =
    { Type: MatchType
      Path: MatchTest list }

and MatchNode =
    { Value: MatchValue
      mutable Pattern: Pattern }

    static member Create(value, pattern) =
        { Value = value
          Pattern = pattern }

and Pattern = MatchTest * MatchNode list

[<RequireQualifiedAccess>]
type Deconstruction =
    | Discard
    | Named of name: string
    | ActivePattern of activePattern: FSharpActivePatternGroup
    | InnerPatterns

type Deconstructions = IDictionary<MatchTest list, Deconstruction>


module MatchType =
    let getFieldConstantValue (context: ITreeNode) (fcsField: FSharpField) =
        fcsField.LiteralValue
        |> Option.map (fun value ->
            ConstantValue.Create(value, fcsField.FieldType.MapType(context))
        )

    let rec ofFcsType (context: ITreeNode) (fcsType: FSharpType) =
        if isNull fcsType then MatchType.Error else

        if fcsType.IsTupleType then createTupleType context false fcsType.GenericArguments else
        if fcsType.IsStructTupleType then createTupleType context true fcsType.GenericArguments else

        if not fcsType.StrippedType.HasTypeDefinition then MatchType.Other fcsType else

        let fcsEntity = fcsType.StrippedType.TypeDefinition

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

        let entityFqn = fcsEntity.BasicQualifiedName
        match entityFqn with
        | "System.Boolean" -> MatchType.Bool fcsType
        | "Microsoft.FSharp.Collections.FSharpList`1" -> MatchType.List fcsType
        | _ ->

        if fcsEntity.IsFSharpUnion then MatchType.Union(FcsEntityInstance.create fcsType) else

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


module MatchTest =
    let rec matches (node: MatchNode) (existingNode: MatchNode) : bool =
        match existingNode.Pattern, node.Pattern with
        | (MatchTest.Discard, _), _ -> true
        | (MatchTest.Named _, _), _ -> true
        | (MatchTest.Value existingValue, _), (MatchTest.Value value, _) -> existingValue.Equals(value)

        | (MatchTest.ActivePatternCase(existingIndex, existingGroup), _), (MatchTest.ActivePatternCase(index, group), _) ->
            existingIndex = index &&
            existingGroup.Name = group.Name &&
            existingGroup.DeclaringEntity = group.DeclaringEntity

        // todo: add test with different unions
        | (MatchTest.Union existingIndex, [node1]), (MatchTest.Union index, [node2]) ->
            existingIndex = index &&
            matches node2 node1

        | (MatchTest.UnionCase, [{ Pattern = MatchTest.Discard, _ }]), (MatchTest.UnionCase, _) -> true

        | (MatchTest.UnionCase, fields1), (MatchTest.UnionCase, fields2) ->
            List.forall2 matches fields2 fields1

        | (MatchTest.UnionCaseField _, [node1]), (MatchTest.UnionCaseField _, [node2]) ->
            matches node2 node1

        // todo: add test with different lengths
        | (MatchTest.Tuple isStruct1, nodes1), (MatchTest.Tuple isStruct2, nodes2) ->
            isStruct1 = isStruct2 &&
            List.forall2 matches nodes2 nodes1 

        | (MatchTest.List true, _), (MatchTest.List true, _) -> true

        | (MatchTest.Or, nodes), _ -> List.exists (matches node) nodes

        | _ -> false

    let rec initialPattern (deconstructions: Deconstructions) (context: ITreeNode) (value: MatchValue) =
        match deconstructions.TryGetValue(value.Path) with
        | true, Deconstruction.Discard -> MatchTest.Discard, []
        | true, Deconstruction.Named name -> MatchTest.Named(Some name), []
        | true, Deconstruction.ActivePattern group -> MatchTest.ActivePatternCase(0, group), []

        | true, Deconstruction.InnerPatterns ->
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
                        let matchPattern = initialPattern deconstructions context itemValue
                        MatchNode.Create(itemValue, matchPattern)
                    )
                    |> List.ofSeq
                MatchTest.Tuple isStruct, nodes

            | MatchType.Union unionEntityInstance ->
                let unionCase = unionEntityInstance.Entity.UnionCases[0]

                let test = MatchTest.Union 0
                let path = test :: value.Path
                let caseMatchType = MatchType.UnionCase(unionCase, unionEntityInstance)
                let caseValue = { Type = caseMatchType; Path = path }

                let casePattern =
                    if not unionCase.HasFields then MatchTest.Discard, [] else
                    initialPattern deconstructions context caseValue

                MatchTest.Union 0, [MatchNode.Create(caseValue, casePattern)]

            | MatchType.UnionCase(unionCase, unionEntityInstance) ->
                let fieldNodes =
                    unionCase.Fields
                    |> Seq.mapi (fun i field ->
                        let test = MatchTest.UnionCaseField i
                        let path = test :: value.Path

                        let fieldFcsType = field.FieldType.Instantiate(unionEntityInstance.Substitution)
                        let fieldType = MatchType.ofFcsType context fieldFcsType
                        let itemValue = { Type = fieldType; Path = path }
                        let matchPattern = initialPattern deconstructions context itemValue
                        MatchNode.Create(itemValue, matchPattern)
                    )
                    |> List.ofSeq

                MatchTest.UnionCase, fieldNodes

            | MatchType.List _ ->
                MatchTest.List true, []

            | _ -> failwith "todo"

        | _ ->
            match value.Type with
            | MatchType.UnionCase(unionCase, unionEntityInstance) ->
                let fields = unionCase.Fields
                let isSingleField = fields.Count = 1

                let caseTest = MatchTest.UnionCase
                let casePath = caseTest :: value.Path

                let fieldNodes =
                    fields
                    |> Seq.mapi (fun i field ->
                        let fieldTest = MatchTest.UnionCaseField i
                        let fieldPath = fieldTest :: casePath
                        let fieldFcsType = field.FieldType.Instantiate(unionEntityInstance.Substitution)
                        let fieldType = MatchType.ofFcsType context fieldFcsType
                        let fieldValue = { Type = fieldType; Path = fieldPath }
                        let fieldPattern =
                            if deconstructions.ContainsKey(fieldPath) then
                                initialPattern deconstructions context fieldValue
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
    let rec bind (usedNames: ISet<string>) (oldPat: IFSharpPattern) (node: MatchNode) =
        let factory = oldPat.CreateElementFactory()

        let replaceWithPattern existingPattern (newPat: IFSharpPattern) =
            let newPat = ModificationUtil.ReplaceChild(existingPattern, newPat)
            ParenPatUtil.addParensIfNeeded newPat

        let replaceTuplePat isStruct existingPattern nodes =
            let tupleItemsText = nodes |> List.map (fun _ -> "_") |> String.concat ", "
            let tuplePatText = if isStruct then $"struct ({tupleItemsText})" else tupleItemsText
            let tuplePat = factory.CreatePattern(tuplePatText, false)
            let tuplePat = replaceWithPattern existingPattern tuplePat :?> ITuplePat

            Seq.iter2 (bind usedNames) tuplePat.Patterns nodes

        match node.Pattern with
        | MatchTest.Named name, _ ->
            let names =
                // todo: ignore `value` name in option/voption
                let namesCollection = FSharpNamingService.createEmptyNamesCollection oldPat
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
                    usedNames.Add(name.RemoveBackticks()) |> ignore
                    name
                else
                    "_"

            factory.CreatePattern(name, false) |> replaceWithPattern oldPat |> ignore

        | MatchTest.ActivePatternCase(index, group), _ ->
            let text = FSharpNamingService.mangleNameIfNecessary group.Names[index]
            factory.CreatePattern(text, false) |> replaceWithPattern oldPat |> ignore

        | MatchTest.Tuple isStruct, nodes ->
            replaceTuplePat isStruct oldPat nodes

        | MatchTest.Union _, [{ Value = { Type = MatchType.UnionCase(unionCase, _) } } as node] ->
            let patText = if not unionCase.HasFields then unionCase.Name else $"{unionCase.Name} _"
            let pat = factory.CreatePattern(patText, false) |> replaceWithPattern oldPat :?> IReferenceNameOwnerPat
            FSharpPatternUtil.bindFcsSymbolToReference pat pat.ReferenceName unionCase "get pattern"

            if not unionCase.HasFields then () else

            let unionCasePat = pat :?> IParametersOwnerPat
            let paramsPat = unionCasePat.Parameters[0]

            match node.Pattern with
            | MatchTest.UnionCase, nodes ->
                match nodes with
                | [] -> ()
                | [node] -> bind usedNames paramsPat node
                | nodes -> replaceTuplePat false paramsPat nodes
            | _ -> ()

        | MatchTest.Value value, _ when value.IsBoolean() ->
            match value.BoolValue with
            | true -> factory.CreatePattern("true", false) |> replaceWithPattern oldPat |> ignore
            | false -> factory.CreatePattern("false", false) |> replaceWithPattern oldPat |> ignore

        | MatchTest.Value constantValue, _ ->
            match node.Value.Type with
            | MatchType.Enum(_, fields) ->
                let field, _ = fields |> Array.find (snd >> ((=) constantValue))
                let patText = field.DisplayNameCore
                let pat = factory.CreatePattern(patText, false) |> replaceWithPattern oldPat :?> IReferenceNameOwnerPat
                FSharpPatternUtil.bindFcsSymbolToReference pat pat.ReferenceName field "get pattern"

            | valueType -> failwith $"Unexpected value type: {valueType}"

        | MatchTest.List true, _ ->
            factory.CreatePattern("[]", false) |> replaceWithPattern oldPat |> ignore

        | _ -> ()

    /// Return true if successfully incremented the value
    let rec increment (deconstructions: Deconstructions) (context: ITreeNode) (node: MatchNode) =
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
                |> List.tryFindIndexBack (increment deconstructions context)

            match changedIndex with
            | None -> false
            | Some index ->
                nodes
                |> List.skip (index + 1)
                |> List.iter (fun node ->
                    node.Pattern <- MatchTest.initialPattern deconstructions context node.Value
                )
                true

        | MatchTest.Discard, _
        | MatchTest.Named _, _ ->
            false

        | MatchTest.ActivePatternCase(index, group), _ ->
            if index < group.Names.Count - 1 then
                node.Pattern <- MatchTest.ActivePatternCase(index + 1, group), []
                true
            else
                false

        | MatchTest.Union index, [caseNode] ->
            increment deconstructions context caseNode ||

            match node.Value.Type with
            | MatchType.Union unionEntityInstance ->
                let unionCases = unionEntityInstance.Entity.UnionCases
                if index < unionCases.Count - 1 then
                    let newIndex = index + 1
                    let unionTest = MatchTest.Union newIndex
                    let unionPath = unionTest :: node.Value.Path
                    let caseMatchType = MatchType.UnionCase(unionCases[newIndex], unionEntityInstance)
                    let caseValue = { Type = caseMatchType; Path = unionPath }
                    let casePattern = MatchTest.initialPattern deconstructions context caseValue

                    node.Pattern <- MatchTest.Union(newIndex), [MatchNode.Create(caseValue, casePattern)]
                    true
                else
                    false

            | _ ->
                false

        | MatchTest.UnionCase, nodes ->
            match List.tryFindIndexBack (increment deconstructions context) nodes with
            | None -> false
            | Some index ->
                nodes
                |> List.skip (index + 1)
                |> List.iter (fun node ->
                    node.Pattern <- MatchTest.initialPattern deconstructions context node.Value
                )
                true

        | MatchTest.UnionCaseField _, [node] ->
            increment deconstructions context node

        | MatchTest.List isEmpty, _ ->
            if isEmpty then
                node.Pattern <- MatchTest.Named None, []
                true
            else
                false

        | nodePattern -> failwith $"Unexpected pattern: {nodePattern}"


let getMatchExprMatchType (matchExpr: IMatchExpr) : MatchType =
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

let rec getMatchPattern (deconstructions: Deconstructions) (value: MatchValue) (pat: IFSharpPattern) discardOnError =
    let addDeconstruction path deconstruction =
        if not (deconstructions.ContainsKey(path)) then
            deconstructions[path] <- deconstruction

    let getUnionCaseIndex (union: FcsEntityInstance) (unionCase: FSharpUnionCase) =
        let equals (t1: FSharpType) (t2: FSharpType) =
            let t1 = t1.StrippedType
            let t2 = t2.StrippedType

            // todo: fix checking Equals in tests
            t1.HasTypeDefinition && t2.HasTypeDefinition &&
            t1.TypeDefinition.XmlDocSig = t2.TypeDefinition.XmlDocSig
    
        if isNull unionCase || not (equals unionCase.ReturnType union.FcsType) then None else
        union.Entity.UnionCases |> Seq.tryFindIndex (fun uc -> uc.XmlDocSig = unionCase.XmlDocSig)

    let addTupleItemDeconstructions parentPath testCtor count =
        for i in 0 .. count do
            let itemPath = testCtor i :: parentPath
            addDeconstruction itemPath Deconstruction.InnerPatterns

    match pat.IgnoreInnerParens(), value.Type with
    | :? IWildPat, _ ->
        addDeconstruction value.Path Deconstruction.Discard
        MatchTest.Discard, []

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
                let matchPattern = getMatchPattern deconstructions itemValue itemPat false
                MatchNode.Create(itemValue, matchPattern)
            )
            |> List.ofSeq

        MatchTest.Tuple tuplePat.IsStruct, itemNodes

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

                    MatchTest.Union index, [MatchNode.Create(caseValue, (MatchTest.Discard, []))]

                | _ ->
                    MatchTest.Error, []

            | _ ->
                MatchTest.Error, []

    | :? IParametersOwnerPat as paramOwnerPat, MatchType.Union unionEntityInstance ->
        let unionCase = paramOwnerPat.Reference.GetFcsSymbol().As<FSharpUnionCase>()
        match getUnionCaseIndex unionEntityInstance unionCase with
        | None -> MatchTest.Error, []
        | Some index ->

        addDeconstruction value.Path Deconstruction.InnerPatterns

        let test = MatchTest.Union index
        let unionPath = test :: value.Path
        let caseMatchType = MatchType.UnionCase(unionCase, unionEntityInstance)
        let caseValue = { Type = caseMatchType; Path = unionPath }
        let caseTest = MatchTest.UnionCase
        let casePath = caseTest :: caseValue.Path

        let makeFieldNode discardOnError index (field: FSharpField) pat =
            let test = MatchTest.UnionCaseField index
            let path = test :: casePath
            let fieldFcsType = field.FieldType.Instantiate(unionEntityInstance.Substitution)
            let fieldType = MatchType.ofFcsType pat fieldFcsType
            let itemValue = { Type = fieldType; Path = path }
            let matchPattern = getMatchPattern deconstructions itemValue pat discardOnError
            MatchNode.Create(itemValue, matchPattern)

        let makeSingleFieldNode pat =
            addDeconstruction casePath Deconstruction.InnerPatterns
            let fieldNode = makeFieldNode false 0 unionCase.Fields[0] pat

            if unionCase.Fields.Count <> 1 then MatchTest.Error, [] else
            MatchTest.UnionCase, [fieldNode]

        let innerPatterns =
            match paramOwnerPat.Parameters.SingleItem with
            | :? IWildPat -> MatchTest.Discard, []

            | :? IParenPat as parenPat ->
                match parenPat.Pattern with
                | :? ITuplePat as tuplePat when not tuplePat.IsStruct ->
                    addDeconstruction casePath Deconstruction.InnerPatterns

                    let innerPatterns = tuplePat.Patterns
                    let caseFields = unionCase.Fields
                    if innerPatterns.Count <> caseFields.Count then
                        let n = min innerPatterns.Count caseFields.Count
                        (Seq.take n caseFields, Seq.take n innerPatterns)
                        ||> Seq.mapi2 (makeFieldNode false)
                        |> List.ofSeq
                        |> ignore
                        MatchTest.Error, [] else

                    let fieldNodes = Seq.mapi2 (makeFieldNode false) caseFields innerPatterns |> List.ofSeq
                    caseTest, fieldNodes

                | pat -> makeSingleFieldNode pat

            | :? INamedPatternsListPat as namedPatListPat ->
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
                    |> Seq.mapi (fun i fcsField -> makeFieldNode true i fcsField fieldPatterns[i])
                    |> List.ofSeq

                MatchTest.UnionCase, fieldNodes

            | pat -> makeSingleFieldNode pat

        MatchTest.Union index, [MatchNode.Create(caseValue, innerPatterns)]

    | :? IListPat as listPat, MatchType.List _ ->
        addDeconstruction value.Path Deconstruction.InnerPatterns

        let isEmpty = Seq.isEmpty listPat.PatternsEnumerable
        MatchTest.List isEmpty, []

    | :? IListConsPat, MatchType.List _ ->
        addDeconstruction value.Path Deconstruction.InnerPatterns

        MatchTest.List false, []

    | :? IAndsPat as andsPat, _ ->
        let nodes = 
            andsPat.Patterns
            |> Seq.map (fun pat -> MatchNode.Create(value, getMatchPattern deconstructions value pat false))
            |> List.ofSeq
        MatchTest.And, nodes

    | :? IOrPat as orPat, _ ->
        let nodes = 
            orPat.Patterns
            |> Seq.map (fun pat -> MatchNode.Create(value, getMatchPattern deconstructions value pat false))
            |> List.ofSeq
        MatchTest.Or, nodes

    | _ -> MatchTest.Error, []

let ofMatchExpr (matchExpr: IMatchExpr) =
    let matchType = getMatchExprMatchType matchExpr
    let matchValue = { Type = matchType; Path = [] }

    let matchNodes = List()
    let deconstructions = Dictionary()

    for clause in matchExpr.ClausesEnumerable do
        if isNull clause.Pattern then () else

        let pattern = getMatchPattern deconstructions matchValue clause.Pattern false
        if isNull clause.WhenExpressionClause then
            matchNodes.Add(MatchNode.Create(matchValue, pattern))

    matchValue, matchNodes, deconstructions
