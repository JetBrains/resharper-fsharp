namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<ElementProblemAnalyzer(typeof<IFSharpTypeElementDeclaration>,
                         HighlightingTypes = [| typeof<ExtensionMemberInNonExtensionTypeWarning> |])>]
type ExtensionAttributeAnalyzer() =
    inherit ElementProblemAnalyzer<IFSharpTypeElementDeclaration>()

    override x.Run(typeDeclaration, _, consumer) =
        match typeDeclaration.DeclaredElement with
        | null -> ()
        | typeElement ->

        if typeElement.HasAttributeInstance(PredefinedType.EXTENSION_ATTRIBUTE_CLASS, false) then () else

        for memberDecl in typeDeclaration.MemberDeclarations do
            for attr in memberDecl.GetAttributes() do
                let reference = attr.Reference
                if isNull reference then () else

                let attributeTypeElement = reference.Resolve().DeclaredElement.As<ITypeElement>()
                if isNull attributeTypeElement then () else

                if attributeTypeElement.GetClrName() = PredefinedType.EXTENSION_ATTRIBUTE_CLASS then
                    consumer.AddHighlighting(ExtensionMemberInNonExtensionTypeWarning(attr))
