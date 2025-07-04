module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpBindUtil

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util

// todo: we modify the tree and then ask FCS. What tree is used? Do we wait for the new analysis?
let bindDeclaredElementToReference (context: ITreeNode) (reference: FSharpSymbolReference)
        (declaredElement: IClrDeclaredElement) opName =
    reference.SetRequiredQualifiers(declaredElement, context)

    let resolveExpr = not (reference.GetElement() :? ITypeReferenceName)
    if not (FSharpResolveUtil.resolvesToQualified declaredElement reference resolveExpr opName) then
        addOpens reference declaredElement |> ignore

let bindFcsSymbolToReference (context: ITreeNode) (reference: FSharpSymbolReference) (fcsSymbol: FSharpSymbol) opName =
    let declaredElement = fcsSymbol.GetDeclaredElement(context.GetPsiModule()).As<IClrDeclaredElement>()
    if isNull reference || reference.IsQualified || isNull declaredElement then () else
    bindDeclaredElementToReference context reference declaredElement opName


// todo: replace Fcs symbols with R# elements when possible
let bindFcsSymbol (pattern: IFSharpPattern) (fcsSymbol: FSharpSymbol) opName =
    // todo: move to reference binding
    let bind name =
        let factory = pattern.CreateElementFactory()

        let name = FSharpNamingService.normalizeBackticks name
        let newPattern = factory.CreatePattern(name, false)
        let pat = ModificationUtil.ReplaceChild(pattern, newPattern)

        let referenceName = FSharpPatternUtil.getReferenceName pat

        let oldQualifierWithDot =
            let referenceName = FSharpPatternUtil.getReferenceName pattern
            if isNotNull referenceName then TreeRange(referenceName.Qualifier, referenceName.Delimiter) else null

        if isNotNull oldQualifierWithDot then
            ModificationUtil.AddChildRangeAfter(referenceName, null, oldQualifierWithDot) |> ignore

        bindFcsSymbolToReference pat referenceName.Reference fcsSymbol opName

        pat
    
    match fcsSymbol with
    | :? FSharpUnionCase as unionCase -> bind unionCase.Name
    | :? FSharpField as field when isEnumMember field -> bind field.Name
    | _ -> failwith $"Unexpected symbol: {fcsSymbol}"
