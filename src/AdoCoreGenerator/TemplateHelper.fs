namespace AdoCoreGenerator

open System.Collections.Generic
open System
open RazorEngine
open RazorEngine.Templating

type TemplateHelper() =
    let templates = Dictionary<string, string>()
    let compiledTemplates = Dictionary<string, bool>()

    member this.LoadTemplates(templatesToLoad: (string* string) seq) =
        templatesToLoad
        |> Seq.iter (fun (key, v) -> templates.Add(key, v) )

    member this.Generate(templateKey: string, model: Object) : string =
        if (compiledTemplates.ContainsKey(templateKey)) then
            Engine.Razor.Run(name = templateKey, model= model)
        else 
            Engine.Razor.RunCompile(templates.[templateKey], templateKey, null, model)

