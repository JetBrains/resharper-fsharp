module Module

[<Interface>]
type Interface =
    abstract member M: x: int -> unit

exception E1 of string
    with member x.NewMessage = ""

type Record = {
    Field1: int
    Field2: int
}

type DU =
    | Case1 of int
    | Case2

type Struct =
    struct
        val X: float
        val Y: float
        new(x: float, y: float) = { X = x; Y = y }
    end

type System.Collections.Generic.List<'a> with
    member x.Length = x.Count
