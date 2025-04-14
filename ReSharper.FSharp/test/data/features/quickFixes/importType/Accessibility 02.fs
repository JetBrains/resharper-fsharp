namespace Ns1

type internal T() =
    class
    end

namespace Ns2

module M =
    let t = T{caret}()
