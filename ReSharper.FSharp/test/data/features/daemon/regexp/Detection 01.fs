module Kek

open JetBrains.Annotations
open System.Diagnostics.CodeAnalysis
open System.Text.RegularExpressions

type A() =
    [<RegexPattern>]
    member x.R = "[123]"

    member x.K = "[123]"

[<CompiledName("compiledName")>]
let f ([<RegexPattern>] x: string) = ()
f ("[123]")
f "[123]"

let f1 =
    fun _ ->
        fun ([<RegexPattern>] x: string) -> ()
f1 "[123]" "[123]"

let f2 (x, y) (x1, [<RegexPattern>] y1) = ()
f2 (1, 2) (3, "[123]")

let g (x: string) = ()
g ("[123]")

[<RegexPattern>]
let x = "[123]"

[<StringSyntax("regex")>]
let y = "[123]"

let _ =
    ()
    let _ = "[123]" //language=regex
    ()

Regex(pattern = "[123]")
Regex((pattern = "[123]"))
id (pattern = "[123]")


type F =
    static member Foo([<StringSyntax(StringSyntaxAttribute.Regex)>] a: string, 
                      [<StringSyntax(StringSyntaxAttribute.Regex)>] ?b: string,
                      ?c: string) = ()

    static member Bar([<StringSyntax(StringSyntaxAttribute.Regex)>] a: string, 
                      [<StringSyntax(StringSyntaxAttribute.Regex)>] b: string,
                      ?c: string) = ()

    static member Car(a, b) (c, [<RegexPattern>] d: string) = ()

F.Foo("[123]")
F.Foo("[123]", "[123]")
F.Foo("[123]", "[123]", "[123]")

F.Bar("[123]")
F.Bar("[123]", "[123]")
F.Bar("[123]", "[123]", "[123]")

F.Car (1, 2) (3, "[123]")
