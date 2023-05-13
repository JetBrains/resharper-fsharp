// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

/// comment that should be preserved
[<System.Obsolete "foo">]
type A = delegate of int * string -> float
{caret}
