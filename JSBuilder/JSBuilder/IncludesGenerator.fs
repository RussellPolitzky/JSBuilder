module IncludesGenerator

open System.Reflection
open System.IO
open References
open ExcelReader
open PathUtils
open TemplateEngine

 
/// Populates a template and saves it to the 
/// target/output file.
let populateTemplate 
        fullPathToTemplate 
        fullPathToOutputFile
        includesSection = 
    let completedTemplate = 
        buildTemplateInstance 
            ([ "jsinclude", includesSection ] |> Map.ofList)
            (File.ReadAllText(fullPathToTemplate))
    File.WriteAllText(fullPathToOutputFile, completedTemplate) 


/// Processes config to produce a populated
/// template.  This function requires that 
/// includes builder as well as the template
/// populator functions be injected and is
/// not intended to be used directly.
let _processConfig 
    buildIncludesFor
    templatePopulator
    (config:IncludesConfig) = 
    if (config.BuildIncludes) then // only process this if asked to do so.
        let absWebApplicationRoot = 
            findParentHavingRoot 
                (getCodeBasePathOfAssembly (Assembly.GetExecutingAssembly())) // should find the web dir from the currently executing assembly
                config.WebApplicationRootFolder
        let fullTemplatePath = combinePaths absWebApplicationRoot config.SourceTemplatePath
        let fullOutputPath = combinePaths absWebApplicationRoot config.TargetHTMLFile
        buildIncludesFor config.RootScript (appendPathSep absWebApplicationRoot) config.IgnoreReferenceIn
        |> templatePopulator fullTemplatePath fullOutputPath


/// Processes config to produce a populated
/// template.  Takes a single IncludesConfig
/// parameter.
let processConfig = 
    _processConfig 
        buildIncludesSectionFor 
        populateTemplate


/// Entry point to this build task.
/// Produces HTML pages with populated 
/// includes as per the given configuration.
let processConfigurationsIn configFileName includesTabName = 
    readIncludesConfig configFileName includesTabName 
    |> Seq.iter (fun config -> processConfig config)




