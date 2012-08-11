namespace HtmlIncludesTask

open Microsoft.Build.Utilities 
open Microsoft.Build.Framework 
open System.Diagnostics
open IncludesGenerator

/// Entry point for the build task.
/// This class is for the benefit of the 
/// build system .. so that it can find the 
/// build task.
type public HtmlIncludesTask() =
    inherit Task()
    
    // Note the auto-properties here.
    // Declare and initialise all in one line.
    [<Required>]
    member val RelativeConfigFilePath = "" with get, set

    [<Required>]
    member val IncludesTabName = "" with get, set
    
    override this.Execute()=
        // Uncomment to debug this build task.
        Debugger.Launch() |> ignore

        try
            processConfigurationsIn  
                this.RelativeConfigFilePath 
                this.IncludesTabName

            this.Log.LogMessage(
                MessageImportance.Low, 
                "HTML Includes successfully generated.");
            true
        with
            | e -> let stackTrace = e.ToString()
                   let message = sprintf "An Exception was thrown while building HTML includes: %A" stackTrace
                   this.Log.LogError(message)
                   false               
        



