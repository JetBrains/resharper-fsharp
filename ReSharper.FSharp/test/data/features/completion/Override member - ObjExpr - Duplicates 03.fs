// ${ABSENT_ITEM:Dispose()}
module Module

let a =
    { new System.IDisposable with
        member x.Dispose() = failwith "..."
        member x.{caret} }