// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

exception A of int * string with
    member a.B (c: string) = 0
{caret}
