using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;
using static NugetUtility.Utilities;

namespace NugetUtility
{
    public class PackageOptions
    {
        private readonly Regex UserRegexRegex = new Regex("^([/#])(.+)\\1$");

        private ICollection<string> _allowedLicenseTypes = new Collection<string>();
        private ICollection<LibraryInfo> _manualInformation = new Collection<LibraryInfo>();
        private ICollection<string> _projectFilter = new Collection<string>();
        private ICollection<string> _packagesFilter = new Collection<string>();
        private Dictionary<string, string> _customLicenseToUrlMappings = new Dictionary<string, string>();

        [Option("allowed-license-types", Default = null, HelpText = "Simple json file of a text array of allowable licenses, if no file is given, all are assumed allowed")]
        public string AllowedLicenseTypesOption { get; set; }

        [Option("include-project-file", Default = false, HelpText = "Adds project file path to information when enabled.")]
        public bool IncludeProjectFile { get; set; }

        [Option('l', "log-level", Default = LogLevel.Error, HelpText = "Sets log level for output display. Options: Error|Warning|Information|Verbose.")]
        public LogLevel LogLevelThreshold { get; set; }

        [Option("manual-package-information", Default = null, HelpText = "Simple json file of an array of LibraryInfo objects for manually determined packages.")]
        public string ManualInformationOption { get; set; }

        [Option("licenseurl-to-license-mappings", Default = null, HelpText = "Simple json file of Dictionary<string,string> to override default mappings")]
        public string LicenseToUrlMappingsOption { get; set; }

        [Option('o', "output", Default = false, HelpText = "Saves as text file (licenses.txt)")]
        public bool TextOutput { get; set; }

        [Option("outfile", Default = null, HelpText = "Output filename")]
        public string OutputFileName { get; set; }

        [Option('f', "output-directory", Default = null, HelpText = "Output Directory")]
        public string OutputDirectory { get; set; }

        [Option('i', "input", HelpText = "The projects in which to search for used nuget packages. This can either be a folder, a project file, a solution file or a json file containing a list of projects.")]
        public string ProjectDirectory { get; set; }

        [Option("projects-filter", Default = null, HelpText = "Simple json file of a text array of projects to skip. Supports Ends with matching such as 'Tests.csproj'")]
        public string ProjectsFilterOption { get; set; }

        [Option("packages-filter", Default = null, HelpText = "Simple json file of a text array of packages to skip, or a regular expression defined between two forward slashes or two hashes.")]
        public string PackagesFilterOption { get; set; }

        [Option('u', "unique", Default = false, HelpText = "Unique licenses list by Id/Version")]
        public bool UniqueOnly { get; set; }

        [Option('p', "print", Default = true, HelpText = "Print licenses.")]
        public bool? Print { get; set; }

        [Option('j', "json", Default = false, HelpText = "Saves licenses list in a json file (licenses.json)")]
        public bool JsonOutput { get; set; }

        [Option('m', "md", Default = false, HelpText = "Saves the licenses list to a markdown file (licenses.md)")]
        public bool MarkDownOutput { get; set; }

        [Option('e', "export-license-texts", Default = false, HelpText = "Exports the raw license texts")]
        public bool ExportLicenseTexts { get; set; }

        [Option('t', "include-transitive", Default = false, HelpText = "Include distinct transitive package licenses per project file.")]
        public bool IncludeTransitive { get; set; }

        [Option('c', "convert-html-to-text", Default = false, HelpText = "Convert html licenses to plain text.")]
        public bool ConvertHtmlToText { get; set; }

        [Option("ignore-ssl-certificate-errors", Default = false, HelpText = "Ignore SSL certificate errors in HttpClient.")]
        public bool IgnoreSslCertificateErrors { get; set; }

        [Option("use-project-assets-json", Default = false, HelpText = "Use the resolved project.assets.json file for each project as the source of package information. Requires the -t option. Requires `nuget restore` or `dotnet restore` to be run first.")]
        public bool UseProjectAssetsJson { get; set; }

        [Option("timeout", Default = 10, HelpText = "Set HttpClient timeout in seconds.")]
        public int Timeout { get; set; }

        [Option("proxy-url", HelpText = "Set a proxy server URL to be used by HttpClient.")]
        public string ProxyURL { get; set; }

        [Option("proxy-system-auth", Default = false, HelpText = "Use the system credentials for proxy authentication.")]
        public bool ProxySystemAuth { get; set; }

        [Usage(ApplicationAlias = "dotnet-project-licenses")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
            new Example ("Simple", new PackageOptions { ProjectDirectory = "~/Projects/test-project" }),
            new Example ("VS Solution", new PackageOptions { ProjectDirectory = "~/Projects/test-project/project.sln" }),
            new Example ("Unique VS Solution to Custom JSON File", new PackageOptions {
                        ProjectDirectory = "~/Projects/test-project/project.sln",
                        UniqueOnly = true,
                        JsonOutput = true,
                        OutputFileName = @"~/Projects/another-folder/licenses.json"
                        }),
            new Example("Export all license texts in a specific directory with verbose log", new PackageOptions
            {
                LogLevelThreshold = LogLevel.Verbose,
                OutputDirectory = "~/Projects/exports",
                ExportLicenseTexts = true,
            }),
                };
            }
        }

        public ICollection<string> AllowedLicenseType
        {
            get
            {
                if (_allowedLicenseTypes.Any()) { return _allowedLicenseTypes; }

                return _allowedLicenseTypes = ReadListFromFile<string>(AllowedLicenseTypesOption);
            }
        }

        public ICollection<LibraryInfo> ManualInformation
        {
            get
            {
                if (_manualInformation.Any()) { return _manualInformation; }

                return _manualInformation = ReadListFromFile<LibraryInfo>(ManualInformationOption);
            }
        }

        public ICollection<string> ProjectFilter
        {
            get
            {
                if (_projectFilter.Any()) { return _projectFilter; }

                return _projectFilter = ReadListFromFile<string>(ProjectsFilterOption)
                    .Select(x => x.EnsureCorrectPathCharacter())
                    .ToList();
            }
        }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public Regex? PackageRegex
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            get
            {
                if (PackagesFilterOption == null) return null;

                // Check if the input is a regular expression that is defined between two forward slashes '/';
                if (UserRegexRegex.IsMatch(PackagesFilterOption))
                {
                    var userRegexString = UserRegexRegex.Replace(PackagesFilterOption, "$2");
                    // Try parse regular expression between forward slashes or hashes
                    try
                    {
                        var parsedExpression = new Regex(userRegexString, RegexOptions.IgnoreCase);
                        return parsedExpression;
                    }
                    // Catch and suppress Argument exception thrown when pattern is invalid
                    catch (ArgumentException e)
                    {
                        throw new ArgumentException($"Cannot parse regex '{userRegexString}'", e);
                    }
                }

                return null;
            }
        }

        public ICollection<string> PackageFilter
        {
            get
            {
                // If we've already found package filters, or the user input is a regular expression,
                // Return the packagesFilter
                if (_packagesFilter.Any() ||
                    (PackagesFilterOption != null && UserRegexRegex.IsMatch(PackagesFilterOption)))
                {
                    return _packagesFilter;
                }

                return _packagesFilter = ReadListFromFile<string>(PackagesFilterOption);
            }
        }

        public IReadOnlyDictionary<string, string> LicenseToUrlMappingsDictionary
        {
            get
            {
                if (_customLicenseToUrlMappings.Any()) { return _customLicenseToUrlMappings; }

                return _customLicenseToUrlMappings = ReadDictionaryFromFile(LicenseToUrlMappingsOption, LicenseToUrlMappings.Default);
            }
        }
    }
}