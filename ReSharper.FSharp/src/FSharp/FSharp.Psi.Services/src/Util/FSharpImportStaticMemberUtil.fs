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

let private tryGetTypeParametersCount (refExpr: IReferenceExpr) =
    match refExpr.TypeArgumentList with
    | null -> None
    | list -> Some list.TypeUsages.Count

let isAvailable (reference: FSharpSymbolReference) =
    isNotNull reference &&

    let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
    isNotNull refExpr &&

    let qualifierRefExpr = refExpr.Qualifier.As<IReferenceExpr>()
    isNotNull qualifierRefExpr &&

    let qualifierReference = qualifierRefExpr.Reference
    isNull qualifierRefExpr.Qualifier && isNullOrModuleLike qualifierReference

let getTypeElements checkMemberName (reference: FSharpSymbolReference) =
    let qualifierReference = reference.QualifierReference
    let qualifierRefExpr = qualifierReference.GetElement().As<IReferenceExpr>()
    let typeName = qualifierReference.GetName()
    let memberName = reference.GetName()
    let typeParametersCount = tryGetTypeParametersCount qualifierRefExpr

    let psiModule = qualifierRefExpr.GetPsiModule()
    let symbolScope = getSymbolScope psiModule true
    let typeElements = symbolScope.GetAllTypeElementsGroupedByName()

    let isApplicable (typeElement: ITypeElement) =
        typeElement.GetSourceName() = typeName &&
        (typeParametersCount.IsNone || typeParametersCount.Value = typeElement.TypeParametersCount) &&
        (not checkMemberName || typeElement.HasMemberWithName(memberName, false)) &&
        FSharpAccessRightUtil.IsAccessible(typeElement, qualifierRefExpr)

    let result = HashSet()
    for typeElement in typeElements do
        if isApplicable typeElement then
            result.add(typeElement)

    result
