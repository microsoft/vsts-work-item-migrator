using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Common.Migration
{
    public static class MigrationHelpers
    {
        public static HashSet<string> GetInlineImageUrlsFromField(string fieldHtmlContent, string accountUrl)
        {
            HashSet<string> result = new HashSet<string>();

            if (fieldHtmlContent != null)
            {
                IList<string> inlineImageHtmlTagsForField = GetInlineImageHtmlTags(fieldHtmlContent, accountUrl);
                foreach (string inlineImageHtmlTag in inlineImageHtmlTagsForField)
                {
                    result.Add(GetUrlFromHtmlTag(inlineImageHtmlTag));
                }
            }

            return result;
        }

        public static IList<string> GetInlineImageHtmlTags(string input, string accountUrl)
        {
            string pattern = $@"<img[^<>]+?src=[""']({accountUrl}WorkItemTracking/v1.0/AttachFileHandler.ashx[^""']+?)[""'][^<>]*?>";
            MatchCollection matches = Regex.Matches(input, pattern);
            return matches.Cast<Match>().Select(m => m.Value).ToList();
        }

        public static string GetUrlFromHtmlTag(string input)
        {
            string pattern = @"(?<=src=\"")[^""]*(?="")";
            Match match = Regex.Match(input, pattern);
            return match.Value;
        }

        public static Guid GetAttachmentUrlGuid(string url)
        {
            string pattern = @"(?<=FileNameGuid=)[a-zA-Z0-9-]*";
            Match match = Regex.Match(url, pattern, RegexOptions.IgnoreCase);
            Guid attachmentGuid;
            return Guid.TryParse(match.Value, out attachmentGuid) ? attachmentGuid : Guid.Empty;
        }

        public static string ReplaceAttachmentUrlGuid(string url, string newGuid)
        {
            string pattern = @"(?<=FileNameGuid=)[a-zA-Z0-9-]*";
            return Regex.Replace(url, pattern, newGuid, RegexOptions.IgnoreCase);
        }

        public static JsonPatchOperation GetJsonPatchOperationAddForField(KeyValuePair<string, object> field)
        {
            string key = field.Key;
            object value = field.Value;

            JsonPatchOperation jsonPatchOperationAdd = new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = $"/{Constants.Fields}/{key}",
                Value = value
            };

            return jsonPatchOperationAdd;
        }

        public static JsonPatchOperation GetJsonPatchOperationReplaceForField(KeyValuePair<string, object> field)
        {
            string key = field.Key;
            object value = field.Value;

            JsonPatchOperation jsonPatchOperationAdd = new JsonPatchOperation()
            {
                Operation = Operation.Replace,
                Path = $"/{Constants.Fields}/{key}",
                Value = value
            };

            return jsonPatchOperationAdd;
        }

        public static JsonPatchOperation GetHyperlinkAddOperation(string hyperlink, string comment, object id)
        {
            JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();
            jsonPatchOperation.Operation = Operation.Add;
            jsonPatchOperation.Path = $"/{Constants.Relations}/-";
            jsonPatchOperation.Value = new
            {
                rel = Constants.Hyperlink,
                url = hyperlink,
                attributes = new
                {
                    id = id,
                    comment = comment
                }
            };

            return jsonPatchOperation;
        }

        public static JsonPatchOperation GetHyperlinkAddOperation(string hyperlink, string comment)
        {
            JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();
            jsonPatchOperation.Operation = Operation.Add;
            jsonPatchOperation.Path = $"/{Constants.Relations}/-";
            jsonPatchOperation.Value = new
            {
                rel = Constants.Hyperlink,
                url = hyperlink,
                attributes = new
                {
                    comment = comment
                }
            };

            return jsonPatchOperation;
        }

        public static JsonPatchOperation GetRelationAddOperation(WorkItemRelation relation)
        {
            JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();
            jsonPatchOperation.Operation = Operation.Add;
            jsonPatchOperation.Path = $"/{Constants.Relations}/-";
            jsonPatchOperation.Value = relation;

            return jsonPatchOperation;
        }

        public static JsonPatchOperation GetRevisionHistoryAttachmentAddOperation(AttachmentLink attachmentLink, int workItemId)
        {
            JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();
            jsonPatchOperation.Operation = Operation.Add;
            jsonPatchOperation.Path = $"/{Constants.Relations}/-";
            jsonPatchOperation.Value = new WorkItemRelation
            {
                Rel = Constants.AttachedFile,
                Url = attachmentLink.AttachmentReference.Url,
                Attributes = new Dictionary<string, object>
                {
                    {  Constants.RelationAttributeName, attachmentLink.FileName },
                    {  Constants.RelationAttributeResourceSize,  attachmentLink.ResourceSize },
                    {  Constants.RelationAttributeComment,  attachmentLink.Comment }
                }
            };

            return jsonPatchOperation;
        }
        
        public static JsonPatchOperation GetRelationRemoveOperation(int existingRelationIndex)
        {
            JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();
            jsonPatchOperation.Operation = Operation.Remove;
            jsonPatchOperation.Path = $"/{Constants.Relations}/{existingRelationIndex}";

            return jsonPatchOperation;
        }

        public static string GetCommentFromAttributes(WorkItemRelation relation)
        {
            if (relation.Attributes != null && relation.Attributes.ContainsKeyIgnoringCase(Constants.RelationAttributeComment))
            {
                // get the key even if its letter case is different but it matches otherwise
                string commentKeyFromFields = relation.Attributes.GetKeyIgnoringCase(Constants.RelationAttributeComment);
                return relation.Attributes[Constants.RelationAttributeComment].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
              
        public static JsonPatchOperation GetWorkItemLinkAddOperation(IMigrationContext migrationContext, WorkItemLink workItemLink)
        {
            string workItemEndpoint = ClientHelpers.GetWorkItemApiEndpoint(migrationContext.Config.TargetConnection.Account, workItemLink.Id);

            JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();
            jsonPatchOperation.Operation = Operation.Add;
            jsonPatchOperation.Path = $"/{Constants.Relations}/-";
            jsonPatchOperation.Value = new
            {
                rel = workItemLink.ReferenceName,
                url = workItemEndpoint,
                attributes = new
                {
                    comment = workItemLink.Comment
                }
            };

            return jsonPatchOperation;
        }
    }
}
