namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2

[<ElementProblemAnalyzer(typeof<IFSharpTypeElementDeclaration>,
                         HighlightingTypes = [| typeof<ExtensionMemberInNonExtensionTypeWarning>
                                                typeof<ExtensionTypeWithNoExtensionMembersWarning>
                                                typeof<ExtensionMemberShouldBeStaticWarning> |])>]
type ExtensionAttributeAnalyzer() =
    inherit ElementProblemAnalyzer<IFSharpTypeElementDeclaration>()

    let mayHaveExtensions (typeElement: TypeElement) =
        typeElement.EnumerateParts()
        |> Seq.exists (fun p -> p.ExtensionMethodInfos.Length > 0)

    let isExtension attr =
        FSharpAttributesUtil.resolvesToType PredefinedType.EXTENSION_ATTRIBUTE_CLASS attr

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

        if not typeDeclHasExtensionAttr && not membersMayHaveExtensionAttrs then () else

        let typeHasExtensionAttr =
            typeDeclHasExtensionAttr ||
            typeElement.HasAttributeInstance(PredefinedType.EXTENSION_ATTRIBUTE_CLASS, false)

        for memberDecl in typeDeclaration.MemberDeclarations do
            for attr in memberDecl.GetAttributes() do
                if not (isExtension attr) then () else

                if not typeHasExtensionAttr then
                    consumer.AddHighlighting(ExtensionMemberInNonExtensionTypeWarning(attr))

                let memberDeclaration = memberDecl.As<IMemberDeclaration>()
                if isNotNull memberDeclaration && not memberDeclaration.IsStatic then
                    consumer.AddHighlighting(ExtensionMemberShouldBeStaticWarning(attr))
