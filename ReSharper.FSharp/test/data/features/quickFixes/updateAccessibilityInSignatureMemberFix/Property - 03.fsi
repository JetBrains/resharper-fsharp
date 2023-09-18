module A

type B =
    new: unit -> B
    member public Foo: int -> int with get
    member Foo: int -> int with set
