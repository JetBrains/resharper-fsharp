[<MyAttribute>]
module TestModule

let binding = ()

[<MyAttribute>]
let bindingWithAttr = ()

let bindingWithAttrPat ([<MyAttribute>] x) = ()

[<MyAttribute>]
type RecordType = { [<MyAttribute>] RecordFieldWithAttr: int; RecordField: int }

type UnionType =
  | UnionCase
  | [<MyAttribute>] UnionCaseWithAttr

type MyType() =
  member x.Method() = ()

  [<MyAttribute>]
  member x.MethodWithAttr() = ()

  member x.MethodWithAttrPat([<MyAttribute>] p) = ()
