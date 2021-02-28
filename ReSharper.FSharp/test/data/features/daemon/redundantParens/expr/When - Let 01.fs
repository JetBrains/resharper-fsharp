module M

match () with
| _ when
    (let x = 1
     x) -> ()

| _ when
    (let x = 1
     true
     true) -> ()

| _ when
    (let x = 1
     true :? bool) -> ()

| _ when
    let x = 1
    (true :? bool) -> ()

| _ when
    let x = 1
    (true :? bool) && true -> ()

| _ when
    let x = 1
    true && (true :? bool) -> ()

| _ when
    let x: int = 1
    true -> ()

| _ when
    let x: bool = (true :? bool)
    x -> ()

| _ when
    (let x: bool = ()
     x) -> ()

| _ when let x: bool = ()
         x -> ()

| _ when (let x: bool = ()
          x) -> ()
