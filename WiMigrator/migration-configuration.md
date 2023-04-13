# Source Connection
```source-connection``` The source account details

* ```account``` fully qualified url for the source account
* ```project``` name of the project to migrate from
* ```access-token``` PAT to use when accessing the account.
    * requires work item read permissions to all work items which need to be migrated.
    * note: personal access tokens require https
* ```use-integrate-auth``` when connecting to TFS, you can use NTLM instead of an access token.

# Target Connection
```target-connection``` The target account details

* ```account``` fully qualified url for the target account
* ```project``` name of the project to migrate to
* ```access-token``` PAT to use when accessing the account.
    * requires user is required to be a project collection administrator.
    * note: personal access tokens require https
* ```use-integrate-auth``` when connecting to TFS, you can use NTLM instead of an access token.

# Migration Options
#### ```query``` the name of the query to use for identifying work items to migrate. Note: query must be a flat
#### ```heartbeat-frequency-in-seconds``` the number of seconds in between logging the migration status. this gives the user a periodic update on the number of work items that have succeeded or failed the phases of migration. the default value is 30.


#### ```query-page-size``` the number of work item ids to return at a time when running the query.  This should be set to 20000 for hosted accounts and 2147483647 for onpremise accounts. 

#### ```parallelism``` the number of threads to run in parallel.  if omitted, defaults to the number of cores on the computer.

#### ```max-attachment-size``` the maximum size in bytes that an attachment can be. if it exceeds this size, it will be skipped. the default value is 60MB

#### ```link-parallelism``` the number of threads to run in parallel when processing links. if omitted, defaults to the number of cores on the computer.

#### ```attachment-upload-chunk-size``` the chunk size to use when uploading a work item in bytes. the default value is 1MB

#### ```skip-existing```  when true, the migration will not attempt to update any work items that have been previously migrated. when false, it will update any previously migrated work items that have changed on the source since the migration was completed.

#### ```move-history``` create a json file containing the updates of the source work item and attach it to the migrated work item.

#### ```move-history-limit```  the limit to the number of updates of the source work item to attach to the migrated work item.

#### ```move-git-links``` migrate git commit links as hyperlinks that point to the web view of the commit on the source account.


#### ```move-attachments``` migrate attachments

#### ```move-links``` preserve and migrate work item links from source to target if the linked work item is part of the current query or it has been previously migrated.

#### ```source-post-move-tag``` the tag to stamp on the work items on the source project once the migration is complete.

#### ```target-post-move-tag``` the tag to stamp on the work items on the target project once the migration is complete.

#### ```skip-work-items-with-type-missing-fields``` when true, will skip the work item if it's type does not have all fields present on the target account when false, will migrate the work item using only the matching fields present on the target account

#### ```skip-work-items-with-missing-area-path``` when true, will skip the work item if the area path does not exist in the target account when false, will migrate the work item and set the area path to the project name when the area path does not exist on the target account.

#### ```skip-work-items-with-missing-iteration-path``` when true, will skip the work item if the iteration path does not exist in the target account when false, will migrate the work item and set the iteration path to the project name when the iteration path does not exist on the target account.

#### ```default-area-path``` when the area path doesn't exist on the target project, the migrator will use this area path instead of defaulting to the root. note: if skip-work-items-with-missing-area-path is true, this setting is ignored.

#### ```default-iteration-path``` when the iteration path doesn't exist on the target project, the migrator will use this iteration path instead of defaulting to the root. note: if skip-work-items-with-missing-iteration-path is true, this setting is ignored.

#### ```area-path-mappings```  used to map areas paths from source to target
```
"area-path-mappings": {
  "Area 1\\Child A": "Area A\\Child 1",
  "Source Area 2": "Target Area 2"
}
```
#### ```iteration-path-mappings```  used to map iterations paths from source to target
```
"iteration-path-mappings": {
  "Iteration A": "Iteration 1"
}
```

#### ```clear-identity-display-names``` if the account has any identities with emojis, it's possible migration will fail if the identity with an emoji has not been added to the account. This setting will remove the display portion of the identity to ensure migration will succeed.

#### ```ensure-identities``` when true, will add any identities that are referenced by work items to the account, adding them to the Licensed Users group.  This applies only to VSTS, not TFS. when false, if any identity that is referenced by the work item does not exist it will be created as a non-identity value which can cause issues for query and in the case of special characters in the name the work item will fail to be migrated.

#### ```include-web-link``` when true, will include the web link in addition to the REST link for the source work item on the target.

#### ```hyper-links-excludes``` can be used to filter out hyperlinks based on the provided excluded words list. For example, the following config will exclude any hyperlink that contains google.com or yahoo.com from being migrated.

```
  "hyper-links-excludes": ["google.com", "yahoo.com"],
```
#### ```process-source-fields``` when true, will includes a preprocess based on more than 1 field on the source to create additional mappings rules. 

#### ```migrate-remote-link-as-hyperlink``` when true, will create remote links as hyperlink on Target. when true will create remote links as remote links on Target. Note: when false, links both items (source and target) to the remote link item.  

#### ```log-level-for-file``` minimum log level that will be logged to the file. if omitted, defaults to information. acceptable values from lowest to highest log level: trace, debug, information, warning, error, critical.

#### ```mapped-work-items```  used to list previously (or manually) mapped work items. this helps with linked work items that are not part of this migration.
```
"mapped-work-items": { "89312": 8567, "563360": 13444 },
```

#### ```static-fields```  used to indicate fields that should be filled with a literal value. In the example below, for the `Engagement` work item type, we're setting the `LeadByTeam` field to that value.
```
"static-fields": {
    "Engagement": { "LedByTeam": "Commercial Software Engineering" }
},
```

#### ```field-replacements```  this can be used to indicate for source field, we can specify an existing target field, a mapping can also be used to specify a map key that's defined in the `field-mappings` config. Finally, a work item type can be specified using the `specific-to-type` and also prefixing the source field name with that work item type.

```
"field-replacements": {
    "Custom.EngagementStatusReasonText": {
      "field-reference-name": "Custom.EngagementStatusReason"
    },
    "System.State": {
      "field-reference-name": "System.State",
      "mapping-name": "System.State.Mapping"
    },
	 "Engagement.System.Id": {
      "field-reference-name": "Custom.CSEADOLink",
      "specific-to-type": "Engagement"
    }
}
```


#### ```field-mappings```  used to provided mapping dictionaries that can be used to map literal values for certain fields in the `field-replacements` config.
```
 "field-mappings": {
    "System.State.Mapping": {
      "01 Exploration": "New",
      "On Hold": "On Hold",
      "02 Screen-in": "Active",
      "Completed": "Completed",
      "Removed": "Removed"
    }
  },
```


#### ```source-fields-processing```  used to provided mapping dictionaries that can be used to map literal values for certain fields in the `process-source-fields` config.
```
 "source-fields-processing": {
    "Custom.ResourceRequestType": {
      "fields": [
        "Custom.ResourceRequestType",
        "Custom.SMEType"
      ],
      "format": "{0};{1}",
      "specific-to-type": "Resource Request"
    }
 },
```


```send-email-notification``` when true, will send a run summary email if there are details in the Email Notification Message

##Email Notification 
This can be used to send a run summary email to one or more recipients. If used, please specify a valid smtp-server, one or more recipient-addresses, and the user-name and password if required by the smtp-server.

```
  "email-notification": {
    "smtp-server": "127.0.0.1",
    "use-ssl": false,
    "port": "25",
    "from-address": "wimigrator@example.com",
    "user-name": "un",
    "password": "pw",
    "recipient-addresses": [
      "test1@test.com",
      "test2@test.com"
    ]
  }
```