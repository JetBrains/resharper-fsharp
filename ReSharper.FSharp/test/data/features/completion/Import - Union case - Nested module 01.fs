// ${COMPLETE_ITEM:B (in Module.Outer.Inner.U)}
module Module

module Outer =
    module Inner =
        type U =
            | A
            | B of int

open Outer

let x = {caret}
