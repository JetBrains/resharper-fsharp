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
    member x.JsonProperty0 = "[ { \"key\": \"value\" } ]"

    [<StringSyntax("json")>]
    member x.JsonProperty1 = $"[ {{ \"key\": \"{42}\" }} ]"

    [<StringSyntax("json")>]
    member x.JsonProperty2 = @"[ {
                                ""key"": null
                                } ]"

    [<StringSyntax("json")>]
    member x.JsonProperty3 = $@"[ {5} ]"

    [<StringSyntax("json")>]
    member x.JsonProperty4 =
            """
            [ {
                "key": null
              } ]
            """

    [<StringSyntax("json")>]
    member x.JsonProperty5 =
              $"""
               [ {{
                   "key": {0}
                  }} ]
               """

    [<LanguageInjection(InjectedLanguage.JSON, Prefix = "{", Suffix = "}")>]
    member x.JsonProperty6 =
        """
            "a": 0,
            "b": null
        """


    [<LanguageInjection(InjectedLanguage.XML)>]
    member x.XmlField0 = "<tag1><tag2 attr=\"value\"/></tag1>"

    [<LanguageInjection(InjectedLanguage.XML)>]
    member x.XmlField1 = $"<tag1><tag2 attr=\"{s1}\"/></tag1>"

    [<LanguageInjection(InjectedLanguage.XML)>]
    member x.XmlField2 = @"<tag1><tag2 attr=""value""/></tag1>"

    [<LanguageInjection(InjectedLanguage.XML)>]
    member x.XmlField3 = $@"<tag1><tag2 attr=""{s1}""/></tag1>"

    [<LanguageInjection(InjectedLanguage.XML)>]
    member x.XmlField4 =
            """
              <tag1>
                <tag2 attr=""/>
              </tag1>
            """

    [<LanguageInjection(InjectedLanguage.XML)>]
    member x.XmlField5 =
                $"""
                 <tag1>
                   <tag2 attr="{0}"/>
                 </tag1>
                 """


    member x.Foo0([<StringSyntax("css")>] _arg) = x.Foo0(".my-awesome-class {}")

    member x.Foo1([<StringSyntax("css")>] _arg) = x.Foo1($".my-awesome-class {}")

    member x.Foo2([<StringSyntax("css")>] _arg) = x.Foo2(@".my-awesome-class")

    member x.Foo3([<StringSyntax("css")>] _arg) = x.Foo3($@".unfinished{}")

    member x.Foo4([<StringSyntax("css")>] _arg) = x.Foo4(""".my-awesome-class {}""")

    member x.Foo5([<StringSyntax("css")>] _arg) = x.Foo5($""".my-awesome-class {}""")
