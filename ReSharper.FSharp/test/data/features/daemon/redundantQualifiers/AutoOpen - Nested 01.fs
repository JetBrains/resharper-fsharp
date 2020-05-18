module TopLevel =
    [<AutoOpen>]
    module Nested =
        type T() = class end

open TopLevel
let t: Nested.T = Nested.T()
