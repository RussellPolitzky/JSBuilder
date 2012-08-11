module TemplateEngine

open System.Text.RegularExpressions

/// Given a template and a map of
/// substitutions, returns the completed
/// template.
let buildTemplateInstance 
    (replacements:Map<string,string>) 
    template = 
    let substitute key (template:string) replaceString = 
        let token = "{" + key + "}"
        template.Replace(token, replaceString)
    replacements
    |> Seq.fold (fun accum kvp -> 
                     let key = kvp.Key
                     let replacement = kvp.Value
                     substitute kvp.Key accum replacement) 
                     template




