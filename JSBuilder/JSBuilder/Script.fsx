
#load "FileUtils.fs"
#load "StringUtils.fs"
#load "SeqExtensions.fs"

open System.IO
open System.Text.RegularExpressions
open FileUtils
open StringUtils
open System.Diagnostics


let startInfo = new ProcessStartInfo()
let file1 = "C:\srce\FEF\Source\.gitignore"
let file2 = "C:\srce\FEF\Source\.gitignore"
startInfo.FileName <- @"C:\Program Files\Perforce\p4merge.exe"
startInfo.Arguments <- sprintf "%s %s" file1 file2
Process.Start(startInfo);

/// Opens a set of files in a diff viewer.
let openDiff file1 file2 =
    let startInfo = new ProcessStartInfo()
    startInfo.FileName <- @"C:\Program Files\Perforce\p4merge.exe"
    startInfo.Arguments <- sprintf "%s %s" file1 file2
    Process.Start(startInfo);

// Usage
//let file1 = "C:\srce\FEF\Source\.gitignore"
//let file2 = "C:\srce\FEF\Source\.gitignore"
openDiff file1 file2 

let stringsDiffer s1 s2 = 
    not (s1 = s1)

let _testStrings (tester:string->string->bool) s1 s2 = 
    Debugger.Launch() |> ignore
    if (tester s1 s2)
    then
        let tempFiles = [| Path.GetTempFileName(); Path.GetTempFileName() |] 
        Array.zip tempFiles [| s1; s2 |] 
        |> Array.iter (fun tpl -> File.WriteAllText((fst tpl), (snd tpl)))
        openDiff tempFiles.[0] tempFiles.[1] |> ignore
        
let testStrings = _testStrings stringsDiffer

testStrings "this" "that" 
