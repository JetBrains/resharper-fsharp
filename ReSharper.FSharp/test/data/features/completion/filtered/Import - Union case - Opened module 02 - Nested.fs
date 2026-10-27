// ${FILTER_ITEM:B}
module Module

module Outer =
    module Inner =
        type U =
            | A
            | B of int

open Outer.Inner

let x = {caret}
