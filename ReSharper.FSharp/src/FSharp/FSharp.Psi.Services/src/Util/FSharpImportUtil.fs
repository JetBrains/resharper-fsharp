[<RequireQualifiedAccess>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpImportUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi

let private isTypeOrNsInScope (openedModulesProvider: OpenedModulesProvider) (declaredElement: IClrDeclaredElement) =
    let name = getClrName declaredElement
    let scopes = openedModulesProvider.OpenedModuleScopes.GetValuesSafe(name)
    OpenScope.inAnyScope openedModulesProvider.Context scopes

let areMembersInScope (openedModulesProvider: OpenedModulesProvider) (typeElement: ITypeElement) =
    match typeElement with
    | :? IFSharpModule as fsModule ->
        isTypeOrNsInScope openedModulesProvider fsModule

    | typeElement ->
        let ns = typeElement.GetContainingNamespace()
        isTypeOrNsInScope openedModulesProvider ns

let isExtensionMemberInScope (openedModulesProvider: OpenedModulesProvider) (typeMember: ITypeMember) =
    let containingTypeOrNs: IClrDeclaredElement =
        let typeElement = typeMember.ContainingType
        match typeElement.GetContainingType() with
        | null -> typeElement.GetNamespace()
        | containingType -> containingType

    isTypeOrNsInScope openedModulesProvider containingTypeOrNs

let isTypeElementInScope (openedModulesProvider: OpenedModulesProvider) (typeElement: ITypeElement) =
    match typeElement.GetContainingType() with
    | null ->
        let ns = typeElement.GetContainingNamespace()
        isTypeOrNsInScope openedModulesProvider ns

    | :? IFSharpModule as fsModule ->
        isTypeOrNsInScope openedModulesProvider fsModule

    | typeElement ->
        areMembersInScope openedModulesProvider typeElement
