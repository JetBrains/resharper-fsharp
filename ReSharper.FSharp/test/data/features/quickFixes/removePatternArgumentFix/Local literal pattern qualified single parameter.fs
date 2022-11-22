type E =
    | A = 1
    | B = 2

do
	let (E.A x{caret}) = E.A
	()
