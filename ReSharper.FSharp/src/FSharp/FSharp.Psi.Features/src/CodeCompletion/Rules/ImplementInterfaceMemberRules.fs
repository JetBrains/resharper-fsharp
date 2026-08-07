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
        (presentationTextOptions: (IOverridableMember -> string) seq)
        (getTextualInfoOptions: (IMemberDeclaration -> TextualInfo) seq)=
        let generatorContext = getGeneratorContext context
        let typeDecl = generatorContext.TypeDeclaration
        let typeElement = typeDecl.DeclaredElement
        let impl = (getMemberOwner context generatorContext).As<IInterfaceImplementation>()
        
        let psiModule = typeDecl.GetPsiModule()

        let generatorElements =
            GenerateOverrides.getInterfaceMembers true impl typeElement psiModule
            |> GenerateOverrides.sanitizeMembers
            
        let textualInfos = Seq.zip presentationTextOptions getTextualInfoOptions

        for generatorElement in generatorElements do
            let memberDecl, types = GenerateOverrides.generateMember context.NodeInFile false generatorElement
                
            textualInfos
            |> Seq.iter (fun (getPresentationText, getTextualInfo) ->
                let info = getTextualInfo memberDecl
                let item =
                    createOverrideLookupItem context getPresentationText generatorElement info types
                collector.Add(item))

        false

open ImplementInterfaceMemberRuleModule

[<Language(typeof<FSharpLanguage>)>]
type ImplementInterfaceMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context |> isOverrideRuleAvailable isWhitespace (fun owner -> (owner :? IInterfaceImplementation))

    override this.AddLookupItems(context, collector) =
        addInterfaceImplMemberItems context collector
            [| (fun mainMember -> $"member {mainMember.ShortName}")
               (fun mainMember -> $"override {mainMember.ShortName}") |]
            [| (fun memberDecl ->
                let originalText = memberDecl.GetText()
                let text = "member " + originalText.RemoveStart($"{memberDecl.MemberKeyword.GetText()} ")
                TextualInfo(text, text, Ranges = context.Ranges))
               (fun memberDecl ->
                let originalText = memberDecl.GetText()
                let text = "override " + originalText.RemoveStart($"{memberDecl.MemberKeyword.GetText()} ")
                TextualInfo(text, text, Ranges = context.Ranges)) |]

    override this.TransformItems(context, collector) =
        keepOnlyOverrideItems collector
        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext

[<Language(typeof<FSharpLanguage>)>]
type QualifiedImplementInterfaceMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context |> isOverrideRuleAvailable isDot (fun owner -> (owner :? IInterfaceImplementation))

    override this.AddLookupItems(context, collector) =
        addInterfaceImplMemberItems context collector
            [| (fun mainMember -> $"{mainMember.ShortName}") |]
            [| (fun memberDecl ->
                let fullText = memberDecl.GetText()
                let selfId = memberDecl.SelfId.GetText()
                let text = fullText.RemoveStart($"{memberDecl.MemberKeyword.GetText()} {selfId}.")
                TextualInfo(text, text, Ranges = context.Ranges)) |]

    override this.TransformItems(context, collector) =
        keepOnlyOverrideItems collector
        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
