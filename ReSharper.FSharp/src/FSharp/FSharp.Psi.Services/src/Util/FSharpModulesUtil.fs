module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpModulesUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

let private toModuleNamespaceDeclaration tokenType nodeType (decl: IDeclaredModuleLikeDeclaration) =
    if isNotNull decl then
        replaceWithToken decl.ModuleOrNamespaceKeyword tokenType
        replaceWithNodeKeepChildren decl nodeType |> ignore

let convertModuleToNamespace: IDeclaredModuleLikeDeclaration -> unit =
    toModuleNamespaceDeclaration FSharpTokenType.NAMESPACE ElementType.NAMED_NAMESPACE_DECLARATION

let convertNamespaceToModule: IDeclaredModuleLikeDeclaration -> unit =
    toModuleNamespaceDeclaration FSharpTokenType.MODULE ElementType.NAMED_MODULE_DECLARATION
