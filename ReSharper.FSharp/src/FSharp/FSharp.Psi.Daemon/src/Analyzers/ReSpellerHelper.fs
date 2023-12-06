namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Features.ReSpeller.Analyzers
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

[<Language(typeof<FSharpLanguage>)>]
type ReSpellerPsiHelper() =
    inherit PsiHelperBase()

    override x.ShouldSkipDeclaration(declaration) =
        match declaration with
        | :? IFSharpPattern as fsPattern -> not fsPattern.IsDeclaration
        | :? IFSharpDeclaration as fsDeclaration -> fsDeclaration.NameIdentifier :? IActivePatternId
        | _ -> true

    override x.GetDeclarationOnIdentifier(node) =
        match node with
        | :? IFSharpIdentifier as fsIdentifier -> ReferencePatNavigator.GetByIdentifier(fsIdentifier) :> _
        | _ -> null

    override x.ShouldSkipInheritedMember(node, _) =
        let memberDeclaration = node.As<IMemberDeclaration>()
        if isNull memberDeclaration then false else

        memberDeclaration.IsOverride ||
        isNotNull (InterfaceImplementationNavigator.GetByTypeMember(memberDeclaration))

    override this.GetNamespaceIdentifiers(identifier, declaration) =
        let decl = declaration.As<INamedNamespaceDeclaration>()
        if isNull decl then base.GetNamespaceIdentifiers(identifier, declaration) else

        let rec loop acc (referenceName: IReferenceName) =
            if isNull referenceName then List.rev acc else

            let acc = struct (referenceName.Identifier :> ITreeNode, referenceName.ShortName) :: acc
            loop acc referenceName.Qualifier

        loop [identifier, identifier.Name] decl.QualifierReferenceName |> Array.ofList
