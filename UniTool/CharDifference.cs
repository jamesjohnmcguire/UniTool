using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTool
{
	public class CharDifference
	{
		public int Position { get; set; }
		public string? Original { get; set; }
		public string? Normalized { get; set; }
	}
}
