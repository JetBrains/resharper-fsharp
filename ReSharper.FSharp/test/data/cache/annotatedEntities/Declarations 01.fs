[<MyAttribute>]
module TestModuleWithAttr

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
  member val AutoPropertyWithAttr = 5 

  [<MyAttribute>]
  member x.MethodWithAttr() = ()

  member x.PropertyWithAttrSetter with set([<MyAttribute>] v) = ()

  member x.MethodWithAttrPat([<MyAttribute>] p) = ()

[<AbstractClass>]
type MyAbstractType =
  abstract member AbstractMethod: unit -> unit

  [<MyAttribute>]
  abstract member AbstractMethodWithAttr: unit -> unit

type MyTypeWithAttrCtor([<MyAttribute>] x) =
  new(x: int, [<MyAttribute>] y) = MyTypeWithAttrCtor(y)

type StructType =
  struct
    [<MyAttribute>]
    val valFieldWithAttr: float
    val valField: float
  end

[<MyAttribute>]
exception ExnWithAttr of string

module NestedModule = ()

[<MyAttribute>]
module NestedModuleWithAttr = ()
