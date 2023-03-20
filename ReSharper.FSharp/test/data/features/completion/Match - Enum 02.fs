// ${COMPLETE_ITEM:Match values}
namespace Ns1

type E =
    | A = 1
    | B = 2
    | C = 1

namespace Ns2

module Module =
    match Ns1.E.A with
    | {caret}
