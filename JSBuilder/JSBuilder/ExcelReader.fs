module ExcelReader

open StringUtils
open System.IO
open System.Text.RegularExpressions
open OfficeOpenXml

type IncludesConfig = {
    mutable BuildIncludes            :bool  
    mutable WebApplicationRootFolder :string    // Absolute path to root folder. 
    mutable RootScript               :string    // JavaScript application root - all references stem from this.
    mutable SourceTemplatePath       :string    // HTML template 
    mutable TargetHTMLFile           :string    // Target for completed template
    mutable SourceFolders            :string[]  // Folders containing JavaScript source.
    mutable IgnoreReferenceIn        :string[]  // Disregard any references in these files.
};

let readRowsFrom fileName tabName =
    let row cellref = Regex.Match(cellref, @"(\d+)").Groups.[1].Value
    let col cellref:string = Regex.Match(cellref, @"([A-Z])").Groups.[1].Value
    let getConfigItem (cells:seq<ExcelRangeBase>) = 
        let config = 
                {
                    BuildIncludes            = false
                    WebApplicationRootFolder = ""
                    RootScript               = ""
                    SourceTemplatePath       = ""
                    TargetHTMLFile           = ""
                    SourceFolders            = [||]
                    IgnoreReferenceIn        = [||]
                }
        cells 
        |> Seq.iter (fun cell -> match (cell.Address |> col) with
                                 | "A" -> config.BuildIncludes           <- cell.Value |> toString |> toLowercase = "y"
                                 | "B" -> config.WebApplicationRootFolder<- cell.Value |> toString
                                 | "C" -> config.RootScript              <- cell.Value |> toString
                                 | "D" -> config.SourceTemplatePath      <- cell.Value |> toString
                                 | "E" -> config.TargetHTMLFile          <- cell.Value |> toString
                                 | "F" -> config.SourceFolders           <- cell.Value |> toString |> split [|';'|]
                                 | "G" -> config.SourceFolders           <- cell.Value |> toString |> split [|';'|]
                                 | _ -> ()) // ignore this data.
        config
    use package = new ExcelPackage(new FileInfo(fileName))
    package.Workbook.Worksheets.["Includes"].Cells
    |> Seq.groupBy (fun cell -> cell.Address |> row)
    |> Seq.skip 1
    |> Seq.map(fun gp -> snd gp |> getConfigItem)


    
                    
    
