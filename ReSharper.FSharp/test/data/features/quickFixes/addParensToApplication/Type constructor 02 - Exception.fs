//${ARGS_OCCURRENCE:f 1 2}
open System

let f a b = ""
ArgumentOutOfRangeException f 1{caret} 2
