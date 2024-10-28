namespace Ns1

[<AutoOpen>]
module Top =
    [<RequireQualifiedAccess>]
    type Union =
        | Case

namespace Ns2

module M =
    let u = Case{caret}
