﻿// ${COMPLETE_ITEM:with}
// ${OCCURRENCE:r2.F2}
module Module

type R1 =
    { F1: int }

type R2 =
    { F2: R1 }

let r1 = { F1 = 1 }
let r2 = { F2 = r1 }

r2.F2.{caret}
