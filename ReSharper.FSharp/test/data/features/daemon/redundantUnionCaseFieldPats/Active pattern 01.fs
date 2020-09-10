open System.Collections.Generic

match KeyValuePair(1, 2) with
| KeyValue (_, _) -> ()

type KeyValue =
    | KeyValue of int * int

match KeyValue(1, 2) with
| KeyValue (_, _) -> ()
