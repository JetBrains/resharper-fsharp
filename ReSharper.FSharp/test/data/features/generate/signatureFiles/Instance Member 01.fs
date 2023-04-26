// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

type X =
    {
        Y: int
    }
    member x.A b c = x.Y - b + c
{caret}
