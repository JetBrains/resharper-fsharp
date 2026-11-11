namespace Ns1

[<RequireQualifiedAccess>]
type Union =
    | Case

namespace Ns2

module M =
    let u = Case{caret}
