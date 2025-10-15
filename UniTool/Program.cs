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

			Console.WriteLine(
				"Unicode Normalization Tool Version: " + version);

			if (arguments.Length == 0)
			{
				ShowUsage();
			}
			else
			{
				string command = arguments[0];

#pragma warning disable CA1308
				command = command.ToLower(CultureInfo.InvariantCulture);
#pragma warning restore CA1308
				string fileName = arguments[1];

				switch (command)
				{
					case "check":
						if (arguments.Length < 2)
						{
							ShowError("Please specify a file path");
							return;
						}

						CheckFile(fileName);
						break;

					case "normalize":
						if (arguments.Length < 3)
						{
							ShowError(
								"Please specify input and output file paths");
							return;
						}

						string outputFileName = arguments[2];
						ShowNormalizeFileResult(fileName, outputFileName);
						break;

					case "compare":
						if (arguments.Length < 3)
						{
							ShowError(
								"Please specify two strings to compare");
							return;
						}

						string string1 = arguments[1];
						string string2 = arguments[2];

						UnicodeNormalizer.CompareStrings(string1, string2);
						break;

					default:
						ShowUsage();
						break;
				}
			}
		}

		private static void CheckFile(string filepath)
		{
			if (!File.Exists(filepath))
			{
				Console.WriteLine($"Error: File not found: {filepath}");
				return;
			}

			Console.WriteLine($"Checking file: {filepath}");
			Console.WriteLine();

			var issues = new List<NormalizationIssue>();
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

			Console.WriteLine(message);
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

		private static void ShowFileIssues(List<NormalizationIssue> issues)
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

				foreach (NormalizationIssue? issue in issues.Take(10))
				{
					ShowFileIssue(issue);
				}

				if (issues.Count > 10)
				{
					int remaining = issues.Count - 10;
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

		private static void ShowUsage()
		{
			Console.WriteLine("Unicode Normalization Tool");
			Console.WriteLine("==========================");
			Console.WriteLine();
			Console.WriteLine("Usage:");
			Console.WriteLine("  check <filepath>              - Check CSV file for non-normalized text");
			Console.WriteLine("  normalize <input> <output>    - Normalize CSV file to NFKC form");
			Console.WriteLine("  compare <string1> <string2>   - Compare two strings and show Unicode info");
			Console.WriteLine();
			Console.WriteLine("Examples:");
			Console.WriteLine("  UnicodeTool check data.csv");
			Console.WriteLine("  UnicodeTool normalize input.csv output.csv");
			Console.WriteLine("  UnicodeTool compare \"⽷\" \"糸\"");
		}
	}
}
