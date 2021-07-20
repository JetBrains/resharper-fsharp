let f () = ""

type System.String with
    member this.Id = this

("".[0])
ignore ("".[0])
ignore (f().[0])
ignore (f().Id.[0])
ignore (f().Id.Id.[0])
ignore ((f()).[0])
