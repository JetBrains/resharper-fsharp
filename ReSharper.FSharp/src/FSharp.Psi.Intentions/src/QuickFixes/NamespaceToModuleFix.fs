namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpModulesUtil
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type NamespaceToModuleFix(node: IFSharpTreeNode) =
    inherit FSharpQuickFixBase()

    let namespaceDeclaration = node.GetContainingNamespaceDeclaration()

    new (error: NamespaceCannotContainBindingsError) = NamespaceToModuleFix(error.Binding)

    new (error: NamespaceCannotContainExpressionsError) = NamespaceToModuleFix(error.Expr)

    override x.Text = "Convert namespace to module"

    override x.IsAvailable _ =
        isValid node &&

        match namespaceDeclaration with
        | null -> false
        | :? IGlobalNamespaceDeclaration -> false
        | _ ->

        let fsFile = FSharpFileNavigator.GetByModuleDeclaration(namespaceDeclaration)
        isNotNull fsFile && fsFile.ModuleDeclarations.Count = 1

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(node.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        convertNamespaceToModule namespaceDeclaration
