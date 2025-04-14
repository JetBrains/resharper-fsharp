namespace Ns1

type private T() =
    class
    end

namespace Ns2

module M =
    let t = T{caret}()
