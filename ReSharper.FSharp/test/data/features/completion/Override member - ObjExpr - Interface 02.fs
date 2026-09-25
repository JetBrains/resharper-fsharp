// ${COMPLETE_ITEM:member Dispose()}
module Module

let a =
    { new obj() with
        override x.ToString() = "..." 
      interface System.IDisposable with
        {caret} }