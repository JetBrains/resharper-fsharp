module Module

async {
    let! x{caret} = async { return () } in ()
}
