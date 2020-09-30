open System
open System.Reflection

module M =
    let [<Literal>] X = ""

let [<Literal>] Y = ""


[<assembly: AssemblyTitle(M.X)>]
do()

[<assembly: AssemblyTitle(Y)>]
do()

[<assembly: AssemblyTitle("Foo")>]
do()


[<AttributeUsage(AttributeTargets.All, AllowMultiple = true)>]
type A(a: int[]) =
    inherit Attribute()

[<assembly: A [||]>]
do()

[<assembly: A([||])>]
do()


[<assembly: AssemblyDelaySign true>]
do()

[<assembly: AssemblyDelaySign(true)>]
do()
