using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Protector.Logic
{
    /// <summary>
    /// Contains all the logic for handling the branch protections
    /// </summary>
    public class ProtectorLogic : IProtectorLogic
    {
        private static InMemoryCredentialStore credentials;
        private static GitHubClient client;
        private readonly IConfiguration Configuration;
        private string UserName;
        private string Organization;

        /// <summary>
        /// Setup our GitHubClient in the constructor to be used
        /// </summary>
        /// <param name="_configuration">Configuration key/values from appsettings</param>
        public ProtectorLogic(IConfiguration _configuration)
        {
            Configuration = _configuration;
            UserName = Configuration["OrgOwner"];
            Organization = Configuration["Organization"];
            credentials = new InMemoryCredentialStore(new Credentials(Configuration["Token"]));
            client = new GitHubClient(new ProductHeaderValue(UserName), credentials);
        }

        /// <summary>
        /// Add the default branch protections to the provided branch in the specified repository
        /// </summary>        
        /// <param name="repositoryName">New repository created that needs branch protections</param>        
        public async Task AddBranchProtections(string repositoryName)
        {
            // The webhook payload does not seem to have the correct Default Branch name, its defaulted to master
            // Also the repository doesn't seem to report the correct default branch consistently either
            var repo = await client.Repository.Get(Organization, repositoryName);            

            if (Configuration["DefaultBranch"] != repo.DefaultBranch)
            {
                Console.WriteLine("Specified default did not match the repo");
            }

            if (await AwaitDefaultBranch(repositoryName, Configuration["DefaultBranch"]))
            {
                // Adding Branch protections
                // Required PR Reviews: Dismiss stale reviews and requiring at least 1 required approver
                // Enforce Admins to follow branch policy as well
                var requiredPullRequestReviews = new BranchProtectionRequiredReviewsUpdate(true, false, 1);
                var branchProtections = new BranchProtectionSettingsUpdate(null, requiredPullRequestReviews, null, true);
                
                var result = await client.Repository.Branch.UpdateBranchProtection(Organization, repositoryName, repo.DefaultBranch, branchProtections);
                
                // Create and close an issue in this repository denoting the actions we've taken
                await CreateIssue(repositoryName);

            } else
            {
                Console.WriteLine("Default branch not detected");
            }            
        }

        /// <summary>
        /// Create and close an issue on the given repository detailing what protections were added
        /// </summary>        
        /// <param name="repositoryName">Repository name to create the issue under</param>        
        private async Task CreateIssue(string repositoryName)
        {
            var tempIssue = new NewIssue("Setup Default Branch Protections");
            tempIssue.Body = $"@{UserName} Default Branch Protections Enabled.\n Protections:\n Dismiss Stale Reviews and require at least 1 approver prior to merging.\n Enforce Admin: Apply same protections to Admins.";
            var createdIssue = await client.Issue.Create(Organization, repositoryName, tempIssue);
            var updateIssue = createdIssue.ToUpdate();
            updateIssue.State = ItemState.Closed;
            await client.Issue.Update(Organization, repositoryName, createdIssue.Number, updateIssue);
        }

        private async Task<bool> AwaitDefaultBranch(string repositoryName, string branch)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var result = await client.Repository.Branch.Get(Organization, repositoryName, branch);
                    
                    if (result != null && result.Name == branch)
                    {
                        return true;
                    }
                }
                catch (Exception) { }
                System.Threading.Thread.Sleep(2000);
            }
            return false;
        }

        /// <summary>
        /// Validate the signature using our provided secret to ensure this is a valid WebHook Event
        /// </summary>
        /// <param name="payload">Payload provided from the event to hash and compare with the signature</param>
        /// <param name="signatureWithPrefix">Signature provided with the sha1= prefix</param>
        /// <returns></returns>
        public bool ValidateSignature(string payload, string signatureWithPrefix)
        {

            // If the signature or payload are empty, this is invalid
            if (string.IsNullOrWhiteSpace(signatureWithPrefix) || string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            // The signature comes in with a prefix, sha1=...signature
            // Split on the equals, verify the prefix is sha1
            var splitSignature = signatureWithPrefix.Split("=");
            if (splitSignature == null || splitSignature.Length != 2 || splitSignature[0] != "sha1")
            {
                return false;
            }

            // Produce the hash sequence based on the secret stored in our config
            var hmacSha1 = new HMACSHA1(Encoding.ASCII.GetBytes(Configuration["Secret"]));

            // Compute the hash for the payload byte array to compare to the signature
            var payloadHash = hmacSha1.ComputeHash(Encoding.ASCII.GetBytes(payload));

            // Create the hash string, use x2 for lowercase to match WebHook
            var hashString = string.Join("", payloadHash.Select(c => ((int)c).ToString("x2")));

            return hashString.Equals(splitSignature[1]);
        }

    }
}
