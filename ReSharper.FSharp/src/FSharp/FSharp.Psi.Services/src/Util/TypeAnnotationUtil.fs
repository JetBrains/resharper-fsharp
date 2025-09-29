[<RequireQualifiedAccess>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationUtil

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpPatternUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpBindUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

let rec private collectTypeUsages acc (fcsType: FSharpType, typeUsage: ITypeUsage) =
    if fcsType.IsGenericParameter then acc else

    let collectMany acc typeUsages =
        typeUsages
        |> Seq.zip fcsType.GenericArguments
        |> Seq.fold collectTypeUsages acc

    match typeUsage with
    | :? INamedTypeUsage as typeUsage ->
        let acc = (typeUsage, fcsType) :: acc

        let typeArgList = typeUsage.ReferenceName.TypeArgumentList
        if isNull typeArgList then acc else

        collectMany acc typeArgList.TypeUsagesEnumerable

    | :? ITupleTypeUsage as typeUsage ->
        collectMany acc typeUsage.Items

    | :? IParenTypeUsage as typeUsage ->
        collectTypeUsages acc (fcsType, typeUsage.InnerTypeUsage)

    | :? IFunctionTypeUsage as typeUsage ->
        let fcsType = getAbbreviatedType fcsType
        let acc = collectTypeUsages acc (fcsType.GenericArguments[0], typeUsage.ArgumentTypeUsage)

        collectTypeUsages acc (fcsType.GenericArguments[1], typeUsage.ReturnTypeUsage)

    | :? IArrayTypeUsage as typeUsage ->
        collectTypeUsages acc (fcsType.GenericArguments[0], typeUsage.TypeUsage)

    | :? IAnonRecordTypeUsage as typeUsage ->
        typeUsage.Fields |> Seq.map _.TypeUsage |> collectMany acc

    | :? IWithNullTypeUsage as typeUsage ->
        let fcsType = fcsType.TypeDefinition.AsType()
        collectTypeUsages acc (fcsType, typeUsage.TypeUsage)

    | _ -> acc

let bindAnnotations annotationsInfo =
    let annotationsInfo =
        annotationsInfo
        |> Seq.fold collectTypeUsages []
        |> Seq.toList

    match annotationsInfo with
    | [] -> ()
    | (typeUsage, _) :: _ ->

    use pinResultsCookie = typeUsage.FSharpFile.PinTypeCheckResults(true, "Specify types")

    for typeUsage, fcsType in annotationsInfo do
        let typeReference = typeUsage.ReferenceName
        let reference = typeReference.Reference.AllowAllSymbolCandidatesCheck()
        let fcsSymbol = fcsType.TypeDefinition

        bindFcsSymbolToReference typeUsage reference fcsSymbol "Specify type"

let private addParens (factory: IFSharpElementFactory) (pattern: IFSharpPattern) =
    let parenPat = factory.CreateParenPat()
    parenPat.SetPattern(pattern) |> ignore
    parenPat :> IFSharpPattern

let specifyPatternTypeImpl (fcsType: FSharpType) (pattern: IFSharpPattern) =
    let pattern = pattern.IgnoreParentParens()
    let factory = pattern.CreateElementFactory()

    let pattern =
        match pattern.IgnoreInnerParens() with
        | :? IAttribPat as attribPat -> attribPat.Pattern
        | _ -> pattern

    let oldPattern, fcsType =
        match pattern.IgnoreInnerParens() with
        | :? IOptionalValPat ->
            let fcsType = if isOption fcsType then fcsType.GenericArguments[0] else fcsType
            pattern, fcsType

        | _ ->

        let optionalValPat = OptionalValPatNavigator.GetByPattern(pattern)
        if isNull optionalValPat then
            pattern, fcsType
        else
            optionalValPat, fcsType.GenericArguments[0]

    let newPattern =
        match oldPattern.IgnoreInnerParens() with
        | :? ITuplePat as tuplePat -> addParens factory tuplePat
        | :? ITypedPat as typedPat -> typedPat.Pattern
        | pattern -> pattern

    let typedPat =
        let typeUsage = factory.CreateTypeUsage(fcsType.Format(), TypeUsageContext.TopLevel)
        factory.CreateTypedPat(newPattern, typeUsage)

    let listConsParenPat = getOutermostListConstPat oldPattern |> _.IgnoreParentParens()

    let typedPat =
        let pat =
            ModificationUtil.ReplaceChild(oldPattern, typedPat)
            |> ParenPatUtil.addParensIfNeeded

        pat.IgnoreInnerParens().As<ITypedPat>()

    // In the case `x :: _: Type` add parens to the whole listConsPat
    //TODO: improve parens analyzer
    if isNotNull listConsParenPat && listConsParenPat :? IListConsPat then
        let listConstPat = (ParenPatUtil.addParens listConsParenPat).As<IListConsPat>()
        let typedPat = getLastTailPattern listConstPat :?> ITypedPat
        fcsType, typedPat.TypeUsage
    else
        fcsType, typedPat.TypeUsage

let specifyPatternType (fcsType: FSharpType) (pattern: IFSharpPattern) =
    let annotationsInfo = [| specifyPatternTypeImpl fcsType pattern |]
    bindAnnotations annotationsInfo
