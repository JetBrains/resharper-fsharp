// ${COMPLETE_ITEM:Dispose}
module Module

open System

type T() =
    override this.ToString() =
        let d: IDisposable = null
        d.{caret}

