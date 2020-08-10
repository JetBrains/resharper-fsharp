namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type ConvertNamespaceToModuleFix(error: NamespaceContainsValueError) =
    inherit FSharpQuickFixBase()

    override x.Text = "Convert namespace to module"

    override x.IsAvailable _ = isValid error.Identifier

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(error.Identifier.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let namespaceDeclaration = error.Identifier.GetContainingNamespaceDeclaration()
        convertNamespaceToModule namespaceDeclaration
