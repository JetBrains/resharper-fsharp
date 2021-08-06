// ${COMPLETE_ITEM:x (make recursive)}
module Module

do
    let x _ =
        {caret}
        let loop x =
            let loop = 1
            ()
        ()
    ()
