// ${BULB_TEXT:Add type annotation}

module Module

type R = { A: int }

let f { A = a{caret} } = ()
