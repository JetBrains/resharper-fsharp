namespace Ns1.Ns2

module Module =
    type A() = class end

namespace Ns1

type B() =
    inherit A{caret}()
