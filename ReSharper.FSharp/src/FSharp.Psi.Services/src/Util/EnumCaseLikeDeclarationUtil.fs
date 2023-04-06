module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.EnumCaseLikeDeclarationUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

let addBarIfNeeded (caseDeclaration: IEnumCaseLikeDeclaration) =
    if isNotNull caseDeclaration.Bar || isNull caseDeclaration.FirstChild then () else

    use cookie = WriteLockCookie.Create(caseDeclaration.IsPhysical())
    addNodesBefore caseDeclaration.FirstChild [
        FSharpTokenType.BAR.CreateLeafElement()
        Whitespace()
    ] |> ignore

    let typeRepr = EnumLikeTypeRepresentationNavigator.GetByEnumLikeCase(caseDeclaration)
    if isNull typeRepr then () else

    let cases = typeRepr.Cases
    if cases.Count <= 1 || cases[0] != caseDeclaration then () else 

    let firstCaseIndent = cases[0].Indent
    let secondCaseIndent = cases[1].Indent

    if firstCaseIndent > secondCaseIndent then
        let indentDiff = firstCaseIndent - secondCaseIndent

        let reduceIndent (node: ITreeNode) =
            if isInlineSpace node && node.GetTextLength() > indentDiff then
                let newSpace = Whitespace(node.GetTextLength() - indentDiff)
                replace node newSpace
                true
            else
                false

        (reduceIndent caseDeclaration.PrevSibling || reduceIndent typeRepr.PrevSibling) |> ignore

let addNewLineIfNeeded (typeDecl: IFSharpTypeDeclaration) (typeRepr: IEnumLikeTypeRepresentation) =
    if typeDecl.StartLine <> typeRepr.StartLine || isNull typeRepr.FirstChild then () else

    use cookie = WriteLockCookie.Create(typeRepr.IsPhysical())
    addNodesBefore typeRepr.FirstChild [
        NewLine(typeRepr.GetLineEnding())
        Whitespace(typeDecl.Indent + typeDecl.GetIndentSize())
    ] |> ignore
