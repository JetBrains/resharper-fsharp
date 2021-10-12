module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpPatternUtil

open FSharp.Compiler.Symbols
open FSharp.Compiler.Tokenization
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

// todo: replace Fcs symbols with R# elements when possible
let bindFcsSymbol (pattern: IFSharpPattern) (fcsSymbol: FSharpSymbol) opName =
    match fcsSymbol with
    | :? FSharpUnionCase as unionCase ->
        let factory = pattern.CreateElementFactory()

        let name = FSharpKeywords.AddBackticksToIdentifierIfNeeded unionCase.Name
        let text = if unionCase.HasFields then $"({name} _)" else $"({name})" // todo: remove parens, escape in factory
        let newPattern = factory.CreatePattern(text, false) :?> IParenPat
        let pat = ModificationUtil.ReplaceChild(pattern, newPattern.Pattern) // todo: move to reference binding

        // todo: unify interface
        let referenceName = 
            match pat with
            | :? IReferencePat as refPat -> refPat.ReferenceName
            | :? IParametersOwnerPat as p -> p.ReferenceName
            | _ -> null

        let declaredElement = fcsSymbol.GetDeclaredElement(pat.GetPsiModule()).As<IClrDeclaredElement>()
        if isNull referenceName || isNull declaredElement then pat else

        let reference = referenceName.Reference
        FSharpReferenceBindingUtil.SetRequiredQualifiers(reference, declaredElement)

        if not (FSharpResolveUtil.resolvesToQualified declaredElement reference opName) then
            // todo: use declared element directly
            let typeElement = declaredElement.GetContainingType()
            addOpens reference typeElement |> ignore

        pat

    | _ -> failwith $"Unexpected symbol: {fcsSymbol}"
