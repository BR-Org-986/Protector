using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Protector.Logic
{
    public interface IProtectorLogic
    {
        bool ValidateSignature(string payload, string signatureWithPrefix);
        Task AddBranchProtections(string defaultBranch, string repositoryName);
    }
}
