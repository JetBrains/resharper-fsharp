module TopLevel

module Nested1 =
    [<AutoOpen>]
    module Nested2 =
        type T() = class end

open Nested1
let t: Nested1.Nested2.T = Nested1.Nested2.T()
