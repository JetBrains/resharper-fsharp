module Declarations

type T =
    static member M(a: int, b: int) = ()

type C(a: int, b: int) = class end

type A(a: int, b: int) =
    inherit System.Attribute()

let f (a: int) (b: int) = ()
