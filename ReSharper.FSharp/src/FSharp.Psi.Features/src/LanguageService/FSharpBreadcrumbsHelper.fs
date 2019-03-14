namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features.Documents
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

[<ProjectFileType(typeof<FSharpProjectFileType>)>]
type FSharpBreadcrumbsHelper() =
    inherit BreadcrumbsHelperBase()

    override x.IsApplicable(declaration) =
        declaration :? ITypeMemberDeclaration && not (declaration :? ISynPat)
