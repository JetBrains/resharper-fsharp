// ${FILTER_ITEMS:Method}
namespace Ns1

module Module =
    type System.Int32 with
        member this.Method() = ()

namespace Ns2

module Module =
    type System.Int32 with
        member this.Method() = ()

namespace Ns3

module Module =
    let i = 1
    i.{caret}
