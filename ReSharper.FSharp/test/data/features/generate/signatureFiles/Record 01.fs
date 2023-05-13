// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

/// comment that should be preserved
[<System.Obsolete "foo">]
type Bar = { A:int; B: int }
{caret}
