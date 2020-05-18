module Module

let i1{on} = 1
let a1{off} = 1 + 1
let t1{off} = 1, 1
let t2, t3{off} = 1, 1

let p1{on} = (1)
let p2{on} = ((1))

let l1{on} = 1L
let l2{on} = 1l

let b{on} = 1uy

let m1{off} = 1M
let m2{off} = 1m

let c1{on} = 'a'

let s1{on} = ""
let s2{on} = """"""
let s3{on} = @""
let s4{on} = @""""""
let s5{off} = ""B

let b1{on} = true
let b2{on} = true
