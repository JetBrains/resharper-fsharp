module Foo

type MyAttribute =
    new: unit -> MyAttribute
    inherit System.Attribute
    
[<Class>]
type Foo =
    static member Bar: ?x:int -> int