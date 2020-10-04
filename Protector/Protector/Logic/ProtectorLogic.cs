using Microsoft.Extensions.Configuration;
using Octokit;
using Octokit.Internal;
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

        /// <summary>
        /// Setup our GitHubClient in the constructor to be used
        /// </summary>
        /// <param name="_configuration">Configuration key/values from appsettings</param>
        public ProtectorLogic(IConfiguration _configuration)
        {
            Configuration = _configuration;
            UserName = Configuration["OrgOwner"];
            credentials = new InMemoryCredentialStore(new Credentials(Configuration["Token"]));
            client = new GitHubClient(new ProductHeaderValue(UserName), credentials);
        }

        /// <summary>
        /// Add the default branch protections to the provided branch in the specified repository
        /// </summary>
        /// <param name="defaultBranch">Represents the default branch name setup by the Organization</param>
        /// <param name="repositoryName">New repository created that needs branch protections</param>        
        public async Task AddBranchProtections(string defaultBranch, string repositoryName)
        {   
            var result = await client.Repository.Branch.UpdateBranchProtection(UserName, repositoryName, defaultBranch, new BranchProtectionSettingsUpdate(true));            
            await CreateIssue(repositoryName);          
        }

        /// <summary>
        /// Create and close an issue on the given repository detailing what protections were added
        /// </summary>        
        /// <param name="repositoryName">Repository name to create the issue under</param>        
        private async Task CreateIssue(string repositoryName)
        {
            var tempIssue = new NewIssue("Setup Default Branch Protections");
            tempIssue.Body = "Testing creating an issue regarding setting master branch";
            var createdIssue = await client.Issue.Create(UserName, repositoryName, tempIssue);
            var updateIssue = createdIssue.ToUpdate();
            updateIssue.State = ItemState.Closed;
            await client.Issue.Update(UserName, repositoryName, createdIssue.Number, updateIssue);
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
