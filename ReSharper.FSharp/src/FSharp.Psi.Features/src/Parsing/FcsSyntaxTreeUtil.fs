[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing.FcsSyntaxTreeUtil

open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open JetBrains.ReSharper.Plugins.FSharp.Util

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

    member x.XmlDoc =
        let (SynField(_, _, _, _, _, XmlDoc xmlDoc, _, _)) = x
        xmlDoc

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

    member x.XmlDoc =
        match x with
        | SynMemberDefn.Member(SynBinding(xmlDoc = xmlDoc), _)
        | SynMemberDefn.ImplicitCtor(xmlDoc = xmlDoc)
        | SynMemberDefn.LetBindings(SynBinding(xmlDoc = xmlDoc) :: _, _, _, _)
        | SynMemberDefn.AbstractSlot(SynValSig(xmlDoc = xmlDoc), _, _)
        | SynMemberDefn.ValField(SynField(xmlDoc = xmlDoc), _)
        | SynMemberDefn.AutoProperty(xmlDoc = xmlDoc) -> xmlDoc.ToXmlDoc(false, None)
        | _ -> XmlDoc.Empty

type SynMemberSig with
    member x.OuterAttributes =
        match x with
        | SynMemberSig.Member(SynValSig(attributes = attrs), _, _)
        | SynMemberSig.ValField(SynField(attributes = attrs), _) -> attrs
        | _ -> []

    member x.XmlDoc =
        match x with
        | SynMemberSig.Member(SynValSig(xmlDoc = xmlDoc), _, _)
        | SynMemberSig.ValField(SynField(xmlDoc = xmlDoc), _) -> xmlDoc.ToXmlDoc(false, None)
        | _ -> XmlDoc.Empty

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


let posMin pos1 pos2 =
    if Position.posLt pos1 pos2 then pos1 else pos2

let rangeStartPosMin (range1: range) (range2: range) =
    posMin range1.Start range2.Start

let rangeStartMin (range1: range) (range2: range) =
    if Position.posLt range1.Start range2.Start then range1 else range2


let attrOwnerStartRange (attrLists: SynAttributeList list) (xmlDoc: XmlDoc) (ownerRange: range) =
    match attrLists with
    | { Range = attrsRange } :: _ ->
        if xmlDoc.IsEmpty then
            rangeStartMin attrsRange ownerRange, XmlDoc.Empty
        else
            rangeStartMin xmlDoc.Range (rangeStartMin attrsRange ownerRange), xmlDoc

    | _ ->
        if xmlDoc.IsEmpty then
            ownerRange, XmlDoc.Empty
        else
            rangeStartMin ownerRange xmlDoc.Range, xmlDoc

let typeDefnGroupStartRange (bindings: SynTypeDefn list) (range: Range) =
    match bindings with
    | SynTypeDefn(SynComponentInfo(attributes = attrs; xmlDoc = XmlDoc xmlDoc), _, _, _, _) :: _ ->
        attrOwnerStartRange attrs xmlDoc range
    | _ -> range, XmlDoc.Empty

let typeSigGroupStartRange (bindings: SynTypeDefnSig list) (range: Range) =
    match bindings with
    | SynTypeDefnSig(SynComponentInfo(attributes = attrs; xmlDoc = XmlDoc xmlDoc), _, _, _) :: _ ->
        attrOwnerStartRange attrs xmlDoc range
    | _ -> range, XmlDoc.Empty

let letBindingGroupStartRange (bindings: SynBinding list) (range: Range) =
    match bindings with
    | SynBinding(attributes = attrs; xmlDoc = XmlDoc xmlDoc) :: _ ->
        attrOwnerStartRange attrs xmlDoc range
    | _ -> range, XmlDoc.Empty


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
