// ${COMPLETE_ITEM:Method (in Ns1.Module)}
namespace Ns1

open System

module Module =
    type Int32 with
        member this.Method() = ()

namespace Ns2

module Module =
    let i = 1
    i.{caret}
