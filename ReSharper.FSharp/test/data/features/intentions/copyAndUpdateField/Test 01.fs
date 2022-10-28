module Program
open System
type InnerRecord =
    {
        Id : int
        Name : string
    }
type OuterRecord =
    {
        Inner : InnerRecord
    }
    
let innerRec = { Id = 1; Name = "john" }
let outerRec = { Inner = innerRec }


let newItem = outerRec.Inner.Name{caret}
