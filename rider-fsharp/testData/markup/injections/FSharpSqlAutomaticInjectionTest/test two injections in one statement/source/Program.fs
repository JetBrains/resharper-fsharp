let f (s1: string) (s2: string) = ()
let g (s1: string, s2: string) = ()

let sql1 = f "Select * from sqlite_master" "select * from sqlite_master"
let sql2 = g ("Select * from sqlite_master", "select * from sqlite_master")
