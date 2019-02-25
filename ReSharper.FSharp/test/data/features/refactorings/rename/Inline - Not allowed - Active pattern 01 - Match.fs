module Module
open System.Collections.Generic

match KeyValuePair<_,_>(1, 2) with
| KeyValue{caret} (k, v) -> ()
