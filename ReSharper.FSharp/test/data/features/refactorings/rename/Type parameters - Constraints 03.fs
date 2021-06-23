module Module

type A = int

type R<'T when 'T :> A> = class end

let f<'a when 'a :> A{caret}> () =
    ()
