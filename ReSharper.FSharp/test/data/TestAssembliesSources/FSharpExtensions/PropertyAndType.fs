[<AutoOpen>]
module NsPropAndType.Extensions

type System.String with
    member _.P with set _ = ()

type P() = class end
