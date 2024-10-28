namespace Ns1

[<RequireQualifiedAccess>]
module Top =
    [<RequireQualifiedAccess>]
    type Union =
        | Case

namespace Ns2

module M =
    match Unchecked.defaultof<Ns1.Top.Union> with
    | Case{caret} -> ()
