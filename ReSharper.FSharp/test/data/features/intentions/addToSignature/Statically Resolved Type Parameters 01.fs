module Foo

let inline fmap{caret} (f: ^a -> ^b) (a: ^a list) = List.map f a
