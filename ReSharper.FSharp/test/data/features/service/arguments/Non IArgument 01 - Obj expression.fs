open System

let dispose (disp: IDisposable) = disp.Dispose()

{selstart}dispose { new IDisposable with override __.Dispose () = () }{selend}
