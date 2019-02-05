[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing.ParseTreeUtil

open System.Collections.Generic
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast

type SynBinding with
    member x.StartPos =
        let (Binding(_, _, _, _, attrs, _, _, headPat, _, _, _ , _)) = x
        match attrs with
        | { Range = r } :: _ -> r.Start
        | _ -> headPat.Range.Start

type SynMemberDefn with
    member x.Attributes =
        match x with
        | SynMemberDefn.LetBindings(Binding(_,_,_,_,attrs,_,_,_,_,_,_,_) :: _, _, _, _)
        | SynMemberDefn.Member(Binding(_,_,_,_,attrs,_,_,_,_,_,_,_),_)
        | SynMemberDefn.AbstractSlot(ValSpfn(attrs,_,_,_,_,_,_,_,_,_,_),_,_)
        | SynMemberDefn.AutoProperty(attrs,_,_,_,_,_,_,_,_,_,_)
        | SynMemberDefn.ValField(Field(attrs,_,_,_,_,_,_,_),_) -> attrs
        | _ -> []

let letStartPos (bindings: SynBinding list) (range: Range.range) =
    match bindings with
    | binding :: _ -> binding.StartPos
    | _ -> range.Start

let rec (|Apps|_|) = function
    | SynExpr.App(_, true, expr, Apps ((cur, next: List<_>) as acc), _)
    | SynExpr.App(_, false, Apps ((cur, next) as acc), expr, _) ->
        next.Add(expr)
        Some acc

    | SynExpr.App(_, true, second, first, _) 
    | SynExpr.App(_, false, first, second, _) ->
        let list = List()
        list.Add(second)
        Some (first, list)

    | _ -> None
