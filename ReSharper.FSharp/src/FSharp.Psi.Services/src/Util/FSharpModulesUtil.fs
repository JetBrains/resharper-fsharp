module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpModulesUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

let private toModuleNamespaceDeclaration
        tokenType (nodeType: CompositeNodeType) (decl: IDeclaredModuleLikeDeclaration) =

    if isNull decl then () else

    replaceWithToken decl.ModuleOrNamespaceKeyword tokenType
    let newModuleDeclaration = ModificationUtil.ReplaceChild(decl, nodeType.Create())
    LowLevelModificationUtil.AddChild(newModuleDeclaration, decl.Children().AsArray())

let convertModuleToNamespace: IDeclaredModuleLikeDeclaration -> unit =
    toModuleNamespaceDeclaration FSharpTokenType.NAMESPACE ElementType.NAMED_NAMESPACE_DECLARATION

let convertNamespaceToModule: IDeclaredModuleLikeDeclaration -> unit =
    toModuleNamespaceDeclaration FSharpTokenType.MODULE ElementType.NAMED_MODULE_DECLARATION
