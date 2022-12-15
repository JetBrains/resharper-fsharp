[<MyAttribute>]
module TestModule

let binding = ()

[<MyAttribute>]
let bindingWithAttr = ()

let bindingWithAttrPat ([<MyAttribute>] x) = ()

let bindingWithInnerFun =
  fun x ->
    fun ([<MyAttribute>] y) -> ()

[<MyAttribute>]
type RecordType = { [<MyAttribute>] RecordFieldWithAttr: int; RecordField: int }

type UnionType =
  | UnionCase
  | [<MyAttribute>] UnionCaseWithAttr

type MyType
  [<MyAttribute>] 
  () =

  member x.Method() = ()

  [<MyAttribute>]
  member x.MethodWithAttr() = ()

  member x.MethodWithAttrPat([<MyAttribute>] p) = ()

type MyTypeWithAttrCtor([<MyAttribute>] x) =
  new(x: int, [<MyAttribute>] y) = MyTypeWithAttrCtor(y)
