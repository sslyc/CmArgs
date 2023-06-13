namespace SSlyc.CmdArgs

open System

///参数类型
type ArgumentType =
    ///字符串型
    | String = 0
    ///布尔型
    | Boolean = 1
    ///整数型
    | Integer = 2
    ///浮点数型
    | Double = 3
    
///可区分联合参数值
type ArgumentValue =
    private
    ///字符串参数值
    | String of s: string //默认值为""
    ///布尔参数值
    | Boolean of b: bool //默认值为true
    ///整数参数值
    | Integer of i: int //默认值为0
    ///实数参数值
    | Double of d: double //默认值为0.0

    ///获取字符串值
    member this.StringValue =
        let (String s) = this
        s

    ///获取布尔值
    member this.BooleanValue =
        let (Boolean b) = this
        b
        
    ///获取整数值
    member this.IntegerValue =
        let (Integer i) = this
        i

    ///获取浮点值
    member this.DoubleValue =
        let (Double d) = this
        d

    ///尝试获取字符串值
    member this.TryGetStringValue(stringValue: outref<string>) =
        match this with
        | String(s) -> stringValue <- s; true
        | _ -> false

    ///尝试获取布尔值
    member this.TryGetBooleanValue(boolValue: outref<bool>) =
        match this with
        | Boolean(b) -> boolValue <- b; true
        | _ -> false

    ///尝试获取整数值
    member this.TryGetIntegerValue(intValue: outref<int>) =
        match this with
        | Integer(i) -> intValue <- i; true
        | _ -> false

    ///尝试获取浮点值
    member this.TryGetDoubleValue(doubleValue: outref<double>) =
        match this with
        | Double(d) -> doubleValue <- d; true
        | _ -> false

///参数配置
[<NoComparison; NoEquality>]
[<CLIMutable>]
type ArgumentConfig =
    { 
        ///主参数名
        Name: string
        ///帮助描述
        Description: string
        ///别名字串
        LongAlias: seq<string>
        ///短别名
        ShortAlias: seq<char>
        ///参数类型
        Type: ArgumentType
        ///是否位置参数
        ByPosition: bool 
    }

///封装解析结果，避免返回FSharMap和null值
type ArgumentValueMap
    ///<summary>通过map初始化</summary>
    ///<param name="map">分析结果的FSharMap</param>
    internal (map: Map<string, ArgumentValue option>) =

    ///通过Key访问
    member this.Item 
        with get(key) =
            match map[key] with
            | None -> Unchecked.defaultof<_>
            | Some(x) -> x

///内部实现模块
module private Arguments =

    ///将配置项转为map，便于使用
    let transConfigMap (configOrigin: seq<ArgumentConfig>) =
        seq {
            for c in configOrigin do
                c.Name, c
                for alia in c.LongAlias do
                    if alia.Length <= 1 then failwith "Invalid config: an alia should have at least 2 character"
                    alia, c
                for shortalia in c.ShortAlias do
                    shortalia.ToString(), c
        }
        |> Map.ofSeq

    ///处理命令行参数
    let processCmdArgs (configOrigin: seq<ArgumentConfig>) positionConfig configMap (args: string[]) =
        //取一个参数的值
        let getConfigValue (configMap: Map<string, ArgumentConfig>) name value =
            //不存在的key，则直接报错
            if not (configMap.ContainsKey(name)) then failwithf "Invalid input: '%s' is unknown argument" name
            else
                //根据类型获取值，或者抛出异常
                match configMap[name].Type with
                | ArgumentType.String ->
                    match value with
                    | Some(x) -> Some(String(x))
                    | None -> Some(String(""))
                | ArgumentType.Boolean ->
                    match value with
                    | Some(x) ->
                        match x.ToLower() with
                        | "" | "true" -> Some(Boolean(true))
                        | "false" -> Some(Boolean(false))
                        | other -> failwithf "Invalid input: %s argument should be true/false，while provided %s" name other
                    | None -> Some(Boolean(true))
                | ArgumentType.Integer ->
                    match value with
                    | Some(x) -> Some(Integer(int x))
                    | None -> Some(Integer(0))
                | ArgumentType.Double ->
                    match value with
                    | Some(x) -> Some(Double(double x))
                    | None -> Some(Double(0.0))
                | _ ->
                    failwith "Invalid type of argument"
                |> fun x -> configMap[name].Name, x
        //递归处理参数
        let rec processCmdArgsRec (positionConfig: ArgumentConfig[]) configIndex configMap (args: string[]) argsIndex rsl =
            match argsIndex with
            | _ when argsIndex > args.Length - 1 ->
                rsl
            | _ -> 
                match args[argsIndex] with
                | full when full.StartsWith("--") ->
                    if full.Length <= 2 then failwithf "Invalid input: '--' without name"
                    if full.Length = 3 then failwithf "Invalid input: '%s' unknown" full
                    let name = full.Substring(2)
                    if name.Contains("=") then
                        let paramArr = name.Split('=')
                        if paramArr[0].Length <= 1 then failwithf "Invalid input: '--%s' unknown" paramArr[0]
                        if paramArr.Length > 2 then failwithf "Invalid input: %s argument take more than 1 '='" name
                        let stringVal = if paramArr.Length = 1 then None else Some paramArr[1]
                        processCmdArgsRec positionConfig configIndex configMap args (argsIndex + 1) (rsl @ [ getConfigValue configMap paramArr[0] stringVal ])
                    elif argsIndex <= args.Length - 2 && not (args[argsIndex + 1].StartsWith("-")) then 
                        processCmdArgsRec positionConfig configIndex configMap args (argsIndex + 2) (rsl @ [ getConfigValue configMap name (Some args[argsIndex + 1]) ])
                    else 
                        processCmdArgsRec positionConfig configIndex configMap args (argsIndex + 1) (rsl @ [ getConfigValue configMap name None ])
                | short when short.StartsWith("-") ->
                    if short.Length <= 1 then failwithf "Invalid input: '-' without name"
                    let nameArr = short.Substring(1) |> Seq.map (fun x -> x.ToString()) |> Array.ofSeq
                    let valueArr = 
                        [
                            for i in 0 .. nameArr.Length - 2 do
                                getConfigValue configMap nameArr[i] None
                        ]
                    if argsIndex <= args.Length - 2 && not (args[argsIndex + 1].StartsWith("-")) then
                        processCmdArgsRec positionConfig configIndex configMap args (argsIndex + 2) (rsl @ valueArr @
                            [ getConfigValue configMap nameArr[nameArr.Length - 1] (Some args[argsIndex + 1]) ])
                    else
                        processCmdArgsRec positionConfig configIndex configMap args (argsIndex + 1) (rsl @ valueArr @
                            [ getConfigValue configMap nameArr[nameArr.Length - 1] None ])
                
                | param ->
                    if configIndex > positionConfig.Length - 1 then failwithf "Invalid input: too many position argument"
                    processCmdArgsRec positionConfig (configIndex + 1) configMap args (argsIndex + 1) (rsl @ [ getConfigValue configMap (positionConfig[configIndex].Name) (Some param) ])
        let empty = [ for x in configOrigin do x.Name, None]
        let userSpec = processCmdArgsRec positionConfig 0 configMap args 0 []
        empty @ userSpec |> Map.ofList //返回参数值map

    ///打印使用说明
    let printUsage (config: seq<ArgumentConfig>) (positionConfig: ArgumentConfig[]) =
        printf "Usage: %s " (System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)
        printf "[options] "
        for pc in positionConfig do
            printf "%s " pc.Name
        printfn ""
        printfn ""
        for pc in positionConfig do
            printfn "%s: %s" pc.Name pc.Description
        printfn ""
        printfn "Options:"
        for c in config do 
            let nameComb = 
                seq {
                    if c.Name.Length = 1 then yield "-" + c.Name
                    yield! (c.ShortAlias |> Seq.map (fun x -> "-" + x.ToString()))
                    if c.Name.Length > 1 then yield "--" + c.Name
                    yield! (c.LongAlias |> Seq.map (fun x -> "--" + x))
                }
                |> Seq.reduce (fun pre cur -> pre + ", " + cur)
            printf "%s" nameComb
            printfn ": %s" c.Description
        printfn ""

open Arguments

///命令行参数工具
type Arguments
    ///<summary>使用配置初始化</summary>
    ///<param name="config">参数配置</param>
    (config) =

    //提取位置参数
    let positionConfigArray = Seq.filter (fun x -> x.ByPosition ) config |> Array.ofSeq
    //参数序列加help
    let configWithHelp = 
        seq { 
            //将help和h放在前，以防止覆盖用户指定
            yield { Name = "help"; Description = "Print help messages."; LongAlias = []; ShortAlias = ['h']; Type = ArgumentType.Boolean; ByPosition = false }
            yield! config 
        }
    //仅通过标签指定的参数列表
    let configWithHelpWithoutPosition = configWithHelp |> Seq.filter (fun x -> not x.ByPosition)
    //转为map，以便比对效率为O(log n)
    let configMap = transConfigMap configWithHelp

    ///<summary>处理命令行参数</summary>
    ///<param name="args">命令行参数数组</param>
    member this.Process(args) = 
        let values = processCmdArgs configWithHelp positionConfigArray configMap args
        match values["help"] with
        | Some x ->
            this.PrintUsage()
            exit 1
        | None ->
            //values 
            //|> Seq.map (fun i -> match i with | KeyValue (str, None) -> str, Unchecked.defaultof<_> | KeyValue (str, Some(x)) -> str, x)
            //|> Map.ofSeq
            //|> System.Collections.Generic.Dictionary<_, _>
            ArgumentValueMap(values)

    ///打印用法
    member this.PrintUsage() = printUsage configWithHelpWithoutPosition positionConfigArray
