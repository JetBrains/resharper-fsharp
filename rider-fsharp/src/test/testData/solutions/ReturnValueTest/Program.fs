
let f x =
    x + 1

let g x =
    x

let h f x =
    f x

let add a b =
    a + b

type T() =
    member this.Prop = 1

let t = T()

let l1 = []
let l2 = [1]
let l3 = [""; ""]
let l4 = l3 |> List.map _.Length

let i1 = l1.Length + l2.Length + l3.Length
let i2 = f 1
let i3 = f (1 + 1)
let i4 = f (System.Math.Max(1, 2))
let i5 = g 1
let i6 = t.Prop
let i7 = h ((+) 1) 1
let i8 = h (add 1) 1

let u1 = g ()
let u2 = id ()

let s1 = "123".Substring(1)
let s2 = "123".Substring(1).Substring(1)

let f1 = add 1

()
