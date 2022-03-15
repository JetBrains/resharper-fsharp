// ${KIND:Overrides}
// ${SELECT0:Item():System.String}

[<AbstractClass>]
type A() =
    abstract Item: int -> string with get, set
    default this.Item with get _ = 1
    default this.Item with set _ _ = ()

type B{caret}() =
  inherit A()
