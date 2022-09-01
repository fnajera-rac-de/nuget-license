using NuGet.Protocol.Core.Types;
using NuGetUtility.LicenseValidator;

namespace NuGetUtility.RaSpecific
{
    public class NopRaJsonOutputWriter : IRaJsonOutputWriter
    {
        public Task AddRaw(string project, IPackageSearchMetadata data)
        {
            return Task.CompletedTask;
        }

        public Task AddValidatorResults(IEnumerable<ValidatedLicense> validatedLicenses, IEnumerable<LicenseValidationError> errors)
        {
            return Task.CompletedTask;
        }

        public Task Write()
        {
            return Task.CompletedTask;
        }
    }
}
