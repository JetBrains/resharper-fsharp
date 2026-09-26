// ${ABSENT_ITEM:ToString()}
module Module

let a =
    { new obj() with
        override x.ToString() = "..." 
      interface System.IDisposable with
        member this.{caret} }