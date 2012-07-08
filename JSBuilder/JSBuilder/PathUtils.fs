[<AutoOpen>]
module PathUtils

open System.Reflection
open System.IO
open System.Numerics
open System


/// Given absolute path, this function 
/// find the nth parent recursing 
/// upward.
let moveUp n absoluteDirectory = 
    let rec findParent dir counter =
        let parent = Directory.GetParent(dir).FullName
        if (counter > 0) 
        then findParent parent (counter-1)
        else parent
    findParent absoluteDirectory n
    

/// Given a root path and a relative one,
/// this function calculates the resulting,
/// absolute path.
let calculatePath rootPath relativePath =
    Path.GetFullPath(Path.Combine(rootPath,relativePath))


/// Finds one path relative to another.
/// see http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/954346c8-cbe8-448c-80d0-d3fc27796e9c/
let makePathRelativeTo absoluteRootPath absolutePath = 
    (new Uri(absolutePath))
        .MakeRelativeUri(new Uri(absoluteRootPath))
        .ToString();


/// Transforms a sequence of absolute paths from
/// to ones relative to the given absolute relative
/// path.
let makePathsRelativeTo absoluteRootPath (absolutePaths:seq<string>) = 
    absolutePaths 
    |> Seq.map (fun absolutePath ->
                    makePathRelativeTo absolutePath absoluteRootPath )


