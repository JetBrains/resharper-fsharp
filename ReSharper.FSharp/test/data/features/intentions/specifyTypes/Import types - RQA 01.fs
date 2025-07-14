namespace NS

[<RequireQualifiedAccess>]
module NS_A =
    [<RequireQualifiedAccess>]
    module NS_B =
        type R = { Field: System.Type }

namespace Test

module A =
    let x = { NS.NS_A.NS_B.R.Field = "".GetType() }

module B =
    open A

    let y{caret} = x
