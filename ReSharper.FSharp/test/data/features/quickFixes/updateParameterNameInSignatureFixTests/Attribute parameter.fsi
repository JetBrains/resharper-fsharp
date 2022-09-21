module Foo

type MyAttribute =
    inherit System.Attribute
    
    new: unit -> MyAttribute

val f: [<My>]x: int -> int