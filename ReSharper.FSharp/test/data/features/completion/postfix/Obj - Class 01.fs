﻿// ${COMPLETE_ITEM:new}
module Module

[<AbstractClass>]
type Base() =
    abstract P1: int
    abstract P2: int
    abstract P3: int

Base().{caret}
