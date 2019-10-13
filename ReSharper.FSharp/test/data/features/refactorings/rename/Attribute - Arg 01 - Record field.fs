//${NEW_NAME:Bar}
module Module

let [<Literal>] Foo = ""

type R =
    { [<CompiledName(Foo{caret})>]
      Field: int }
