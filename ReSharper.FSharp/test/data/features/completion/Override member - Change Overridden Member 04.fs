// ${COMPLETE_ITEM:Equals(obj)}
module Module

let a =
    { new obj() with
        override x.ToString() = "" 
        override x.{caret} = 
            let a = 1
            let b = 2
            failwith "preserved" }
