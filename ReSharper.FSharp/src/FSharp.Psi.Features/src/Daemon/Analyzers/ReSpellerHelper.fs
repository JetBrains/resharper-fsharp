namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Features.ReSpeller.Analyzers
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type ReSpellerPsiHelper() =
    inherit PsiHelperBase()

    override x.ShouldSkipDeclaration(declaration) =
        match declaration with
        | :? ISynPat as synPat -> not synPat.IsDeclaration
        | :? IFSharpDeclaration as fsDeclaration -> fsDeclaration.NameIdentifier :? IActivePatternId
        | _ -> true

    override x.GetDeclarationOnIdentifier(node) =
        match node with
        | :? IFSharpIdentifier as fsIdentifier -> NamedPatNavigator.GetByIdentifier(fsIdentifier) :> _
        | _ -> null

    override x.ShouldSkipInheritedMember(node, _) =
        let memberDeclaration = node.As<IMemberDeclaration>()
        if isNull memberDeclaration then false else

        memberDeclaration.IsOverride ||
        isNull (InterfaceImplementationNavigator.GetByTypeMember(memberDeclaration))
