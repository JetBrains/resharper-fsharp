// ${FILTER_ITEMS:B}
module Module

module Outer =
    [<AutoOpen>]
    module Inner =
        type U =
            | A
            | B of int

open Outer

let x = {caret}
