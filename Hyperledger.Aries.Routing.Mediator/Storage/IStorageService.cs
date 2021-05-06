using Hyperledger.Aries.Decorators.Attachments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hyperledger.Aries.Routing.Mediator.Storage
{
    public interface IStorageService
    {
        Task<DateTimeOffset> StoreBackupAsync(string backupId, IEnumerable<Attachment> attachments);
        Task<List<Attachment>> RetrieveBackupAsync(string backupId);
        Task<IEnumerable<string>> ListBackupsAsync(string backupId);
        Task<List<Attachment>> RetrieveBackupAsync(string backupId, long timestamp);
    }
}