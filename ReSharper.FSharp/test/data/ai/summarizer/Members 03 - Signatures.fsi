module Module

type Type =
    inherit ResizeArray<int>
    interface System.Collections.Generic.IEnumerable<string>

    new: unit -> Type
    member Method: x: int * y: int * int -> unit
    member Prop: int with get, set
