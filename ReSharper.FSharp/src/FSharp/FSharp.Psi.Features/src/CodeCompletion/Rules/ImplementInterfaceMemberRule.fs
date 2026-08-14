namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules.OverrideRuleModule
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type ImplementInterfaceMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context
        |> isOverrideRuleAvailable (fun owner -> (owner :? IInterfaceImplementation))

    override this.AddLookupItems(context, collector) =
        let generatorContext = getGeneratorContext context
        let typeDecl = generatorContext.TypeDeclaration
        let typeElement = typeDecl.DeclaredElement
        let impl = (getMemberOwner context generatorContext).As<IInterfaceImplementation>()

        let generatorElements =
            GenerateOverrides.getInterfaceMembers true impl typeElement
            |> GenerateOverrides.sanitizeMembers

        for generatorElement in generatorElements do

            let item =
                createOverrideLookupItem context generatorElement false

            collector.Add(item)

        false

    override this.TransformItems(context, collector) =
        keepOnlyOverrideItems collector
        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
