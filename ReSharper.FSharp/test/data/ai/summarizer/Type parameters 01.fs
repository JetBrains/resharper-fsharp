open System.Collections.Generic

[<Interface>]
type A<'a, 'b when 'b :> IDisposable> =
    inherit IList<IList<string>>
    inherit IList<string list>
    inherit IList<(int * int) * int>
    inherit IList<string | null>
    inherit IList<'t>
    inherit IList<int -> int>
    inherit IList<{|Kek: int
                    Lol: string|}>
