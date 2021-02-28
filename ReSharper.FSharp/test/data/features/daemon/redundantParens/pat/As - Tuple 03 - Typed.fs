module Module

do let (a: int as b, c) = 1, 2 in ()
do let (a: int as b, c) as d = 1, 2 in ()

do let (a, b: int as c as d) = 1, 2 in ()
do let (a: int, b as c as d) = 1, 2 in ()

do let (a, b: int as c as d) as e = 1, 2 in ()
