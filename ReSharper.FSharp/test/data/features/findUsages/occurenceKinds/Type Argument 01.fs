module Module

open System.Collections.Generic

let l: List<string> = List<string>()

match obj() with
| :? IList<string> -> ()
| :? IList<System.String> -> ()
| :? IList<string * int> -> ()
| _ -> ()
