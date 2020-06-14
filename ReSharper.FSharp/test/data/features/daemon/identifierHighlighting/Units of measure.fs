[<Measure>] type m
[<Measure>] type s

type vector3D<[<Measure>] 'u> = { x : float<'u>; y : float<'u>; z : float<'u> }
let xvec : vector3D<m> = { x = 0.0<m>; y = 0.0<m>; z = 0.0<m> }

type vector3D2<'u> = { x : 'u; y : 'u; z : 'u }
let xvec2 : vector3D2<int> = { x = 0; y = 0; z = 0 }