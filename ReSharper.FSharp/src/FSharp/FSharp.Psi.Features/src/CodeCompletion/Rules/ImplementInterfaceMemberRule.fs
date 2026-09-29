namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules.OverrideMemberRule
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type ImplementInterfaceMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context
        |> isOverrideRuleAvailable GeneratorStandardKinds.MissingMembers

    override this.AddLookupItems(context, collector) =
        let generatorContext = getGeneratorContext context
        let typeDecl = generatorContext.TypeDeclaration
        let typeElement = typeDecl.DeclaredElement
        let interfaceImpl = (getMemberOwner context generatorContext).As<IInterfaceImplementation>()

        let implContext =
            if isNull interfaceImpl then
                match typeDecl with
                | :? IObjExpr as objExpr ->
                    ValueSome (InterfaceImplementationContext.Create(objExpr))
                | _ -> ValueNone
            else
                ValueSome (InterfaceImplementationContext.Create(interfaceImpl))

        match implContext with
        | ValueNone -> false
        | ValueSome implContext ->

        let generatorElements =
            GenerateOverrides.getInterfaceMembers true implContext typeElement
            |> GenerateOverrides.sanitizeMembers

        for generatorElement in generatorElements do

            let item =
                createOverrideLookupItem context generatorContext generatorElement false

            collector.Add(item)

        false

    override this.TransformItems(context, collector) =
        keepOnlyOverrideItems collector
        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
