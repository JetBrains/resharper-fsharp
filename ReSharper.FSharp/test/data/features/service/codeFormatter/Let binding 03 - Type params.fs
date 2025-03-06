module Module

let inline isNull<'T when 'T: not struct> (x: 'T) =
    ()
