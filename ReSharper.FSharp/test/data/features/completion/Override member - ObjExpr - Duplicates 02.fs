// ${ABSENT_ITEM:Equals(obj)}
module Module

let a =
    { new obj() with
        override x.Equals(o) = failwith "..." 
        override x.{caret} }