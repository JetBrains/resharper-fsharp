namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExpectedTypes
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Tree

// TODO: test for first item of completion list
// TODO: IsAvailable implementation (simple - detect computation expression)
// TODO: better relevance for custom operations?
// TODO: cases where custom operation is possible inside CE (for, if, match, ... ?)

[<Language(typeof<FSharpLanguage>)>]
type CompExprRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        //let n = context.ReparsedContext.Reference :?> IFSharpExpression |> ComputationExprNavigator.GetByExpression
        //n <> null
        true

    override this.TransformItems(context, collector) =
        collector.Items |> Seq.iter (fun item ->
           match item with
            | :? FcsLookupItem as fcsItem ->
                match fcsItem.FcsSymbol with
                | :? FSharpMemberOrFunctionOrValue as m when m.Attributes |> Seq.exists (fun a -> a.AttributeType.DisplayName = "CustomOperationAttribute") ->
                    item.Placement.Location <- PlacementLocation.Top
                    markRelevance item CLRLookupItemRelevance.ExpectedTypeMatch
                | _ -> ()
            | _ -> ()
        )


