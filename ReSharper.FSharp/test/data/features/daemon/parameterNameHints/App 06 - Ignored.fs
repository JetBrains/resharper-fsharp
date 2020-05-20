open System.IO

DirectoryInfo(path="") |> ignore
DirectoryInfo("").Delete(recursive=true)
DirectoryInfo("").Delete (recursive=true)
