open System.Diagnostics
open System

let GetArrayFromCmd dir command =
    use proc = new Process()
    let pi = ProcessStartInfo "cmd.exe"
    pi.WorkingDirectory <- dir
    pi.CreateNoWindow <- false
    pi.ErrorDialog <- false
    pi.UseShellExecute <- false
    pi.Arguments <- command
    pi.RedirectStandardOutput <- true
    pi.RedirectStandardError <- true
    proc.StartInfo <- pi
    let lst = ResizeArray()
    let handleData s =
        match s with
        | null -> ()
        | "" -> ()
        | (s:string) -> lst.Add(s)
    proc.OutputDataReceived.AddHandler(DataReceivedEventHandler(fun _ args -> handleData args.Data))
    proc.Start() |> ignore
    proc.BeginErrorReadLine()
    proc.BeginOutputReadLine()
    proc.WaitForExit()
    lst

let GetGitFiles dir = 
    let files = GetArrayFromCmd dir "cmd /c git ls-files --cached"
    files

let GetChangeCount dir filename =
    let files = GetArrayFromCmd dir $"cmd /c git log --follow --format=format: --name-only {filename}"
    files.Count, filename

let GetPath () =
    printfn "%s" "Enter path (Where the .git file is)."
    let path = Console.ReadLine()
    path

[<EntryPoint>]
let main argv =
    let mutable path = ""
    match argv.Length with
    | 1 -> path <- argv.[0]
    | _ -> path <- GetPath()
    
    printfn "%s" "Please wait."
    
    let files = GetGitFiles path
    let getChangeCountAsync filename = async {return GetChangeCount path filename}
    let ts = Stopwatch.GetTimestamp()
    
    files.ConvertAll(getChangeCountAsync)
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Array.sortByDescending (fun (i,_) -> i)
    |> Array.iter (fun (i, s) -> printfn "%d %s" i s)
    
    printfn "Elapsed time: %s" ((Stopwatch.GetElapsedTime ts).ToString())
    0
    