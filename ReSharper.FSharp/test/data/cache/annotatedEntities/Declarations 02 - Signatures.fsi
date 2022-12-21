namespace Test

[<MyAttribute>]
module TestModuleWithAttr =

  val binding: unit

  [<MyAttribute>]
  val bindingWithAttr: unit

  [<MyAttribute>]
  type RecordType = { [<MyAttribute>] RecordFieldWithAttr: int; RecordField: int }

  type UnionType =
    | UnionCase
    | [<MyAttribute>] UnionCaseWithAttr

  type MyType =
    [<MyAttribute>] 
    new: unit -> MyType

    member Method: unit -> unit

    [<MyAttribute>]
    member MethodWithAttr: unit -> unit

  [<AbstractClass>]
  type MyAbstractType =
    abstract member AbstractMethod: unit -> unit

    [<MyAttribute>]
    abstract member AbstractMethodWithAttr: unit -> unit

  type StructType =
    struct
      [<MyAttribute>]
      val valFieldWithAttr: float
      val valField: float
    end
