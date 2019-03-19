module Module

open System.Runtime.InteropServices

type T() =
    static member Method([<Optional; DefaultParameterValue(null: string)>] s: string) = 123
