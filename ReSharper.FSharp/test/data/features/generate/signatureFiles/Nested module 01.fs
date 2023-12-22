// ${KIND:SignatureFile}
// ${SELECT0:Generate signature file title}
module Foo

    /// comment that should be preserved
    [<System.Obsolete "foo">]
    module Bar =
        open System
{caret}
