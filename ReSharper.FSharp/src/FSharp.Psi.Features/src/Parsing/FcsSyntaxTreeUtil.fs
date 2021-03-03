[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing.FcsSyntaxTreeUtil

open FSharp.Compiler.Syntax
open FSharp.Compiler.Text

type SynBinding with
    member x.StartPos =
        let (SynBinding(headPat = headPat)) = x
        headPat.Range.Start

type SynField with
    member x.StartPos =
        let (SynField(attrs, _, id, _, _, _, _, range)) = x
        let range =
            match attrs, id with
            | attrList :: _, _ -> attrList.Range
            | _, Some id -> id.idRange
            | _ -> range
        range.Start

type SynMemberDefn with
    member x.OuterAttributes =
        match x with
        | SynMemberDefn.Member(SynBinding(attributes = attrs), _)
        | SynMemberDefn.AbstractSlot(SynValSig(attributes = attrs), _, _)
        | SynMemberDefn.AutoProperty(attributes = attrs)
        | SynMemberDefn.ValField(SynField(attributes = attrs), _) -> attrs

        | SynMemberDefn.LetBindings(SynBinding(attributes = attrs) :: _, _, _, range) ->
            match attrs with
            | [] -> []
            | head :: _ ->

            let letStart = range.Start
            if Position.posGeq head.Range.Start letStart then attrs else  
            attrs |> List.takeWhile (fun attrList -> Position.posLt attrList.Range.Start letStart)

        | _ -> []

type SynMemberSig with
    member x.OuterAttributes =
        match x with
        | SynMemberSig.Member(SynValSig(attributes = attrs), _, _)
        | SynMemberSig.ValField(SynField(attributes = attrs), _) -> attrs
        | _ -> []

    member x.Range =
        match x with
        | SynMemberSig.Member(_, _, range)
        | SynMemberSig.ValField(_, range)
        | SynMemberSig.Inherit(_, range)
        | SynMemberSig.Interface(_, range) -> range
        | _ -> range.Zero

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


let attrOwnerStartPos (attrLists: SynAttributeList list) (ownerRange: range) =
    match attrLists with
    | { Range = attrsRange } :: _ ->
        let attrsStart = attrsRange.Start
        if Position.posLt attrsStart ownerRange.Start then attrsStart else ownerRange.Start
    | _ -> ownerRange.Start

let typeDefnGroupStartPos (bindings: SynTypeDefn list) (range: Range) =
    match bindings with
    | SynTypeDefn(SynComponentInfo(attributes = attrs), _, _, _, _) :: _ -> attrOwnerStartPos attrs range
    | _ -> range.Start

let typeSigGroupStartPos (bindings: SynTypeDefnSig list) (range: Range) =
    match bindings with
    | SynTypeDefnSig(SynComponentInfo(attributes = attrs), _, _, _) :: _ -> attrOwnerStartPos attrs range
    | _ -> range.Start

let letBindingGroupStartPos (bindings: SynBinding list) (range: Range) =
    match bindings with
    | SynBinding(attributes = attrs) :: _ -> attrOwnerStartPos attrs range
    | _ -> range.Start


let rec skipGeneratedLambdas expr =
    match expr with
    | SynExpr.Lambda(_, true, _, bodyExpr, _, _) ->
        skipGeneratedLambdas bodyExpr
    | _ -> expr

and skipGeneratedMatch expr =
    match expr with
    | SynExpr.Match(_, _, [ SynMatchClause(_, _, innerExpr, _, _) as clause ], matchRange) when
            matchRange.Start = clause.Range.Start ->
        skipGeneratedMatch innerExpr
    | _ -> expr

let inline getLambdaBodyExpr expr =
    let skippedLambdas = skipGeneratedLambdas expr
    skipGeneratedMatch skippedLambdas

let getGeneratedAppArg (expr: SynExpr) =
    if not expr.Range.IsSynthetic then expr else

    match expr with
    | SynExpr.App(_, false, func, arg, _) when func.Range.IsSynthetic -> arg
    | _ -> expr

type SynArgPats with
    member x.IsEmpty =
        match x with
        | SynArgPats.Pats pats -> pats.IsEmpty
        | SynArgPats.NamePatPairs(idsAndPats, _) -> idsAndPats.IsEmpty
