namespace Ns1

module Top =
    [<CompiledName "F">]
    let f x = 1

namespace Ns2

module M =
    let i = f{caret}
