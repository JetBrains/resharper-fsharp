module Module

type T() =
    member val Prop{caret}: int = 1 with get, set

T().Prop
