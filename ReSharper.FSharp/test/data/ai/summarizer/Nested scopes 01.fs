namespace N.N1

open System

module M1 =
    module M2 =
        type T =
            interface IDisposable with
                member _.Dispose() = ()
