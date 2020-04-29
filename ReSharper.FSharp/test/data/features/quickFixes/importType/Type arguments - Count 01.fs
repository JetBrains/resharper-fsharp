namespace Ns1

type T() = class end

namespace Ns2

type T<'T1>() = class end

namespace Ns3

type T<'T1, 'T2>() = class end

namespace Ns4

module Module =
    let t = T{caret}<_,_>()
