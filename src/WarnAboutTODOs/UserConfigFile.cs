using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace WarnAboutTODOs
{
    internal class UserConfigFile : AdditionalText
    {
        private UserConfigFile(string path)
        {
            this.Path = path;
        }

        public override string Path { get; }

        public static UserConfigFile FromApplicationData(string configFileName)
        {
            var defaultFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), configFileName);
            return File.Exists(defaultFilePath) ? new UserConfigFile(defaultFilePath) : null;
        }

        public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken))
        {
            return SourceText.From(File.ReadAllText(this.Path));
        }

    }
}
