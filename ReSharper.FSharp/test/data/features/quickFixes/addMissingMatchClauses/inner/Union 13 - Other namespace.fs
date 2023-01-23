namespace Ns1

type U =
    | A
    | B
    | C

namespace Ns2

module Module =    
    match Ns1.U.A{caret} with
    | Ns1.U.A -> ()
