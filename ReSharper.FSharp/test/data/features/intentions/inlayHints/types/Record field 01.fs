// ${BULB_ITEM:Add type annotation}

module Module

type R = { A: int }

let f { A = a{caret} } = ()
