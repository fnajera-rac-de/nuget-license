using NuGet.Protocol.Core.Types;
using NuGetUtility.LicenseValidator;

namespace NuGetUtility.RaSpecific
{
    public interface IRaJsonOutputWriter
    {
        Task AddRaw(string project, IPackageSearchMetadata data);
        Task AddValidatorResults(IEnumerable<ValidatedLicense> validatedLicenses, IEnumerable<LicenseValidationError> errors);
        Task Write();
    }

}
