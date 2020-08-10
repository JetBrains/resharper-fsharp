module Module

let a =
  let b{caret} = (
    let a = 1
    let b = 2

    a + b
  )

  ()
