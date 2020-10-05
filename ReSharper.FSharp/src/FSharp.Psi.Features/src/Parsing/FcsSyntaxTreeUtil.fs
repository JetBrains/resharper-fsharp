[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing.FcsSyntaxTreeUtil

open FSharp.Compiler
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.Range

type SynBinding with
    member x.StartPos =
        let (Binding(headPat = headPat)) = x
        headPat.Range.Start

type SynField with
    member x.StartPos =
        let (Field(attrs, _, id, _, _, _, _, range)) = x
        let range =
            match attrs, id with
            | attrList :: _, _ -> attrList.Range
            | _, Some id -> id.idRange
            | _ -> range
        range.Start

type SynMemberDefn with
    member x.OuterAttributes =
        match x with
        | SynMemberDefn.Member(Binding(attributes = attrs), _)
        | SynMemberDefn.AbstractSlot(ValSpfn(attributes = attrs), _, _)
        | SynMemberDefn.AutoProperty(attributes = attrs)
        | SynMemberDefn.ValField(Field(attributes = attrs), _) -> attrs

        | SynMemberDefn.LetBindings(Binding(attributes = attrs) :: _, _, _, range) ->
            match attrs with
            | [] -> []
            | head :: _ ->

            let letStart = range.Start
            if posGeq head.Range.Start letStart then attrs else  
            attrs |> List.takeWhile (fun attrList -> posLt attrList.Range.Start letStart)

        | _ -> []

type SynSimplePats with
    member x.Range =
        match x with
        | SynSimplePats.SimplePats(range = range)
        | SynSimplePats.Typed(range = range) -> range

type SynSimplePat with
    member x.Range =
        match x with
        | SynSimplePat.Id(range = range)
        | SynSimplePat.Typed(range = range)
        | SynSimplePat.Attrib(range = range) -> range


let attrOwnerStartPos (attrLists: SynAttributeList list) (ownerRange: Range.range) =
    match attrLists with
    | { Range = attrsRange } :: _ ->
        let attrsStart = attrsRange.Start
        if posLt attrsStart ownerRange.Start then attrsStart else ownerRange.Start
    | _ -> ownerRange.Start

let typeDefnGroupStartPos (bindings: SynTypeDefn list) (range: Range.range) =
    match bindings with
    | TypeDefn(ComponentInfo(attributes = attrs), _, _, _) :: _ -> attrOwnerStartPos attrs range
    | _ -> range.Start

let typeSigGroupStartPos (bindings: SynTypeDefnSig list) (range: Range.range) =
    match bindings with
    | TypeDefnSig(ComponentInfo(attributes = attrs), _, _, _) :: _ -> attrOwnerStartPos attrs range
    | _ -> range.Start

let letBindingGroupStartPos (bindings: SynBinding list) (range: Range.range) =
    match bindings with
    | Binding(attributes = attrs) :: _ -> attrOwnerStartPos attrs range
    | _ -> range.Start


let rec skipGeneratedLambdas expr =
    match expr with
    | SynExpr.Lambda(_, true, _, bodyExpr, _) ->
        skipGeneratedLambdas bodyExpr
    | _ -> expr

and skipGeneratedMatch expr =
    match expr with
    | SynExpr.Match(_, _, [ Clause(_, _, innerExpr, _, _) as clause ], matchRange) when
            matchRange.Start = clause.Range.Start ->
        skipGeneratedMatch innerExpr
    | _ -> expr

let inline getLambdaBodyExpr expr =
    let skippedLambdas = skipGeneratedLambdas expr
    skipGeneratedMatch skippedLambdas


let rec getGeneratedLambdaParam dflt expr =
    match expr with
    | SynExpr.Lambda(_, true, pats, bodyExpr, _) ->
        getGeneratedLambdaParam pats bodyExpr
    | _ -> dflt

let getLastLambdaParam expr =
    match expr with
    | SynExpr.Lambda(_, _, pats, bodyExpr, _) -> getGeneratedLambdaParam pats bodyExpr
    | _ -> failwithf "Expecting lambda expression, got:\n%A" expr

let getGeneratedAppArg (expr: SynExpr) =
    if not expr.Range.IsSynthetic then expr else

    match expr with
    | SynExpr.App(_, false, func, arg, _) when func.Range.IsSynthetic -> arg
    | _ -> expr

type SynArgPats with
    member x.IsEmpty =
        match x with
        | Pats pats -> pats.IsEmpty
        | NamePatPairs(idsAndPats, _) -> idsAndPats.IsEmpty
