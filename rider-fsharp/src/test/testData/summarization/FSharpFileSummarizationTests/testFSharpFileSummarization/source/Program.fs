[<Literal>]
let bar = 123
let foo() = bar

let baz qux qaz = "asdasd"

type MyMegaType(foo: string, bar: int) =
  let letMember = 123

  let funcMember() = 123

  member _.FooMember() = 321
  member _.BarMember = 321

  interface System.IDisposable with
    member _.Dispose() = ()

  static member Frob() = ()

type MyMegaStruct = struct end
