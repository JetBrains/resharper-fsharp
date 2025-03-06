namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpAttributesUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ToAbstractFix(typeDecl: IFSharpTypeDeclaration) =
    inherit FSharpQuickFixBase()

    new (error: NoImplementationGivenInTypeError) =
        ToAbstractFix(error.TypeDecl)

    new (error: NoImplementationGivenInTypeWithSuggestionError) =
        ToAbstractFix(error.TypeDecl.As<IFSharpTypeDeclaration>())

    override x.Text = "To abstract"

    override x.IsAvailable _ =
        isValid typeDecl

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(typeDecl.IsPhysical())

        let attributeList = getTypeDeclarationAttributeList typeDecl
        let attribute = typeDecl.CreateElementFactory().CreateAttribute("AbstractClass")
        addAttribute attributeList attribute |> ignore
