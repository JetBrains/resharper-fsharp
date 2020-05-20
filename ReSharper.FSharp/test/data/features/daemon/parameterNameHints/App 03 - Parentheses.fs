open System.Drawing

let green = 234

Color.FromArgb((123), green, blue=100) |> ignore
Color.FromArgb(123, (green), blue=100) |> ignore
Color.FromArgb(123, green, blue=(100)) |> ignore
