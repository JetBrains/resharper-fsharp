let sayHello nameOpt =
    match nameOpt with
    | None -> "Hi there"
    | Some name -> "Hi, " + name
