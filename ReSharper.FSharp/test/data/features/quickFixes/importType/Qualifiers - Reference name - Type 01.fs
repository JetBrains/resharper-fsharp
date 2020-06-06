namespace Ns1

[<RequireQualifiedAccess>]
module Module1 =
    type T() = class end

namespace N2

module Nested =
    match null with
    | :? T{caret} -> ()
