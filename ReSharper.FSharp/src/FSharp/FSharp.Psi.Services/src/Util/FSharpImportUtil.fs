[<RequireQualifiedAccess>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpImportUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi

let private isTypeOrNsInScope (openedModulesProvider: OpenedModulesProvider) (declaredElement: IClrDeclaredElement) =
    let name = getQualifiedName declaredElement
    let scopes = openedModulesProvider.OpenedModuleScopes.GetValuesSafe(name)
    OpenScope.inAnyScope openedModulesProvider.Context scopes

let areMembersInScope (openedModulesProvider: OpenedModulesProvider) (typeElement: ITypeElement) =
    match typeElement with
    | :? IFSharpModule as fsModule ->
        isTypeOrNsInScope openedModulesProvider fsModule

    | containingType ->
        let ns = containingType.GetContainingNamespace()
        isTypeOrNsInScope openedModulesProvider ns

let isTypeMemberInScope (openedModulesProvider: OpenedModulesProvider) (typeMember: ITypeMember) =
    areMembersInScope openedModulesProvider typeMember.ContainingType

let isTypeElementInScope (openedModulesProvider: OpenedModulesProvider) (typeElement: ITypeElement) =
    isTypeOrNsInScope openedModulesProvider typeElement ||

    match typeElement.GetContainingType() with
    | :? IFSharpModule as fsModule ->
        isTypeOrNsInScope openedModulesProvider fsModule

    | null ->
        let ns = typeElement.GetContainingNamespace()
        isTypeOrNsInScope openedModulesProvider ns

    | _ -> false
