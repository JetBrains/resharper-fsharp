module Module

let f: unit -> int = fun () -> 1

let s: string = f (){caret}
