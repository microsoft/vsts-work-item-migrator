using System;

namespace Common.Migration
{
    //Define where the migration of a workitem can go wrong
    [Flags]
    public enum FailureReason
    {
        None = 0,
        UnsupportedWorkItemType = 1 << 0,
        ProjectDifferentFromSource = 1 << 1,
        BadRequest = 1 << 2,
        CriticalError = 1 << 3,
        UnexpectedError = 1 << 4,
        AttachmentUploadError = 1 << 5,
        AttachmentDownloadError = 1 << 6,
        InlineImageUrlFormatError = 1 << 7,
        InlineImageUploadError = 1 << 8,
        InlineImageDownloadError = 1 << 9,
        DuplicateSourceLinksOnTarget = 1 << 10,
        CreateBatchFailureError = 1 << 11,
    }
}