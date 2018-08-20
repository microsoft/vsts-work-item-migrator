# Introduction 
WiMigrator is a command line tool designed with the following goals in mind:
* Migrate work items from one VSTS/TFS project to another
* Real world example of how to use the WIT REST APIs
* Cross platform support

![Build Status](https://vsts-wit.visualstudio.com/_apis/public/build/definitions/2a08f204-c80c-4f7e-82c8-f27e28f2becd/1/badge)

# Features
* Migrate the latest revision of a work item or set of work items based on the provided query, including:
  * Work item links (for work items within the query results set) 
  * Attachments
  * Git commit links (link to the source git commit)
  * Work item history (last 200 revisions as an attachment)
  * Tagging of the source items that have been migrated

# Getting Started
## Requirements
* Source Project on **VSTS** or **TFS 2017 Update 2** or later
* Target Project on **VSTS** or **TFS 2018** or later
* Personal access tokens or NTLM for authentication 
* Project Collection Administrator permissions required on target project
* Process metadata **should** be consistent between the processes
  * Limited field mapping support is provided to map fields from the source to target account
  * Area/Iteration paths can be defaulted to a specific value when they don't exist on the target

## Running
WiMigrator supports the following command line options:
* --validate validates that the metadata between the source and target projects is consistent 
* --migrate re-runs validation and then migrates the work items 

Migration runs in two parts:
* Validation
  * Configuration settings
  * Process metadata is consistent between projects
  * Identifies any work items that were previously migrated
* Migration
  * Phase 1: Work item fields
  * Phase 2: Attachments, links, git commit links, history, target move tag
  * Phase 3: Source move tag

A sample configuration file is provided with documentation of all the settings
* sample-configuration.json

Execution example:
```
dotnet run --validate configuration.json
```

## Limitations:
  * Artifact links (other than git) are not migrated
  * Board fields are not migrated
  * Test artifacts (e.g. test results) are not migrated

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
