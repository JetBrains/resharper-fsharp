namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open FSharp.Compiler.Syntax.PrettyNaming
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText
open JetBrains.Util

type ToRecursiveFunctionInfo(text) =
    inherit TextualInfo(text, text)

    override this.MakeSafe(text) =
        FSharpNamingService.mangleNameIfNecessary text

type ToRecursiveFunctionBehavior(info, offset) =
    inherit TextualBehavior<ToRecursiveFunctionInfo>(info)

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        let node = textControl.Document.GetPsiSourceFile(solution).FSharpFile.FindNodeAt(nameRange)
        node.ContainingNodes<ILetBindings>().ToEnumerable()
        |> Seq.tryFind (fun letBindings -> letBindings.GetTreeStartOffset() = offset)
        |> Option.orElseWith (fun _ -> failwithf "Can't find let bindings")
        |> Option.iter (fun letBindings ->
            use writeCookie = WriteLockCookie.Create(letBindings.IsPhysical())
            use transactionCookie =
                PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, "To recursive")

            letBindings.SetIsRecursive(true))

[<Language(typeof<FSharpLanguage>)>]
type ToRecursiveFunctionRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion && not context.IsQualified

    override this.AddLookupItems(context, collector) =
        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference then false else

        let referenceOwner = reference.GetElement().As<IReferenceExpr>()
        if isNull referenceOwner then false else 

        let usedNames = HashSet()

        let addNames pattern =
            if isNotNull pattern then
                let names =
                    FSharpNamingService.getPatternsNames null [pattern]
                    |> Seq.map (fun name -> name.RemoveBackticks())
                usedNames.AddRange(names)

        for node in referenceOwner.ContainingNodes<ITreeNode>() do
            match node with
            | :? ILetOrUseExpr as letExpr ->
                letExpr.Bindings |> Seq.iter (fun binding -> addNames binding.HeadPattern)

            | :? IForEachExpr as forEachExpr -> addNames forEachExpr.Pattern
            | :? IMatchClause as matchClause -> addNames matchClause.Pattern
            | :? ILambdaExpr as lambdaExpr -> Seq.iter addNames lambdaExpr.PatternsEnumerable

            | :? IBinding as binding ->
                let refPat = binding.HeadPattern.As<IReferencePat>()
                if isNull refPat then () else

                let parameterPatterns = binding.ParameterPatternsEnumerable
                if Seq.isEmpty parameterPatterns then () else

                let names =
                    FSharpNamingService.getPatternsNames null parameterPatterns
                    |> Seq.map (fun name -> name.RemoveBackticks())
                usedNames.AddRange(names)

                let name = refPat.SourceName
                if usedNames.Contains(name) then () else
                if name = SharedImplUtil.MISSING_DECLARATION_NAME || IsActivePatternName name then () else

                let letBindings = LetBindingsNavigator.GetByBinding(binding)
                if isNull letBindings || letBindings.IsRecursive then () else

                let lookupText = RichText(name)
                LookupUtil.AddInformationText(lookupText, "(make recursive)")

                let info = ToRecursiveFunctionInfo(name, Ranges = context.Ranges)
                let item =
                    LookupItemFactory.CreateLookupItem(info)
                        .WithPresentation(fun _ -> TextualPresentation(lookupText, info) :> _)
                        .WithBehavior(fun _ -> ToRecursiveFunctionBehavior(info, letBindings.GetTreeStartOffset()) :> _)
                        .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)
                        .WithRelevance(CLRLookupItemRelevance.GenerateItems)

                collector.Add(item)

            | _ -> ()

        true
