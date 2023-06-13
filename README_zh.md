# SSlyc.CmdArgs

本包为命令行扩展库，提供命令行参数解析功能

## 概览

- 支持位置参数、单字符参数、多字符参数。
- 支持多别名。任何参数都可以拥有任意多个单字符和多字符参数别名。
- 支持必填参数。
- 支持4种类型的参数：字符串、布尔值、整数、小数。支持布尔值参数省略参数值。（例如-y）
- 参数与值之间的`=`可省略。（例如：--name=Sam，--name Sam）
- 支持单字符参数的合并，仅可指定最后一个参数值（例如：-ltr，-rn 1, -rn=1)

## 特性

根据配置解析命令行参数。

命令行参数分以下几种：

- **位置参数**： 通过位置确定参数值
- **--参数**： 通过`--`开头的、长度大于一个字符的参数
- **-参数缩写**：通过`-`开头的、长度为一个字符的参数

目前支持如下特性：

- 参数支持四种类型：`string`、`bool`、`int`和`double`，所有值都是可null的。
    - 参数未指定，则参数值为`null`
    - 指定参数，但是不提供值时，默认分别为 `""`、 **`true`** 、 `0` 和 `0.0`
- `--`后面仅支持多个字符的参数，每个`--`指定一个参数。`--`提供的参数值可以由两种方式指定：
    - 直接在后面接`=值`，参数名和空格之间不允许有空格。例如：`--url=0.0.0.0:80`
    - 在参数后添加空格，然后直接书写值。此时不能有`=`号。例如：`--url 0.0.0.0:80`
- `-`后面可以跟多个字符，每个字符都被视作一个参数。如果书写多个字符，则：
    - 只有最后一个字符对应的参数，才能指定值。指定值的方式与`--`参数的第二种相同，不支持`=`指定。例如：`-mau 0.0.0.0:80`，则`u`的值为`0.0.0.0:80`。
    - 不在最后一位的字符参数只能使用默认值。例如：`-mau 0.0.0.0:80`，`m`和`a`都使用默认值。注意`bool`类型的默认值是`true`。
- 同一个参数可以支持任意多个单字符和多字符的别名，他们完全等效。
- 位置参数按照指定时的顺序解析，注意位置参数如果非必填，只能从后往前省略。
    - 如果省略位置参数，你同样会得到对应参数的`null`结果。但注意，位置参数的省略只能从右往左，无法直接省略中间的位置参数，否则会导致错位。
    - 位置参数仍然可以通过`--`或者`-`指定。
- 参数支持多次指定，根据指定顺序决定优先级，后指定的会覆盖之前的指定。这也包括位置参数同时被`--`或者`-`指定的情况。

## 用法

### 说明

1. 通过配置项初始化Arguments类
2. 使用Arguments对象，解析命令行参数列表args
3. 调用Process方法，获取解析结果。

关于配置项：

1. 配置项必须有一个Name，作为主参数名，可以是单个字符，也可以是多个字符。单个字符则用`-`指定，多个则用`--`。
2. 配置项可以包含多个长别名，和多个短别名。长别名必须大于1个字符，采用`--`指定。短别名只能是一个字符，使用`-`指定。

关于解析结果：

1. 对解析的结果，可以通过索引器查询需要的值，未指定的参数会返回null。
2. 对于非null的值，可以使用`.XXXValue`属性或者`.TryGetXXXValue`方法获取具体类型的参数值。
    - `.XXXValue`如果不是该类型会抛出异常
    - `.TryGetXXXValue`类似`int.TryParse`，会返回是否成功，并通过out传出值。
3. 解析结果中参数值始终通过提供的配置项的Name来访问，不适用别名。

关于打印：

Arguments对象还提供了PrintUsage方法，自动打印用法。并且会在接收到`--help`和`-h`参数时，自动终止程序，并打印用法。

### 示例

此处使用C#代码举例。

首先定义参数配置：

```c#
//定义配置
var configs = new List<ArgumentConfig>
{
    new ArgumentConfig
    {
        Name = "path",
        ByPosition = true,
        Description = "文件路径",
        LongAlias = new List<string>(),
        ShortAlias = new List<char>(),
        Type = ArgumentType.String
    },
    new ArgumentConfig
    {
        Name = "file",
        ByPosition = false,
        Description = "强制覆盖",
        LongAlias = new List<string>(),
        ShortAlias = new List<char> { 'f' },
        Type = ArgumentType.Boolean
    },
    new ArgumentConfig
    {
        Name = "show-detail",
        ByPosition = false,
        Description = "显示详细信息",
        LongAlias = new List<string>(),
        ShortAlias = new List<char> { 's' },
        Type = ArgumentType.Boolean
    }
};
//使用配置初始化
Arguments arguments = new Arguments(configs);
```

在程序加载时进行解析：

```c#
//解析
var values = arguments.Process(args);
//取值
var path = values["path"]?.StringValue ?? "";
var force = values["force"]?.BooleanValue ?? false;
var showDetail = values["show-detail"]?.BooleanValue ?? false;
```

如果通过下列命令调用cmd：

```shell
some-bin /root/txt -fs
```

则path的值为/root/txt， force和show-detail为true

如果使用：

```shell
some-bin -h
```

则会打印用法并退出cmd，不会执行主体代码

```txt
Usage: some-bin [options] path

path: 文件路径

Options:
-h, --help: Print help messages.
-f, --force: 强制覆盖
-s, --show-detail: 显示详细信息
```
