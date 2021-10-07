namespace Ns1.Ns2

type A() = class end

namespace Ns1.Ns3

type B() =
    inherit A{caret}()
