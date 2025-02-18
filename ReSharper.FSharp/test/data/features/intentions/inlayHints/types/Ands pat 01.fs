// ${BULB_ITEM:Add type annotation}

module Module

let (|Bool|) (x: int) = true

let f (x{caret} & Bool(_)) = ()
