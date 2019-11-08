[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing.ParseTreeUtil

open FSharp.Compiler
open FSharp.Compiler.Ast
open FSharp.Compiler.Range

type SynBinding with
    member x.StartPos =
        let (Binding(_, _, _, _, _, _, _, headPat, _, _, _ , _)) = x
        headPat.Range.Start

type SynMemberDefn with
    member x.OuterAttributes =
        match x with
        | SynMemberDefn.Member(Binding(_, _, _, _, attrs, _, _, _, _, _, _, _), _)
        | SynMemberDefn.AbstractSlot(ValSpfn(attrs, _, _, _, _, _, _, _, _, _, _), _, _)
        | SynMemberDefn.AutoProperty(attrs, _, _, _, _, _, _, _, _, _, _)
        | SynMemberDefn.ValField(Field(attrs, _, _, _, _, _, _, _), _) -> attrs

        | SynMemberDefn.LetBindings(Binding(_, _, _, _,attrs, _, _, _, _, _, _, _) :: _, _, _, range) ->
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

let letStartPos (bindings: SynBinding list) (range: Range.range) =
    match bindings with
    | Binding(_, _, _, _, { Range = r } :: _, _, _, _, _, _, _ , _) :: _
        when posLt r.Start range.Start -> r.Start

    | _ -> range.Start


let rec skipGeneratedLambdas expr =
    match expr with
    | SynExpr.Lambda(_, true, _, bodyExpr, _) ->
        skipGeneratedLambdas bodyExpr
    | _ -> expr

and skipGeneratedMatch expr =
    match expr with
    | SynExpr.Match(_, _, [ Clause(_, _, innerExpr, clauseRange, _) ], matchRange) when
            matchRange.Start = clauseRange.Start ->
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
