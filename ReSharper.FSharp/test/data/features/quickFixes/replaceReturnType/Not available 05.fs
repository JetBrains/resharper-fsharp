type OptionBuilder() =
    member x.Bind(v,f) = Option.bind f v
    member x.Return v = Some v
    member x.ReturnFrom o = o
    member x.Zero () = None

let opt = OptionBuilder()

let v : int option = opt {
    let! x = (1{caret}: string option) in
    return (int) x
}
