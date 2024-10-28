namespace Ns1

type Union =
    | Case

namespace Ns2

module M =
    match Unchecked.defaultof<Ns1.Union> with
    | Case{caret} -> ()
