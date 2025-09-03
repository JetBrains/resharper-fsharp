module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpImportStaticMemberUtil

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi

let private isNullOrModuleLike (fcsReference: FSharpSymbolReference) =
    let fcsEntity = fcsReference.GetFcsSymbol().As<FSharpEntity>()
    isNull fcsEntity || fcsEntity.IsNamespace || fcsEntity.IsFSharpModule

let private tryGetTypeParametersCount (referenceOwner: IFSharpReferenceOwner) =
    let typeArgumentList: ITypeArgumentList =
        match referenceOwner with
        | :? IReferenceExpr as refExpr -> refExpr.TypeArgumentList
        | :? IReferenceName as referenceName -> referenceName.TypeArgumentList 
        | _ -> null

    match typeArgumentList with
    | null -> None
    | list -> Some list.TypeUsages.Count

let isAvailable (reference: FSharpSymbolReference) =
    isNotNull reference &&

    let qualifierReference = reference.QualifierReference
    isNotNull qualifierReference &&

    not qualifierReference.IsQualified && isNullOrModuleLike qualifierReference

let getTypeElements (memberName: string option) (qualifierReference: FSharpSymbolReference) : ITypeElement seq =
    let qualifierRefExpr = qualifierReference.GetElement().As<IFSharpReferenceOwner>()
    let typeName = qualifierReference.GetName()
    let typeParametersCount = tryGetTypeParametersCount qualifierRefExpr

    let psiModule = qualifierRefExpr.GetPsiModule()
    let symbolScope = getSymbolScope psiModule true
    let typeElements = symbolScope.GetAllTypeElementsGroupedByName()

    // todo: check not is nested (but allow types in modules)
    let isApplicable (typeElement: ITypeElement) =
        typeElement.GetSourceName() = typeName &&
        (typeParametersCount.IsNone || typeParametersCount.Value = typeElement.TypeParametersCount) &&
        (memberName.IsNone || typeElement.HasMemberWithName(memberName.Value, false)) &&
        FSharpAccessRightUtil.IsAccessible(typeElement, qualifierRefExpr)

    let result = HashSet()
    for typeElement in typeElements do
        if isApplicable typeElement then
            result.add(typeElement)

    result
