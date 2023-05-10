namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open JetBrains.Application
open JetBrains.ReSharper.Plugins.FSharp.Psi.Injections
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeAnnotations
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.impl.Shared.InjectedPsi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FSharpErrorUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected.FSharpInjectionAnnotationUtil

type FSharpInjectionTargetsFinder() =
    let possibleInjectorFunctionNames = [|"html"; "css"; "sql"; "javascript"; "json"; "jsx"|]
    let normalizeLanguage = function
        | "js" | "jsx" -> "javascript"
        | language -> language

    let checkForAttributes (expr: IFSharpExpression) =
        match getAttributesOwner expr with
        | ValueNone -> ValueNone
        | ValueSome attributesOwner ->

        let info = getAnnotationInfo<StringSyntaxAnnotationProvider, string>(attributesOwner)
        if isNotNull info then ValueSome(info, "", "") else
        let info = getAnnotationInfo<LanguageInjectionAnnotationProvider, InjectionAnnotationInfo>(attributesOwner)
        if isNotNull info then ValueSome(info.Language, info.Prefix, info.Suffix) else ValueNone

    static member val Instance = FSharpInjectionTargetsFinder()

    interface ILanguageInjectionTargetsFinder with
        member this.Find(searchRoot, consumer) =
            let mutable descendants = searchRoot.CompositeDescendants()
            while descendants.MoveNext() do
                Interruption.Current.CheckAndThrow()

                match descendants.Current with
                | :? IInjectionHostNode as expr when expr.IsValidHost ->
                    match checkForAttributes expr with
                    | ValueSome(language, prefix, suffix) when not (equalsIgnoreCase language "Regex") ->
                        consumer.Consume(expr, normalizeLanguage language, prefix, suffix)
                    | _ ->

                    // support VSCode-compatible injection functions
                    // https://github.com/alfonsogarciacaro/vscode-template-fsharp-highlight
                    let prefixApp = PrefixAppExprNavigator.GetByArgumentExpression(expr.IgnoreParentParens())
                    if isNull prefixApp then () else
                    match prefixApp.FunctionExpression.IgnoreInnerParens() with
                    | :? IReferenceExpr as ref when isSimpleQualifiedName ref ->
                        let language = normalizeLanguage ref.ShortName
                        if Array.contains language possibleInjectorFunctionNames then
                            consumer.Consume(expr, language, "", "")
                    | _ ->

                    match expr with
                    | :? IConstExpr as expr ->
                        match tryGetTypeProviderName expr with
                        | ValueSome "SqlCommandProvider" ->
                            consumer.Consume(expr, "sql", "", "")
                        | ValueSome "JsonProvider" when expr.GetText().Contains("{") ->
                            consumer.Consume(expr, "json", "", "")
                        | ValueSome "XmlProvider" when expr.GetText().Contains("<") ->
                            consumer.Consume(expr, "xml", "", "")
                        | _ -> ()
                    | _ -> ()

                | :? IChameleonNode as c when not c.IsOpened -> descendants.SkipThisNode()
                | _ -> ()


[<Language(typeof<FSharpLanguage>)>]
type FSharInjectionTargetsFinderFactory() =
    interface ILanguageInjectionTargetsFinderFactory with
        member this.CreateAnnotationTargetsFinder() = FSharpInjectionTargetsFinder.Instance
