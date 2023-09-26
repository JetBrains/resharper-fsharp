namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeStructure

open JetBrains.Application
open JetBrains.ReSharper.Features.Navigation.Goto.GotoProviders
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi

[<ShellFeaturePart>]
type FSharpGotoClassMemberProvider() =
    inherit ClrGotoClassMemberProviderBase()

    override this.IsApplicable(primarySourceFile, _, _, _) =
        primarySourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>()

    override this.BasicFileMemberFilter(typeToSearchIn, typeMember) =
        match typeToSearchIn with
        | :? IFSharpModule -> typeMember.ContainingType == typeToSearchIn
        | _ -> base.BasicFileMemberFilter(typeToSearchIn, typeMember)
