type Record = { ``type``: int; foo: int }
type Record1 = { ``fun``: Record; foo: int }

let f item =
    { item with ``fun`` ={caret} { item.``fun`` with ``type`` = 3 } }
