module Module

type D<'T> = delegate of 'T -> 'T

let f<'T> (a: 'T) = Unchecked.defaultof<'T>
