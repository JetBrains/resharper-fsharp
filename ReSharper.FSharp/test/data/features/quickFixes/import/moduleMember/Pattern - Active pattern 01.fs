namespace Ns1

module Top =
    let (|Aaa|Bbb|) x = if x then Aaa else Bbb

namespace Ns2

module M =
    match () with
    | Aaa{caret} -> ()
