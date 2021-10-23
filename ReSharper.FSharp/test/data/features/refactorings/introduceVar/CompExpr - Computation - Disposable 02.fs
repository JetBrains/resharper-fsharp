//${OCCURRENCE0:Bind computation with let!}

let getCookie () : Async<System.IDisposable> = async { return null }

async {

    {selstart}getCookie (){selend}{caret}
    return 1
}
