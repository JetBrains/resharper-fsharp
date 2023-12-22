// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

/// comment that should be preserved
[<System.Obsolete "foo">]
exception A of int * string
{caret}
