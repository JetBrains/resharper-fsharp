module Foo

type Teq<'a, 'b> =
    class
    end

let map{caret} (f: 'b -> 'c) (t: Teq<'a, 'b>) : Teq<'a, 'c> = failwith "todo"
