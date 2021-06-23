module Module

let f<'a when 'a :> string> x =
    let _: 'a{caret} = x
    ()
