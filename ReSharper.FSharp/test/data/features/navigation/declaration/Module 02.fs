module Module

module Nested =
    let x = 1

type Nested =
    { F: int }

open Nested{on}
