module Foo

type Foo =
    new: unit -> Foo
    member Second: a:int * b:string -> c:int * d:int -> string
