module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl.Reflection2

let private resolvesTo (declaredElement: IDeclaredElement) (reference: FSharpSymbolReference) qualified opName =
    match reference.ResolveWithFcs(opName, qualified) with
    | None -> false
    | Some symbolUse ->

    let referenceOwner = reference.GetElement()
    let unqualifiedElement = symbolUse.Symbol.GetDeclaredElement(referenceOwner.GetPsiModule(), referenceOwner)
    if declaredElement.Equals(unqualifiedElement) then true else

    // Workaround for case where unqualified resolve may return module with implicit suffix instead of type.
    let compiledTypeElement = unqualifiedElement.As<CompiledTypeElement>()
    if isNull compiledTypeElement then false else

    let shortName = reference.GetName()
    if not (unqualifiedElement.ShortName.HasModuleSuffix() && not (shortName.HasModuleSuffix())) then false else
    if not (isCompiledModule compiledTypeElement) then false else

    let typeElement = FSharpImplUtil.TryGetAssociatedType(compiledTypeElement, shortName)
    declaredElement.Equals(typeElement)

let resolvesToUnqualified (declaredElement: IDeclaredElement) (reference: FSharpSymbolReference) opName =
    resolvesTo declaredElement reference false opName

let resolvesToQualified (declaredElement: IDeclaredElement) (reference: FSharpSymbolReference) opName =
    resolvesTo declaredElement reference true opName
