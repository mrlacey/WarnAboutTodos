# Warn About TODOs

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Download this extension from the [VS Gallery](https://visualstudiogallery.msdn.microsoft.com/[GuidFromGallery])

---------------------------------------

Visual Studio automatically takes code comments that include `TODO` and turns them into User Tasks that are displayed on the Task List.  
This extension takes those same tasks and also creates warnings for them.  
This can be useful if you don't look at the Task List or want an extra reminder to do something before commiting a change.

Works for single-line, multi-line, and documentation comments in both VB.Net and C#.

## Configuration

The default behavior is to create a warning about any comment line that starts with `TODO`.

This can be overriddedn by including an `AdditionalFile` in the project called **todo-warn.config**.
If this file exists, warnings will only be reported for comments that start with any of the non-blank lines in that file.

For example, if the config file contained the line `TODO BEFORE CHECK-IN`, only comments that start that way are reported.

![screenshot](./art/screenshot-filtered.png)
