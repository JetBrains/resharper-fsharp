namespace Ns1.Ns2

type T2() =
    class end

namespace Ns1

type T() =
    member this.M() =
        T2{caret}
