// ${KIND:Overrides}
// ${SELECT0:get_Item():System.String}
// ${SELECT1:set_Item(System.String):System.Void}

[<AbstractClass>]
type A() =
    abstract Item: int -> string with get, set
    default this.Item with get _ = 1
    default this.Item with set _ _ = ()

type B{caret}() =
  inherit A()
