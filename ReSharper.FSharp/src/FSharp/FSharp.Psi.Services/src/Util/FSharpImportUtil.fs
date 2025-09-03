[<RequireQualifiedAccess>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpImportUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi

let private isTypeOrNsInScope (openedModulesProvider: OpenedModulesProvider) (declaredElement: IClrDeclaredElement) =
    let name = getQualifiedName declaredElement
    let scopes = openedModulesProvider.OpenedModuleScopes.GetValuesSafe(name)
    OpenScope.inAnyScope openedModulesProvider.Context scopes

let isTypeMemberInScope (openedModulesProvider: OpenedModulesProvider) (typeMember: ITypeMember) =
    match typeMember.ContainingType with
    | :? IFSharpModule as fsModule ->
        isTypeOrNsInScope openedModulesProvider fsModule

    | containingType ->
        let ns = containingType.GetContainingNamespace()
        isTypeOrNsInScope openedModulesProvider ns

let isTypeElementInScope (openedModulesProvider: OpenedModulesProvider) (typeElement: ITypeElement) =
    match typeElement with
    | :? IFSharpModule as fsModule ->
        isTypeOrNsInScope openedModulesProvider fsModule

    | containingType ->
        let ns = containingType.GetContainingNamespace()
        isTypeOrNsInScope openedModulesProvider ns

let isInScope (openedModulesProvider: OpenedModulesProvider) (declaredElement: IClrDeclaredElement) =
    match declaredElement with
    | :? ITypeElement as typeElement ->
        isTypeElementInScope openedModulesProvider typeElement

    | :? ITypeMember as typeMember ->
        isTypeMemberInScope openedModulesProvider typeMember

    | _ ->
        let containingType = declaredElement.GetContainingType()
        isNotNull containingType && isTypeOrNsInScope openedModulesProvider containingType
