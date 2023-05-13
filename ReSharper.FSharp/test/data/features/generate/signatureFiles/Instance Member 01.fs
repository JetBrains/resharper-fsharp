// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

type X =
    {
        Y: int
    }
    /// comment that should be preserved
    [<System.Obsolete "foo">]
    member x.A b c = x.Y - b + c
{caret}
