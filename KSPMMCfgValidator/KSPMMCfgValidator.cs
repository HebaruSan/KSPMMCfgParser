using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CommandLine;
using log4net;
using log4net.Config;
using log4net.Core;
using ParsecSharp;
using static ParsecSharp.Text;

using KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParserPrimitives;

namespace KSPMMCfgValidator
{
    /// <summary>
    /// Main class for validator
    /// </summary>
    public class KSPMMCfgValidator
    {
        /// <summary>
        /// Entry point for validator
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>
        /// Standard Unix exit codes (0=success, 1=badopt, 2=fail)
        /// </returns>
        public static int Main(string[] args)
        {
            var logRepo = LogManager.GetRepository(Assembly.GetExecutingAssembly());
            BasicConfigurator.Configure(logRepo);
            logRepo.Threshold = Level.Warn;

            try
            {
                var options = new ValidatorOptions();
                CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
                var results = FindFromPaths(options.Paths)
                    .SelectMany(ParsePath)
                    .ToArray();
                return results.Contains(null) ? ExitError : ExitOk;
            }
            catch (Exception exc)
            {
                log.Error("Oops!", exc);
                return ExitError;
            }
        }

        private static IEnumerable<KSPConfigNode?> ParsePath(string path)
            // TODO: Can it handle a stream without ReadToEnd?
            => ConfigFile.Parse(new StreamReader(path).ReadToEnd())
                         .CaseOf(failure =>
                                 {
                                     log.WarnFormat("{0}:{1}:{2}: {3}",
                                                    path,
                                                    failure.State.Position.Line,
                                                    failure.State.Position.Column,
                                                    failure.Message);
                                     return Enumerable.Repeat<KSPConfigNode?>(null, 1);
                                 },
                                 success => success.Value);

        private static IEnumerable<string> FindFromPaths(List<string> paths)
            => paths.Count < 1
                ? FindCfgFiles(".")
                : paths.SelectMany(p => 
                    File.Exists(p)
                        ? new string[] { p }
                        : FindCfgFiles(p));

        private static IEnumerable<string> FindCfgFiles(string dir)
            => Directory.EnumerateFiles(dir, "*.cfg", SearchOption.AllDirectories);

        private const int ExitOk     = 0;
        private const int ExitBadOpt = 1;
        private const int ExitError  = 2;

        private static readonly ILog log = LogManager.GetLogger(typeof(KSPMMCfgValidator));
    }

    /// <summary>
    /// Command line options parser
    /// </summary>
    public class ValidatorOptions
    {
        /// <summary>
        /// Paths to scan specified on command line
        /// </summary>
        [ValueList(typeof(List<string>))]
        public List<string> Paths { get; set; } = new List<string>();
    }
}
