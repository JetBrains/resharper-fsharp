namespace Ns1

module ``Foo Bar`` =
    type T() = class end

namespace Ns2

module M =
    let t = T{caret}()
