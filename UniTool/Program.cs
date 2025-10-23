/////////////////////////////////////////////////////////////////////////////
// <copyright file="Program.cs" company="James John McGuire">
// Copyright © 2025 James John McGuire. All Rights Reserved.
// </copyright>
/////////////////////////////////////////////////////////////////////////////

namespace UniTool
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;
	using DigitalZenWorks.CommandLine.Commands;
	using DigitalZenWorks.Common.Utilities;
	using DigitalZenWorks.Common.VersionUtilities;

	/// <summary>
	/// The program class.
	/// </summary>
	internal sealed class Program
	{
		/// <summary>
		/// The program's main entry point.
		/// </summary>
		/// <param name="arguments">An array of arguments passed to
		/// the program.</param>
		internal static void Main(string[] arguments)
		{
			string version = VersionSupport.GetVersion();
			string message = "Unicode Normalization Tool Version: " + version;

			Console.WriteLine(message);

			CommandsSet? commands = GetCommands();

			CommandLineInstance commandLine = new (commands, arguments);

			if (commandLine.ValidArguments == false)
			{
				ShowError(commandLine.ErrorMessage);
				commandLine.ShowHelp();
			}
			else
			{
				Command command = commandLine.Command;

				switch (command.Name)
				{
					case "check":
						CheckFile(command);
						break;
					case "compare":
						CompareStrings(command);
						break;
					case "normalize":
						Normalize(command);
						break;
					default:
						commandLine.ShowHelp("Unicode Normalization Tool");
						break;
				}
			}
		}

		private static void CheckFile(Command command)
		{
			string filepath = command.Parameters[0];

			if (!File.Exists(filepath))
			{
				ShowError($"File not found: {filepath}");
			}
			else
			{
				Console.WriteLine($"Checking file: {filepath}");
				Console.WriteLine();

				List<NormalizationIssue> issues = [];
				int lineNumber = 0;

				IEnumerable<string> lines =
					File.ReadLines(filepath, Encoding.UTF8);

				foreach (string line in lines)
				{
					lineNumber++;

					NormalizationIssue? issue =
						UnicodeNormalizer.CheckLine(lineNumber, line);

					if (issue != null)
					{
						issues.Add(issue);
					}
				}

				ShowFileIssues(issues);
			}
		}

		private static void CompareStrings(Command command)
		{
			string string1 = command.Parameters[0];
			string string2 = command.Parameters[1];

			UnicodeNormalizer.CompareStrings(string1, string2);
		}

		private static CommandsSet? GetCommands()
		{
			CommandsSet? commandsSet;

			string tempPath = Path.GetTempPath();
			string path = Path.Combine(tempPath, "commands.json");

			bool result = FileUtils.CreateFileFromEmbeddedResource(
				"UniTool.commands.json",
				path);

			if (result == false)
			{
				throw new FileNotFoundException();
			}
			else
			{
				commandsSet = new ();
				commandsSet.JsonFromFile(path);
			}

			return commandsSet;
		}

		private static void Normalize(Command command)
		{
			string inputFile = command.Parameters[0];
			string outputFile = command.Parameters[1];

			ShowNormalizeFileResult(inputFile, outputFile);
		}

		private static void ShowCompareStrings(string string1, string string2)
		{
			string? message;

			Console.WriteLine("String Comparison:");
			Console.WriteLine("==================");
			Console.WriteLine();

			Console.WriteLine($"String 1: '{string1}'");
			ShowUnicodeInformation(string1);
			Console.WriteLine();

			Console.WriteLine($"String 2: '{string2}'");
			ShowUnicodeInformation(string2);
			Console.WriteLine();

			string normalizedString1 =
				string1.Normalize(NormalizationForm.FormKC);
			string normalizedString2 =
				string2.Normalize(NormalizationForm.FormKC);

			string? normalizedHexString1 =
				UnicodeNormalizer.GetHexadecimalString(normalizedString1);
			string? normalizedHexString2 =
				UnicodeNormalizer.GetHexadecimalString(normalizedString2);

			Console.WriteLine("After Form KC Normalization:");

			message = string.Format(
				CultureInfo.InvariantCulture,
				"  String 1: '{0}' (U+{1})",
				normalizedString1,
				normalizedHexString1);
			Console.WriteLine(message);

			message = string.Format(
				CultureInfo.InvariantCulture,
				"  String 2: '{0}' (U+{1})",
				normalizedString2,
				normalizedHexString2);
			Console.WriteLine(message);

			Console.WriteLine();

			bool isEqual = UnicodeNormalizer.CompareStrings(
				normalizedString1, normalizedString2);

			if (isEqual == true)
			{
				message = "✓ Strings are equivalent after normalization";
			}
			else
			{
				message = "✗ Strings are different even after normalization";
			}

			Console.WriteLine(message);
		}

		private static void ShowCharacterIssue(CharDifference difference)
		{
			string positionPadded = $"{difference.Position,3}";
			char issueChar = difference.Original![0];
			int issueBits = (int)issueChar;
			string issueBitsFormatted = $"(U+{issueBits:X4})";

			char normalizedChar = difference.Normalized![0];
			int normalizedBits = (int)normalizedChar;
			string normalizedBitsFormatted = $"(U+{normalizedBits:X4})";

			string message = string.Format(
				CultureInfo.InvariantCulture,
				"  Column: {0} '{1}' {2} → '{3}' {4}",
				positionPadded,
				difference.Original,
				issueBitsFormatted,
				difference.Normalized,
				normalizedBitsFormatted);

			Console.WriteLine(message);
		}

		private static void ShowError(string message)
		{
			message = "Error: " + message;
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ResetColor();
		}

		private static void ShowFileIssue(NormalizationIssue issue)
		{
			string message = $"Line {issue.LineNumber,4}:";

			Console.WriteLine(message);

			foreach (CharDifference difference in issue.Differences!)
			{
				ShowCharacterIssue(difference);
			}

			Console.WriteLine();
		}

		private static void ShowFileIssues(
			List<NormalizationIssue> issues, int limit = 10)
		{
			string message;

			if (issues.Count == 0)
			{
				message = "✓ All text is properly normalized (Form KC)";
				Console.WriteLine(message);
			}
			else
			{
				message = string.Format(
					CultureInfo.InvariantCulture,
					"⚠ Found {0} line(s) with normalization issues:",
					issues.Count);

				Console.WriteLine(message);
				Console.WriteLine();

				IEnumerable<NormalizationIssue> limited = issues.Take(limit);

				foreach (NormalizationIssue? issue in limited)
				{
					ShowFileIssue(issue);
				}

				if (issues.Count > limit)
				{
					int remaining = issues.Count - limit;
					message = $"... and {remaining} more issue(s)";
					Console.WriteLine(message);
				}
			}
		}

		private static void ShowNormalizeFileResult(
			string inputPath, string outputPath)
		{
			Console.WriteLine($"Normalizing: {inputPath} → {outputPath}");

			int linesChanged = UnicodeNormalizer.NormalizeFile(
				inputPath, outputPath, out int linesProcessed);

			if (linesChanged == -1)
			{
				Console.WriteLine($"Error: File not found: {inputPath}");
			}
			else
			{
				string message = string.Format(
					CultureInfo.InvariantCulture,
					"✓ Complete: {0} lines processed, {1} lines normalized",
					linesProcessed,
					linesChanged);

				Console.WriteLine(message);
			}
		}

		private static void ShowUnicodeCharacterNormalization(char character)
		{
			string characterString = character.ToString();
			bool isNormalizedFormC =
				characterString.IsNormalized(NormalizationForm.FormC);
			bool isNormalizedFormKC =
				characterString.IsNormalized(NormalizationForm.FormKC);

			string isNormalizedFormCString = isNormalizedFormC ? "✓" : "✗";
			string isNormalizedFormKCString = isNormalizedFormKC ? "✓" : "✗";

			string message = string.Format(
				CultureInfo.CurrentCulture,
				"    Normalized Form C: {0} | Normalized Form KC: {1}",
				isNormalizedFormCString,
				isNormalizedFormKCString);

			Console.WriteLine(message);
		}

		private static void ShowUnicodeCharacterPoint(char character)
		{
				int codePoint = (int)character;

				string message = string.Format(
					CultureInfo.CurrentCulture,
					"  Character: '{0}' → U+{1:X4} (decimal: {1})",
					character,
					codePoint);

				Console.WriteLine(message);
		}

		private static void ShowUnicodeInformation(string text)
		{
			foreach (char character in text)
			{
				ShowUnicodeCharacterPoint(character);

				ShowUnicodeCharacterNormalization(character);
			}
		}
	}
}
