namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2

[<ElementProblemAnalyzer(typeof<IFSharpTypeElementDeclaration>,
                         HighlightingTypes = [| typeof<ExtensionMemberInNonExtensionTypeWarning>
                                                typeof<ExtensionTypeWithNoExtensionMembersWarning> |])>]
type ExtensionAttributeAnalyzer() =
    inherit ElementProblemAnalyzer<IFSharpTypeElementDeclaration>()

    let mayHaveExtensions (typeElement: TypeElement) =
        typeElement.EnumerateParts()
        |> Seq.exists (fun p -> p.ExtensionMethodInfos.Length > 0)

    let isExtension (attr: IAttribute) =
        let reference = attr.ReferenceName.Reference
        if isNull reference then false else

        // todo: we should also account type abbreviations in future
        let referenceName = reference.GetName()
        if referenceName <> "Extension" && referenceName <> "ExtensionAttribute" then false else

        let attributeTypeElement = reference.Resolve().DeclaredElement.As<ITypeElement>()
        if isNull attributeTypeElement then false else

        attributeTypeElement.GetClrName() = PredefinedType.EXTENSION_ATTRIBUTE_CLASS

    override x.Run(typeDeclaration, _, consumer) =
        match typeDeclaration.DeclaredElement.As<TypeElement>() with
        | null -> ()
        | typeElement ->

        let mutable typeDeclHasExtensionAttr = false
        let membersMayHaveExtensionAttrs = mayHaveExtensions typeElement

        for attr in typeDeclaration.GetAttributes() do
            if not typeDeclHasExtensionAttr && isExtension attr then
                typeDeclHasExtensionAttr <- true

                if not membersMayHaveExtensionAttrs then
                    consumer.AddHighlighting(ExtensionTypeWithNoExtensionMembersWarning(attr))

        if not membersMayHaveExtensionAttrs then () else

        let typeHasExtensionAttr =
            typeDeclHasExtensionAttr ||
            typeElement.HasAttributeInstance(PredefinedType.EXTENSION_ATTRIBUTE_CLASS, false)

        if typeHasExtensionAttr then () else

        for memberDecl in typeDeclaration.MemberDeclarations do
            for attr in memberDecl.GetAttributes() do
                if isExtension attr then
                    consumer.AddHighlighting(ExtensionMemberInNonExtensionTypeWarning(attr))
