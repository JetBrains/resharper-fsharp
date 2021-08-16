// ${COMPLETE_ITEM:fun int -> string ->}
module Module

let f (a: int) (b: int -> string -> unit) = ()
let g = f 1

g {caret}
