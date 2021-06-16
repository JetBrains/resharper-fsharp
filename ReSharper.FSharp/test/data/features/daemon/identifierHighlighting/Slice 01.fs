#nowarn "57"

let l = []

l.[^1]
l.[..1]
l.[1..]

let a: int[,] = Unchecked.defaultof<_>

a.[1, *]
a.[^1.., *]
a.[*, 1]
a.[*, ..^1]
a.[*, *]
a.[*]
