[<MyAttribute>]
module TestModuleWithAttr

let binding = ()

[<CompiledName("compiledName")>]
[<MyAttribute>]
let bindingWithAttr = ()

let bindingWithAttrPat ([<MyAttribute>] x) = ()

let bindingWithAttrPat2 (a, [<MyAttribute>] b) = ()

let bindingWithAttrPat3 (a, b) ([<MyAttribute>] c) = ()

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

  [<CompiledName("compiledName")>]
  member x.MethodWithParamAttr1(a, [<MyAttribute>] b) = ()

  member x.MethodWithParamAttr2(a, b) ([<MyAttribute>] c) = ()

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
