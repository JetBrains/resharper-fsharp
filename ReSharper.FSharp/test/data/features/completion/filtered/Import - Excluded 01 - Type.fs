// ${FILTER_ITEMS:T}
namespace Ns1

type T() = class end

namespace Ns2

type T() = class end

namespace Ns3

module M =
    let x = {caret}
