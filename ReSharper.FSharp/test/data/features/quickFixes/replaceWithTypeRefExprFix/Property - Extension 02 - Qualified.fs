namespace Ns1

type A() = class end

namespace Ns2

type Ns1.A with
    static member P = 1

module Module =
    let a = Ns1.A()
    a.P{caret}
