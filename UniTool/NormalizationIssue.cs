using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTool
{
	public class NormalizationIssue
	{
		public int LineNumber { get; set; }
		public string? OriginalLine { get; set; }
		public string? NormalizedLine { get; set; }
		public List<CharDifference>? Differences { get; set; }
	}
}
