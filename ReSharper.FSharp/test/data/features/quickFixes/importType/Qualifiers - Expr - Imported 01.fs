namespace Ns1

[<RequireQualifiedAccess>]
module Module1 =
    type T() = class end

namespace N2

open Ns1

module Nested =
    let t = T{caret}()
