module Module

open System.Collections.Generic

let l: List<string> = List<string>()

match obj() with
| :? IList<string> -> ()
| :? IList<string[]> -> ()
| :? IList<string * int> -> ()
| _ -> ()
