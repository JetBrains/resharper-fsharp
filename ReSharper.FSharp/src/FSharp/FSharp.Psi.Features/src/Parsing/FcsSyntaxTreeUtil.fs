[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing.FcsSyntaxTreeUtil

open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia
open FSharp.Compiler.Text
open FSharp.Compiler.Xml

let firstGetSetBinding (binding1: SynBinding option) (binding2: SynBinding option) =
    match binding1, binding2 with
    | Some binding1, Some binding2 ->
        if Position.posLt binding1.RangeOfBindingWithoutRhs.Start binding2.RangeOfBindingWithoutRhs.Start then
            Some binding1
        else
            Some binding2

    | Some binding, _
    | _, Some binding -> Some binding

    | _ -> None

type SynMemberDefn with
    member x.Attributes =
        match x with
        | SynMemberDefn.Member(SynBinding(attributes = attrs), _)
        | SynMemberDefn.AbstractSlot(SynValSig(attributes = attrs), _, _, _)
        | SynMemberDefn.AutoProperty(attributes = attrs)
        | SynMemberDefn.ValField(SynField(attributes = attrs), _) -> attrs

        | SynMemberDefn.GetSetMember(getBinding, setBinding, _, trivia) ->
            match firstGetSetBinding getBinding setBinding with
            | Some(SynBinding(attributes = attrs)) ->
                attrs |> List.takeWhile (fun attrList -> Position.posLt attrList.Range.Start trivia.WithKeyword.Start)
            | _ -> []

        | _ -> []

    member x.XmlDoc =
        match x with
        | SynMemberDefn.Member(SynBinding(xmlDoc = xmlDoc), _)
        | SynMemberDefn.ImplicitCtor(xmlDoc = xmlDoc)
        | SynMemberDefn.LetBindings(SynBinding(xmlDoc = xmlDoc) :: _, _, _, _, _)
        | SynMemberDefn.AbstractSlot(SynValSig(xmlDoc = xmlDoc), _, _, _)
        | SynMemberDefn.ValField(SynField(xmlDoc = xmlDoc), _)
        | SynMemberDefn.AutoProperty(xmlDoc = xmlDoc) -> xmlDoc.ToXmlDoc(false, None)

        | SynMemberDefn.GetSetMember(getBinding, setBinding, _, _) ->
            match firstGetSetBinding getBinding setBinding with
            | Some(SynBinding(xmlDoc = xmlDoc)) -> xmlDoc.ToXmlDoc(false, None)
            | _ -> XmlDoc.Empty

        | _ -> XmlDoc.Empty

type SynArgPats with
    member x.IsEmpty =
        match x with
        | SynArgPats.Pats pats -> pats.IsEmpty
        | SynArgPats.NamePatPairs(idsAndPats, _, _) -> idsAndPats.IsEmpty

type XmlDoc with
    member x.HasDeclaration = x.UnprocessedLines.Length > 0

let rec skipGeneratedLambdas expr =
    match expr with
    | SynExpr.Lambda(_, true, _, bodyExpr, _, _, _) ->
        skipGeneratedLambdas bodyExpr
    | _ -> expr

and skipGeneratedMatch expr =
    match expr with
    | SynExpr.Match(_, _, [ SynMatchClause(_, _, innerExpr, _, _, _) as clause ], matchRange, _) when
            matchRange.Start = clause.Range.Start ->
        skipGeneratedMatch innerExpr
    | _ -> expr

let inline getLambdaBodyExpr expr =
    let skippedLambdas = skipGeneratedLambdas expr
    skipGeneratedMatch skippedLambdas

let getActivePatternIdRange trivia range =
    match trivia with
    | Some(IdentTrivia.HasParenthesis(lparen, rparen)) -> Range.unionRanges lparen rparen
    | _ -> range

let (|LidWithTrivia|) (SynLongIdent(lid, _, trivia)) =
    let rec loop acc lid trivia =
        match lid, trivia with
        | headId :: restIds, headTrivia :: restTrivia -> loop ((headId, headTrivia) :: acc) restIds restTrivia
        | headId :: restId, _ -> loop ((headId, None) :: acc) restId []
        | _ -> List.rev acc

    loop [] lid trivia    
