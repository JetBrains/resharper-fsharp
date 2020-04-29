module Module

let i1{on} = 1
let i2 as i3{on} = 1
let (i4{on}) = 1
let i5, i6{off} = 1, 2

let (|Id|) x = x + 1
let (Id i7{off}) = 1
