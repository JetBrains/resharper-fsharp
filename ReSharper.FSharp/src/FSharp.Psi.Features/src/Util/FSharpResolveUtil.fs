module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl.Reflection2

/// Workaround for case where unqualified resolve may return module with implicit suffix instead of type.
let private resolvesToAssociatedModule (declaredElement: IDeclaredElement) (unqualifiedElement: IDeclaredElement) (reference: FSharpSymbolReference) =
    let unqualifiedTypeElement = unqualifiedElement.As<CompiledTypeElement>()
    if isNull unqualifiedTypeElement then false else

    let shortName = reference.GetName()
    if not (unqualifiedTypeElement.ShortName.HasModuleSuffix() && not (shortName.HasModuleSuffix())) then false else
    if not (isCompiledModule unqualifiedTypeElement) then false else

    let typeElement = FSharpImplUtil.TryGetAssociatedType(unqualifiedTypeElement, shortName)
    declaredElement.Equals(typeElement)

let private resolvesTo (declaredElement: IDeclaredElement) (reference: FSharpSymbolReference) qualified opName =
    match reference.ResolveWithFcs(opName, qualified) with
    | None -> false
    | Some symbolUse ->

    let referenceOwner = reference.GetElement()
    let unqualifiedElement = symbolUse.Symbol.GetDeclaredElement(referenceOwner.GetPsiModule(), referenceOwner)
    if declaredElement.Equals(unqualifiedElement) then true else

    resolvesToAssociatedModule declaredElement unqualifiedElement reference

let resolvesToUnqualified (declaredElement: IDeclaredElement) (reference: FSharpSymbolReference) opName =
    resolvesTo declaredElement reference false opName

let resolvesToQualified (declaredElement: IDeclaredElement) (reference: FSharpSymbolReference) opName =
    resolvesTo declaredElement reference true opName

let resolvesToFcsSymbolUnqualified (fcsSymbol: FSharpSymbol) (reference: FSharpSymbolReference) opName =
    match reference.ResolveWithFcs(opName, false) with
    | None -> false
    | Some symbolUse ->

    let resolvedFcsSymbol = symbolUse.Symbol
    if resolvedFcsSymbol.IsEffectivelySameAs(fcsSymbol) then true else

    if not (resolvedFcsSymbol :? FSharpEntity) then false else

    let referenceOwner = reference.GetElement()
    let psiModule = referenceOwner.GetPsiModule()

    let declaredElement = fcsSymbol.GetDeclaredElement(psiModule, referenceOwner)
    let resolvedElement = resolvedFcsSymbol.GetDeclaredElement(psiModule, referenceOwner)

    resolvesToAssociatedModule declaredElement resolvedElement reference
