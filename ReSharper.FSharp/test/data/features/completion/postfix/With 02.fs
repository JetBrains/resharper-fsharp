// ${COMPLETE_ITEM:with}
module Module

type R =
    { F: int }

let f () =
    { F = 1 }

f().{caret}
