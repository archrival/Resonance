using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.SubsonicCompat
{
    public class SubsonicQueryParameters
    {
        public string AuthenticationToken { get; set; }
        public string Callback { get; set; }
        public string ClientName { get; set; }
        public SubsonicReturnFormat Format { get; set; }
        public string Password { get; set; }
        public Version ProtocolVersion { get; set; }
        public string Salt { get; set; }
        public string Username { get; set; }

        public static SubsonicQueryParameters FromCollection(IEnumerable<KeyValuePair<string, StringValues>> collection)
        {
            var subsonicQueryParameters = new SubsonicQueryParameters();

            Parallel.ForEach(collection, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, kvp =>
            {
                var value = kvp.Value.FirstOrDefault();

                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                switch (kvp.Key)
                {
                    case "u":
                        subsonicQueryParameters.Username = value;
                        break;

                    case "p":
                        subsonicQueryParameters.Password = value;
                        break;

                    case "t":
                        subsonicQueryParameters.AuthenticationToken = value;
                        break;

                    case "s":
                        subsonicQueryParameters.Salt = value;
                        break;

                    case "v":
                        if (Version.TryParse(value, out var version))
                        {
                            subsonicQueryParameters.ProtocolVersion = version;
                        }

                        break;

                    case "c":
                        subsonicQueryParameters.ClientName = value;
                        break;

                    case "f":
                        if (Enum.TryParse(value, true, out SubsonicReturnFormat format))
                        {
                            subsonicQueryParameters.Format = format;
                        }

                        break;

                    case "callback":
                        subsonicQueryParameters.Callback = value;
                        break;
                }
            });

            return subsonicQueryParameters;
        }
    }
}