namespace System.Diagnostics.CodeAnalysis

open System

[<AttributeUsage(AttributeTargets.Parameter
                 ||| AttributeTargets.Field
                 ||| AttributeTargets.Property)>]
type StringSyntaxAttribute(syntax: string) =
    inherit Attribute()
    member x.Syntax = syntax


namespace JetBrains.Annotations

open System

type InjectedLanguage =
    | CSS = 0
    | HTML = 1
    | JAVASCRIPT = 2
    | JSON = 3
    | XML = 4

[<AttributeUsage(AttributeTargets.Parameter
                 ||| AttributeTargets.Field
                 ||| AttributeTargets.Property)>]
type LanguageInjectionAttribute(injectedLanguage: InjectedLanguage) =
    inherit Attribute()
    member x.InjectedLanguage = injectedLanguage
    member val Prefix = "" with get, set
    member val Suffix = "" with get, set

namespace Test

open System.Diagnostics.CodeAnalysis
open JetBrains.Annotations


type C =
    [<StringSyntax("json")>]
    member x.JsonProperty =
            """
            [ { 
                "key": null 
              } ]
            """

    [<LanguageInjection(InjectedLanguage.XML)>]
    member x.XmlField =
        """
              <tag1>
                <tag2 attr=""/>
              </tag1>
            """

    member x.Foo([<StringSyntax("css")>] _arg) = x.Foo(""".my-awesome-class {}""")
