namespace Ns1

type T =
    { Field: int }

namespace Ns2

module Module =
    open Ns1

    type T with
        member x.Foo = 123
