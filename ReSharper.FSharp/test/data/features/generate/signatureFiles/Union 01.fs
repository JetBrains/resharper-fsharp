// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

/// comment that should be preserved
[<System.Obsolete "foo">]
type Bar = | Bar of a:int * b:int
{caret}
