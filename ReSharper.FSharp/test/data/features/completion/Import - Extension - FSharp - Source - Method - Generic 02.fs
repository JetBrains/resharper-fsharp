// ${COMPLETE_ITEM:Method (in Ns1.Module)}
namespace Ns1

open System.Collections.Generic

module Module =
    type IList<'T> with
        member this.Method() = ()

namespace Ns2

open System.Collections.Generic

module Module =
    let l: List<int> = null
    l.{caret}
