namespace global

open System.Collections.Generic

type T<'T1>() =
     static member M<'T2, 'T3 when 'T2 :> IEnumerable<'T3>>() = 1
 