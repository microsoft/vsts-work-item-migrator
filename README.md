# Introduction 
WiMigrator is a command line tool designed with the following goals in mind:
* Migrate work items from one VSTS/TFS project to another
* Real world example of how to use the WIT REST APIs
* Cross platform support

# Features
* Migrate the latest revision of a work item, including:
  * Work item links
  * Attachments
  * Git commit links (link to the source git commit)
  * Work item history (last 200 revisions as an attachment)

# Getting Started
## Requirements
* Source Project on **VSTS** or **TFS 2017 Update 2**
* Target Project on **VSTS** or **TFS 2018**
* Personal access tokens or NTLM for authentication 
* Project Collection Administrator permissions required on target project
* Process metadata **should** be consistent between the processes
  * Limited field mapping support is provided to map fields from the source to target account
  * Area/Iteration paths can be defaulted to a specific value when they don't exist on the target

## Running
WiMigrator supports the following command line options:
* -v validates that the metadata between the source and target projects is consistent 
* -m re-runs validation and then migrates the work items 

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
dotnet run -v configuration.json
```

## Limitations:
  * Artifact links (other than git) are not migrated
  * Board fields are not migrated

