// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

type R =
    { F: int }
    member _.Func (?x:int -> int) = 23
{caret}
