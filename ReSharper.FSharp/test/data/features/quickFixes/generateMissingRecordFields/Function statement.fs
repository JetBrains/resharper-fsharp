type R = { A: int; B: int }
module foo =
    let r : (string option -> R) = function | Some _ -> {}{caret} | None -> {} 