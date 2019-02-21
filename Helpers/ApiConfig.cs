using System.Collections.Generic;

namespace Api.Helpers
{
    public class ApiConfig
    {
        public string SecurityKey { get; set; }
        public string JwtIssuer { get; set; }
        public string FilesDirectoryPath { get; set; }
        public string[] Roles { get; set; }
    }
}
