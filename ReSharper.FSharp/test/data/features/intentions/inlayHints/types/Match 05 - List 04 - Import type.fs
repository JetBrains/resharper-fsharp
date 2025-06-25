// ${BULB_TEXT:Add type annotation}

module Module

match ["".GetType()] with
| x :: (_ :: _{caret}) -> ()
