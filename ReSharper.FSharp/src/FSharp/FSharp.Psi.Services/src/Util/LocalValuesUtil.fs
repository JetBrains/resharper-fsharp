module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.LocalValuesUtil

open System.Collections.Generic
open FSharp.Compiler.CodeAnalysis
open JetBrains.Application
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

let valuesKey = Key<IDictionary<string, IFSharpDeclaration * FSharpSymbolUse>>("LocalValuesUtil")

let getLocalValues (context: ITreeNode) : IDictionary<_, _> =
    let values = Dictionary<string, IFSharpDeclaration * FSharpSymbolUse>()

    let addValue (decl: IFSharpDeclaration) =
        let sourceName = decl.SourceName
        if sourceName <> "_" then
            let symbolUse = decl.GetFcsSymbolUse()
            values.TryAdd(sourceName, (decl, symbolUse)) |> ignore

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

    for parentNode in context.ContainingNodes() do
        Interruption.Current.CheckAndThrow()

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

    values
