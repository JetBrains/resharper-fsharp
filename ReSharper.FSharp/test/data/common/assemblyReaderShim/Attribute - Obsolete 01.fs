module Module

let c1: ObsoleteClass = null
let c2 = ObsoleteClass()

let s1: ObsoleteStruct = ObsoleteStruct()
let s2 = ObsoleteStruct()

let i1: IObsoleteInterface = null
let i2 = { new IObsoleteInterface with
             member this.P = 1 }

let d1: ObsoleteDelegate = null
let d2 = ObsoleteDelegate(fun _ -> ())

let e1: ObsoleteEnum = Unchecked.defaultof<_>
let e2 = ObsoleteEnum()
let e3 = ObsoleteEnum.A

Class() |> ignore
Class(1) |> ignore

Class.ObsoleteField |> ignore
Class.ObsoleteMethod()
Class.ObsoleteProperty |> ignore
Class.ObsoleteEvent |> ignore

Enum.A |> ignore
