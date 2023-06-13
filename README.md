# SSlyc.CmdArgs

For Chinese version, click [here](./README_zh.md).

This package is a libary supporting arguments processing for CMD program.

## Overview

- Support position argument, single-character named argument, string named argument.
- Support multiple alias for one argument.
- Support required argument.
- Support 4 kinds of arguments: string, bool, integer, float. Support using default value（eg: -y）
- Support omitted `=`.（eg: --name=Sam, --name Sam）
- Support single-character argument combined. Only the last argument value can be assigned.（eg: -ltr, -rn 1, -rn=1)

## Feature

This package support 3 methods to provide argument in command line:

- **by potision**: Supply value by position
- **--**: Begin with `--`, provide the argument string alia, and assign the value or using default.
- **-**: Begin with`-`, provide the argument single-character alia, and assign the value or using default..

This package has these features:

- Support 4 kinds of arguments: `string`,`bool`,`int` and `double`. Each value can be `null`.
    - If an argument not appear in command line, the value will be `null`.
    - If an argument be supplied, but not providing value, the value will be default. That means `""`,  **`true`** , `0` or `0.0`.
- Following `--`, should be string alia for an argument. That means you can not use single-character alia after `--`. Each `--` provides an argument and value.
    - You can use `=value` to assign the value. Notice that there would not be any blank character surrounding `=`. eg: `--url=0.0.0.0:80`
    - You can use '` value`' to assign the value. Notice that there is a single space between argument alia and value. eg: `--url 0.0.0.0:80`
- Following `-`, should be a single-character alia for an argument. But it supports more than one character written following single `-`. Each character means one arguemnt. If you do this,
    - Only the last argument can assign value. This should use '` value`' grammer, `=` is not supported. eg: `-mau 0.0.0.0:80`, the value of argument `u` is `0.0.0.0:80`.
    - The arguments that not at the last place will use default value. eg: `-mau 0.0.0.0:80`, `m` and `a` will use default value. Be metion that `bool` type argument has a default value of **`true`**.
- You can give one argument many alias, including single-character alias and string alias.
- Possition argument will be process only by given order.  That means if a position argument can be omitted, you can only omit then from the tail to the head one by one.
    - For position argument omitted, you will alse get `null` value. But you can not omit a position argument in middle which has other arguments not omitted following it. That will cause mistake.
    - You can use `--` or `-` to supply a position argument . That would not get an error.
- When one argument supplied more than once, the last one will effect. This includes the situation that using `--` or `-` to supply a position argument.

## Usage

### Intro

1. Intialize Arguments object by ArgumentConfig.
2. Using Arguments object, processing args.
3. Invoke Process method, to get the result.

About Config:

1. Arguemnt should have a name, this can be use as its first alia. The main name can be either single-character or string. 
2. Argument can config any amount of long alia(string alia) and short alia(single-character alia), while long alia must be more than one character.

About Result:

1. You can use indexer to search the value. The argument not supplied will return you `null`.
2. You can use `.XXXValue` property or `.TryGetXXXValue` method to get the strong type value, while the value is not null.
    - `.XXXValue` may through exception
    - `.TryGetXXXValue` is very simple to `int.TryParse`
3. You must use `Name` to get the value for an argument, not the alias.

About help:

Arguments support printing help message by PrintUsage methoed. And it would auto work when receive `--help` or `-h` argument.

### Sample

The sample is using C# language.

First, config the Argument.

```c#
//Config
var configs = new List<ArgumentConfig>
{
    new ArgumentConfig
    {
        Name = "path",
        ByPosition = true,
        Description = "File path",
        LongAlias = new List<string>(),
        ShortAlias = new List<char>(),
        Type = ArgumentType.String
    },
    new ArgumentConfig
    {
        Name = "file",
        ByPosition = false,
        Description = "Force to replace",
        LongAlias = new List<string>(),
        ShortAlias = new List<char> { 'f' },
        Type = ArgumentType.Boolean
    },
    new ArgumentConfig
    {
        Name = "show-detail",
        ByPosition = false,
        Description = "Show details",
        LongAlias = new List<string>(),
        ShortAlias = new List<char> { 's' },
        Type = ArgumentType.Boolean
    }
};
//Initialize Arguments object with config
Arguments arguments = new Arguments(configs);
```

Process the argument in main method:

```c#
//Processing 
var values = arguments.Process(args);
//Get the values for arguments
var path = values["path"]?.StringValue ?? "";
var force = values["force"]?.BooleanValue ?? false;
var showDetail = values["show-detail"]?.BooleanValue ?? false;
```

If starting the cmd using these arguments:

```shell
some-bin /root/txt -fs
```

the value of path will be `/root/txt`,  the value of force and show-detail are `true`

If the argument is following:

```shell
some-bin -h
```

the program will print help messages and exit. The following code will not be executed

```txt
Usage: some-bin [options] path

path: File path

Options:
-h, --help: Print help messages.
-f, --force: Force to replace
-s, --show-detail: Show details
```
