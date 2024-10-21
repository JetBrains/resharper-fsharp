namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open FSharp.Compiler.CodeAnalysis
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText
open JetBrains.Util

type FcsSymbolInfo(text, symbolUse: FSharpSymbolUse) =
    inherit TextualInfo(text, text)

    interface IFcsLookupItemInfo with
        member this.FcsSymbol = if isNotNull symbolUse then symbolUse.Symbol else Unchecked.defaultof<_>
        member this.FcsSymbolUse = symbolUse
        member this.NamespaceToOpen = [||]

[<Language(typeof<FSharpLanguage>)>]
type LocalValuesRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let valuesKey = Key<Dictionary<string, FSharpSymbolUse>>(nameof LocalValuesRule)

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion &&

        let node = context.ReparsedContext.TreeNode
        isNotNull node &&

        let refExpr = node.Parent.As<IReferenceExpr>()
        isNotNull refExpr && not refExpr.IsQualified && refExpr.Identifier == node

    override this.AddLookupItems(context, collector) =
        let values = Dictionary<string, FSharpSymbolUse>()

        let addValue (decl: IFSharpDeclaration) =
            let sourceName = decl.SourceName
            if sourceName <> "_" then
                values.TryAdd(sourceName, decl.GetFcsSymbolUse()) |> ignore

        let addPatternValues (pat: IFSharpPattern) =
            if isNull pat then () else

            for declPattern in pat.Declarations do
                let pat = declPattern.As<IFSharpPattern>()
                if pat.IsDeclaration then
                    addValue declPattern

        let addLetBindingsValues (letBindings: ILetBindings) =
            for otherBinding in letBindings.Bindings do
                addPatternValues otherBinding.HeadPattern

        let addSelfIdValue (selfId: ISelfId) =
            if isNotNull selfId then
                addValue selfId

        let addMembersValues (contextOffset: int) (members: ITreeNode seq) =
            let members = members |> Seq.takeWhile (fun m -> m.GetTreeStartOffset().Offset < contextOffset)

            for otherMember in members do
                match otherMember with
                | :? ILetBindings as letBindings ->
                    for binding in letBindings.Bindings do
                        addPatternValues binding.HeadPattern

                | _ -> ()

        for parentNode in context.ReparsedContext.TreeNode.ContainingNodes() do
            match parentNode with
            | :? IMatchClause as matchClause ->
                addPatternValues matchClause.Pattern

            | :? IBinding as binding ->
                Seq.iter addPatternValues binding.ParameterPatterns

                let letBindings = LetBindingsNavigator.GetByBinding(binding)
                if isNotNull letBindings && letBindings.IsRecursive then
                    addLetBindingsValues letBindings

            | :? IAccessorDeclaration as decl ->
                Seq.iter addPatternValues decl.ParameterPatternsEnumerable

            | :? IFSharpTypeDeclaration as typeDecl ->
                let ctorDecl = typeDecl.PrimaryConstructorDeclaration
                if isNotNull ctorDecl then
                    addPatternValues ctorDecl.ParameterPatterns
                    addSelfIdValue ctorDecl.SelfIdentifier

            | :? IFSharpExpression as expr ->
                let letExpr = LetOrUseExprNavigator.GetByInExpression(expr)
                if isNotNull letExpr then
                    addLetBindingsValues letExpr

                match expr with
                | :? ILambdaExpr as lambdaExpr ->
                    for pattern in lambdaExpr.PatternsEnumerable do
                        addPatternValues pattern

                | :? IForEachExpr as forExpr ->
                    addPatternValues forExpr.Pattern

                | :? IForExpr as forExpr ->
                    addValue forExpr.Identifier

                | _ -> ()

            | :? IModuleMember as moduleMember ->
                let moduleDecl = ModuleLikeDeclarationNavigator.GetByMember(moduleMember)
                if isNotNull moduleDecl then
                    let isRecursive =
                        match moduleDecl with
                        | :? IDeclaredModuleLikeDeclaration as moduleDecl -> moduleDecl.IsRecursive
                        | _ -> false

                    let contextOffset =
                        if isRecursive then moduleDecl.GetTreeEndOffset() else moduleMember.GetTreeStartOffset()

                    addMembersValues contextOffset.Offset (moduleDecl.MembersEnumerable |> Seq.cast)

                let typeDecl = FSharpTypeDeclarationNavigator.GetByTypeMember(moduleMember.As())
                if isNotNull typeDecl then
                    addMembersValues (moduleMember.GetTreeStartOffset().Offset) (typeDecl.TypeMembersEnumerable |> Seq.cast)

            | :? ITypeBodyMemberDeclaration as typeMember ->
                let typeDecl = FSharpTypeDeclarationNavigator.GetByTypeMember(typeMember)
                if isNotNull typeDecl then
                    addMembersValues (typeMember.GetTreeStartOffset().Offset) (typeDecl.TypeMembersEnumerable |> Seq.cast)

                let memberDecl = typeMember.As<IMemberDeclaration>()
                if isNotNull memberDecl then
                    addSelfIdValue memberDecl.SelfId
                    Seq.iter addPatternValues memberDecl.ParameterPatterns

            | _ -> ()

        if values.Count > 0 then
            context.PutData(valuesKey, values)

        for KeyValue(name, fcsSymbolUse) in values do
            let icon = if isNull fcsSymbolUse then null else getIconId fcsSymbolUse.Symbol
            
            let info = FcsSymbolInfo(name, fcsSymbolUse, Ranges = context.Ranges)
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ -> TextualPresentation(name, info, icon))
                    .WithBehavior(fun _ -> TextualBehavior(info))
                    .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)

            item.Presentation.DisplayTypeName <-
                if isNull fcsSymbolUse then null else

                match getReturnType fcsSymbolUse.Symbol with
                | Some t -> RichText(t.Format(fcsSymbolUse.DisplayContext))
                | _ -> null

            collector.Add(item)

        false

    override this.TransformItems(context, collector) =
        let values = context.GetData(valuesKey)
        if isNull values then () else

        collector.RemoveWhere(fun item ->
            let fcsLookupItem = item.As<FcsLookupItem>()
            isNotNull fcsLookupItem &&

            values.ContainsKey(fcsLookupItem.Text)
        )
