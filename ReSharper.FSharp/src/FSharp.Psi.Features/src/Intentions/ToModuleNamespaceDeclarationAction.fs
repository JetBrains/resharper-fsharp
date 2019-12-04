namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<ContextAction(Name = "ToModuleNamespace", Group = "F#", Description = "To module/namespace")>]
type ToModuleNamespaceDeclarationAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    let isNamespace (declaration: IModuleLikeDeclaration) =
        declaration :? INamespaceDeclaration
    
    let getNewNodeTypes decl =
        if isNamespace decl then
            FSharpTokenType.MODULE, ElementType.NAMED_MODULE_DECLARATION
        else
            FSharpTokenType.NAMESPACE, ElementType.NAMED_NAMESPACE_DECLARATION

    override x.Text =
        if isNamespace (dataProvider.GetSelectedElement()) then "To module" else "To namespace"

    override x.IsAvailable _ =
        let moduleDeclaration = dataProvider.GetSelectedElement<IQualifiableModuleLikeDeclaration>()
        if not (isAtModuleDeclaration dataProvider moduleDeclaration) then false else

        true

    override x.ExecutePsiTransaction(_, _) =
        let moduleDeclaration = dataProvider.GetSelectedElement<IDeclaredModuleLikeDeclaration>()
        use writeCookie = WriteLockCookie.Create(moduleDeclaration.IsPhysical())

        let tokenType, nodeType = getNewNodeTypes moduleDeclaration
        replaceWithToken moduleDeclaration.ModuleOrNamespaceKeyword tokenType
        let upcastExpr = ModificationUtil.ReplaceChild(moduleDeclaration, nodeType.Create())
        LowLevelModificationUtil.AddChild(upcastExpr, moduleDeclaration.Children().AsArray())

        null
