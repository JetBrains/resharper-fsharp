namespace global

[<AutoOpen>]
module FSharpGlobalUtil =
    type Extension = System.Runtime.CompilerServices.ExtensionAttribute

    type System.Object with
        member x.As<'T when 'T: null>() =
            match x with
            | :? 'T as t -> t
            | _ -> null

    /// Reference equality.
    let inline (==) a b = LanguagePrimitives.PhysicalEquality a b

    /// Reference inequality.
    let inline (!=) a b = not (a == b)
    
    let someUnit = Some ()
