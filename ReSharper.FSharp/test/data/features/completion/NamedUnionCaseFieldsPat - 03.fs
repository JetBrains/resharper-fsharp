// ${COMPLETE_ITEM:banana}
// ${COMPLETE_ITEM:citrus}
module Foo

type Foo =
    | Meh of int * string
    | Bar of apple:int * banana: string * citrus: float

let a (b: Foo) =
    match b with
    | Bar(a = apple; {caret}) -> ()
