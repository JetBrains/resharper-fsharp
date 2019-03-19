module Module

type T =
    { Field: int }

    static member Instance = { Field = 1 }

    static member op_Equality(a: T, b) = a = b
    static member op_Inequality(a: T, b) = a <> b
