// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

type A(a:int,b) =
    do printfn "%s" b
    member this.B() : int = 0
{caret}
