module Module

type T =
    { Field: int }

    static member op_Implicit(value: int) = { Field = value }
    static member op_Explicit(value: string) = { Field = int value }
