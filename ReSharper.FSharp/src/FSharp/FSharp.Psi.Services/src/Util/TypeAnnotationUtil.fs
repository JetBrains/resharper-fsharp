module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationUtil

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpBindUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util

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
