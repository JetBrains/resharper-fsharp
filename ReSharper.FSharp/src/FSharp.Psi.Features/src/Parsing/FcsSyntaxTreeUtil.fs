[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing.FcsSyntaxTreeUtil

open FSharp.Compiler.Syntax
open FSharp.Compiler.Xml

type SynMemberDefn with
    member x.Attributes =
        match x with
        | SynMemberDefn.Member(SynBinding(attributes = attrs), _)
        | SynMemberDefn.AbstractSlot(SynValSig(attributes = attrs), _, _)
        | SynMemberDefn.AutoProperty(attributes = attrs)
        | SynMemberDefn.ValField(SynField(attributes = attrs), _) -> attrs
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

type SynArgPats with
    member x.IsEmpty =
        match x with
        | SynArgPats.Pats pats -> pats.IsEmpty
        | SynArgPats.NamePatPairs(idsAndPats, _) -> idsAndPats.IsEmpty

type XmlDoc with
    member x.HasDeclaration = x.UnprocessedLines.Length > 0

let rec skipGeneratedLambdas expr =
    match expr with
    | SynExpr.Lambda(_, true, _, bodyExpr, _, _, _) ->
        skipGeneratedLambdas bodyExpr
    | _ -> expr

and skipGeneratedMatch expr =
    match expr with
    | SynExpr.Match(_, _, _, _, [ SynMatchClause(_, _, innerExpr, _, _, _) as clause ], matchRange) when
            matchRange.Start = clause.Range.Start ->
        skipGeneratedMatch innerExpr
    | _ -> expr

let inline getLambdaBodyExpr expr =
    let skippedLambdas = skipGeneratedLambdas expr
    skipGeneratedMatch skippedLambdas
