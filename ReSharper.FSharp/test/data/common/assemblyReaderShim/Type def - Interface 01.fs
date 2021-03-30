module Module

type IInherit1 =
    inherit IInterface

type IInherit2 =
    interface IInterface


type Type() =
    interface IInterface

let t = Type()
t :> IInterface |> ignore
t :> IBaseInterface |> ignore
