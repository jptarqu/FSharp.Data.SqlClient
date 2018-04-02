namespace FSharp.Data

open System
open System.Collections.Generic
open System.Data
open System.Data.SqlClient
open System.Data.SqlTypes
open Microsoft.SqlServer.Server


open FSharp.Data.SqlClient


type internal RowType = {
    Provided: Type
    ErasedTo: Type
    
}

type internal ReturnType = {
    Single: Type
    PerRow: RowType option
}  with 
    member this.RowMapping = 
        match this.PerRow with
        | Some x -> x.Mapping
        | None -> Expr.Value Unchecked.defaultof<RowMapping> 
    member this.RowTypeName = 
        match this.PerRow with
        | Some x -> Expr.Value( x.ErasedTo.AssemblyQualifiedName)
        | None -> <@@ null: string @@>

type ColDef =
    {
        Name: string
        ClrType: string
    }
type RowDef =
    {
        Name: string
        Cols: ColDef list
    }

module Generator =
//    let GetDataRowType (columns: Column list) : RowDef = 
//        {
//            Name = "Row"
//            Cols = columns |> List.mapi(fun i col ->
//                       
//                                   if col.Name = "" then failwithf "Column #%i doesn't have name. Only columns with names accepted. Use explicit alias." (i + 1)
//                       
//                                   {
//                                       Name = col.Name
//                                       ClrType = col.
//                                   }
//                               )
//        }
//            
//    let GetOutputTypes (outputColumns: Column list, resultType, rank: ResultRank, hasOutputParameters) =    
//        if resultType = ResultType.DataReader 
//        then 
//            { Single = typeof<SqlDataReader>; PerRow = None }
//        elif outputColumns.IsEmpty
//        then 
//            { Single = typeof<int>; PerRow = None }
//        elif resultType = ResultType.DataTable 
//        then
//            let dataRowType = DesignTime.GetDataRowType(outputColumns, 
//            ?unitsOfMeasurePerSchema = unitsOfMeasurePerSchema)
//            let dataTableType = DesignTime.GetDataTableType("Table", dataRowType, outputColumns)
//            dataTableType.AddMember dataRowType
//
//            { Single = dataTableType; PerRow = None }
//
//        else 
//            let providedRowType, erasedToRowType, rowMapping = 
//                if List.length outputColumns = 1
//                then
//                    let column0 = outputColumns.Head
//                    let erasedTo = column0.ErasedToType
//                    let provided = column0.GetProvidedType(?unitsOfMeasurePerSchema = unitsOfMeasurePerSchema)
//                    let values = Var("values", typeof<obj[]>)
//                    let indexGet = Expr.Call(Expr.Var values, typeof<Array>.GetMethod("GetValue",[|typeof<int>|]), [Expr.Value 0])
//                    provided, erasedTo, Expr.Lambda(values,  indexGet) 
//
//                elif resultType = ResultType.Records 
//                then 
//                    let provided = DesignTime.GetRecordType(outputColumns, ?unitsOfMeasurePerSchema = unitsOfMeasurePerSchema)
//                    let names = Expr.NewArray(typeof<string>, outputColumns |> List.map (fun x -> Expr.Value(x.Name))) 
//                    let mapping = 
//                        <@@ 
//                            fun (values: obj[]) -> 
//                                let data = Dictionary()
//                                let names: string[] = %%names
//                                for i = 0 to names.Length - 1 do 
//                                    data.Add(names.[i], values.[i])
//                                DynamicRecord( data) |> box 
//                        @@>
//
//                    upcast provided, typeof<obj>, mapping
//                else 
//                    let erasedToTupleType = 
//                        match outputColumns with
//                        | [ x ] -> x.ErasedToType
//                        | xs -> Microsoft.FSharp.Reflection.FSharpType.MakeTupleType [| for x in xs -> x.ErasedToType |]
//
//                    let providedType = 
//                        match outputColumns with
//                        | [ x ] -> x.GetProvidedType()
//                        | xs -> Microsoft.FSharp.Reflection.FSharpType.MakeTupleType [| for x in xs -> x.GetProvidedType(?unitsOfMeasurePerSchema = unitsOfMeasurePerSchema) |]
//
//                    let clrTypeName = erasedToTupleType.FullName
//                    let mapping = <@@ Microsoft.FSharp.Reflection.FSharpValue.PreComputeTupleConstructor (Type.GetType(clrTypeName, throwOnError = true))  @@>
//                    providedType, erasedToTupleType, mapping
//            
//            let nullsToOptions = QuotationsFactory.MapArrayNullableItems(outputColumns, "MapArrayObjItemToOption") 
//            let combineWithNullsToOptions = typeof<QuotationsFactory>.GetMethod("GetMapperWithNullsToOptions") 
//            
//            { 
//                Single = 
//                    match rank with
//                    | ResultRank.ScalarValue -> providedRowType
//                    | ResultRank.SingleRow -> ProvidedTypeBuilder.MakeGenericType(typedefof<_ option>, [ providedRowType ])
//                    | ResultRank.Sequence -> 
//                   
//                        let collectionType = if hasOutputParameters then typedefof<_ list> else typedefof<_ seq>
//                        ProvidedTypeBuilder.MakeGenericType( collectionType, [ providedRowType ])
//                    | unexpected -> failwithf "Unexpected ResultRank value: %A" unexpected
//
//                PerRow = Some { 
//                    Provided = providedRowType
//                    ErasedTo = erasedToRowType
//                    Mapping = Expr.Call( combineWithNullsToOptions, [ nullsToOptions; rowMapping ]) 
//                }               
//            }
    let GetOutputColumns (connection: SqlConnection, commandText, parameters: Parameter list, isStoredProcedure) = 
                try
                    connection.GetFullQualityColumnInfo(commandText) 
                with :? SqlException as why ->
                    try 
                        let commandType = if isStoredProcedure then CommandType.StoredProcedure else CommandType.Text
                        connection.FallbackToSETFMONLY(commandText, commandType, parameters) 
                    with :? SqlException ->
                        raise why
                        
    let GenerateRoutinesCodes(conn: SqlConnection, schema, uddtsPerSchema, resultType, designTimeConnectionString, useReturnValue) =
        use c = conn.UseLocally()
        let isSqlAzure = conn.IsSqlAzure
        let routines = conn.GetRoutines( schema, isSqlAzure) 
        for routine in routines do
            printfn "\n snd routine.TwoPartName %s" (snd routine.TwoPartName)
//            let cmdProvidedType = ProvidedTypeDefinition(snd routine.TwoPartName, 
//                Some typeof<``ISqlCommand Implementation``>, HideObjectMethods = true)

            do
                routine.Description |> Option.iter (printfn "\n routine.Description %s")
            let parameters = conn.GetParameters( routine, isSqlAzure, useReturnValue)
            
            let commandText = routine.ToCommantText(parameters)
            printfn "\n commandText %s" commandText
            
            let outputColumns = GetOutputColumns(conn, commandText, parameters, routine.IsStoredProc)
            let rank = if routine.Type = ScalarValuedFunction then ResultRank.ScalarValue else ResultRank.Sequence

            let hasOutputParameters = parameters |> List.exists (fun x -> x.Direction.HasFlag( ParameterDirection.Output))

            printfn "\n Params:"
            for param in parameters do
                printfn "\n %s %s" param.Name (param.GetProvidedType().Name)
            printfn "\n Columns:"
            for col in outputColumns do
                printfn "\n %s %s" col.Name (col.GetProvidedType().Name)
                
//            let returnType = DesignTime.GetOutputTypes(outputColumns, resultType, rank, hasOutputParameters, unitsOfMeasurePerSchema)

//            do
//                SharedLogic.alterReturnTypeAccordingToResultType returnType cmdProvidedType resultType

            //ctors
//            let sqlParameters = Expr.NewArray( typeof<SqlParameter>, parameters |> List.map QuotationsFactory.ToSqlParam)
//
//            let designTimeConfig = 
//                let expectedDataReaderColumns = 
//                    Expr.NewArray(
//                        typeof<string * string>, 
//                        [ for c in outputColumns -> Expr.NewTuple [ Expr.Value c.Name; Expr.Value c.TypeInfo.ClrTypeFullName ] ]
//                    )
//
//                <@@ {
//                    SqlStatement = commandText
//                    IsStoredProcedure = %%Expr.Value( routine.IsStoredProc)
//                    Parameters = %%sqlParameters
//                    ResultType = resultType
//                    Rank = rank
//                    RowMapping = %%returnType.RowMapping
//                    ItemTypeName = %%returnType.RowTypeName
//                    ExpectedDataReaderColumns = %%expectedDataReaderColumns
//                } @@>
//
//            yield! DesignTime.GetCommandCtors(
//                cmdProvidedType, 
//                designTimeConfig, 
//                designTimeConnectionString,
//                config.IsHostedExecution
//            )
//
//            let executeArgs = DesignTime.GetExecuteArgs(cmdProvidedType, parameters, uddtsPerSchema, unitsOfMeasurePerSchema)
//
//            yield upcast DesignTime.AddGeneratedMethod(parameters, hasOutputParameters, executeArgs, cmdProvidedType.BaseType, returnType.Single, "Execute") 
//
//            if not hasOutputParameters
//            then
//                let asyncReturnType = ProvidedTypeBuilder.MakeGenericType(typedefof<_ Async>, [ returnType.Single ])
//                yield upcast DesignTime.AddGeneratedMethod(parameters, hasOutputParameters, executeArgs, cmdProvidedType.BaseType, asyncReturnType, "AsyncExecute")
//
//            if returnType.PerRow.IsSome
//            then
//                let providedReturnType = ProvidedTypeBuilder.MakeGenericType(typedefof<_ option>, [ returnType.PerRow.Value.Provided ])
//                let providedAsyncReturnType = ProvidedTypeBuilder.MakeGenericType(typedefof<_ Async>, [ providedReturnType ]) 
//
//                if not hasOutputParameters
//                then
//                    yield upcast DesignTime.AddGeneratedMethod(parameters, hasOutputParameters, executeArgs, cmdProvidedType.BaseType, providedReturnType, "ExecuteSingle") 
//                    yield upcast DesignTime.AddGeneratedMethod(parameters, hasOutputParameters, executeArgs, cmdProvidedType.BaseType, providedAsyncReturnType, "AsyncExecuteSingle")
//     