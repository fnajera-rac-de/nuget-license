using NuGet.Protocol.Core.Types;
using NuGetUtility.LicenseValidator;
using System.Text.Json;

namespace NuGetUtility.RaSpecific
{
    public class FileRaJsonOutputWriter : IRaJsonOutputWriter
    {
        private readonly string _file;
        private readonly Dictionary<string, List<IPackageSearchMetadata>> _raw;
        private readonly List<LicenseValidationError> _errors;
        private readonly List<ValidatedLicense> _validatedLicenses;

        public FileRaJsonOutputWriter(string file)
        {
            _file = file;
            _raw = new Dictionary<string, List<IPackageSearchMetadata>>();
            _errors = new List<LicenseValidationError>();
            _validatedLicenses = new List<ValidatedLicense>();
        }

        public Task AddRaw(string project, IPackageSearchMetadata data)
        {
            if (!_raw.TryGetValue(project, out var list))
            {
                list = new List<IPackageSearchMetadata>();
                _raw[project] = list;
            }
            list.Add(data);
            return Task.CompletedTask;
        }

        public Task AddValidatorResults(IEnumerable<ValidatedLicense> validatedLicenses, IEnumerable<LicenseValidationError> errors)
        {
            _validatedLicenses.AddRange(validatedLicenses);
            _errors.AddRange(errors);
            return Task.CompletedTask;
        }

        public async Task Write()
        {
            var directory = Path.GetDirectoryName(_file);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fs = File.Create(_file);
            await JsonSerializer.SerializeAsync(fs, new
            {
                Raw = _raw,
                Licenses = _validatedLicenses,
                Errors = _errors,
            }, new JsonSerializerOptions { WriteIndented = true });
        }

    }
}
