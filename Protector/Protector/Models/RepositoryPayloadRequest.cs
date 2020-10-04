namespace Protector.Models
{
    public class RepositoryPayloadRequest
    {
        public string action { get; set; }

        public Repository repository { get; set; }
    }

    public class Repository
    {
        public string name { get; set; }
        public string full_name { get; set; }
        public Owner owner { get; set; }
        public string default_branch { get; set; }
    }

    public class Owner
    {
        public string login { get; set; }
    }
}
