namespace HtmlIncludesTask

open References
open Microsoft.Build.Utilities 

/// Entry point for the build task.
/// This class is for the benefit of the 
/// build system .. so that it can find the 
/// build task.
type public HtmlIncludesTask =
    inherit Task
    new () = {} // Note the parameterless contructor here.
    override this.Execute()=
        buildIncludesSectionFor "" "" |> ignore
        true 