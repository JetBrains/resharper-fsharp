// ${COMPLETE_ITEM:override A1(int, int)}
module Foo

type A() =
    abstract A1: x:int -> unit
    default _.A1 (x: int) = ()
    abstract A1: x:int * y:int -> unit
    default _.A1 (x:int, y:int) = ()

type B () =
    inherit A()
    {caret}
    member val B1 : string = "foo"