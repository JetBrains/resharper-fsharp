module A

type B =
    new: unit -> B
    member Foo: int with get
    member internal Foo: int with set
