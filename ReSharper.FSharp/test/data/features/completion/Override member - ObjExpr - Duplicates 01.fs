// ${ABSENT_ITEM:Dispose()}
module Module

let a =
    { new obj() with
        override x.ToString() = "..." 
      interface System.IDisposable with
        member this.Dispose() = ()
        member this.{caret} }