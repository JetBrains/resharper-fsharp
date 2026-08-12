namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules.OverrideRuleModule
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.Util.Extension

module ImplementInterfaceMemberRuleModule =

    let addInterfaceImplMemberItems
        (context: FSharpCodeCompletionContext)
        (collector: IItemsCollector)
        (presentationText: IOverridableMember -> string)
        (getTextualInfo: IMemberDeclaration -> TextualInfo)
        =
        let generatorContext = getGeneratorContext context
        let typeDecl = generatorContext.TypeDeclaration
        let typeElement = typeDecl.DeclaredElement
        let impl = (getMemberOwner context generatorContext).As<IInterfaceImplementation>()

        let psiModule = typeDecl.GetPsiModule()

        let generatorElements =
            GenerateOverrides.getInterfaceMembers true impl typeElement psiModule
            |> GenerateOverrides.sanitizeMembers

        for generatorElement in generatorElements do
            let memberDecl, types =
                GenerateOverrides.generateMember context.NodeInFile false generatorElement

            let info = getTextualInfo memberDecl

            let item =
                createOverrideLookupItem context presentationText generatorElement info types

            collector.Add(item)

        false

open ImplementInterfaceMemberRuleModule

[<Language(typeof<FSharpLanguage>)>]
type ImplementInterfaceMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    /// Available for both unqualified (whitespace) and qualified (dot) contexts inside an interface implementation owner.
    override this.IsAvailable(context) =
        context
        |> isOverrideRuleAvailable (fun node -> isWhitespace node || isDot node) (fun owner -> (owner :? IInterfaceImplementation))

    override this.AddLookupItems(context, collector) =
        let node = context.NodeInFile

        let mkTextualInfo prefix : IMemberDeclaration -> TextualInfo =
            fun (memberDecl: IMemberDeclaration) ->
                let originalText = memberDecl.GetText()
                let text = prefix + originalText.RemoveStart($"{memberDecl.MemberKeyword.GetText()} ")
                TextualInfo(text, text, Ranges = context.Ranges)

        if isDot node then
            addInterfaceImplMemberItems
                context
                collector
                (fun mainMember -> $"{mainMember.ShortName}")
                (fun memberDecl ->
                    let fullText = memberDecl.GetText()
                    let selfId = memberDecl.SelfId.GetText()
                    let text = fullText.RemoveStart($"{memberDecl.MemberKeyword.GetText()} {selfId}.")
                    TextualInfo(text, text, Ranges = context.Ranges))
        else
            addInterfaceImplMemberItems
                context
                collector
                (fun mainMember -> $"member {mainMember.ShortName}")
                (mkTextualInfo "member ")

    override this.TransformItems(context, collector) =
        keepOnlyOverrideItems collector
        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
