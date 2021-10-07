namespace Ns1.Ns2

module Module =
    type A() = class end

type B() =
    inherit A{caret}()
