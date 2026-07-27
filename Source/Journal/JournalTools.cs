using System;
using System.Collections.Generic;
using System.Text;
using FlaxEngine;

namespace Journal
{
	/// <summary>
	/// This class provides useful tools for consoles that you can use.
	/// Mostly obvious stuff so you dont need to reimplement it again.
	/// </summary>
	public static class ConsoleTools
	{
		public static IEnumerable<string> NormalizeArgs(string input)
		{	
			var argBuild = new StringBuilder(64);
			var quoted = false;
			var textStart = false;
			
			for (var i = 0; i < input.Length; i++)
			{
				var chr = input[i];
				switch (chr)
				{
					case '"':
						if (quoted)
						{
							if (i + 1 < input.Length && input[i+1] != ' ' && input[i+1] != '\t')
								throw new Exception("No space after closing quote at position " + (i+1));
							yield return argBuild.ToString();
							argBuild.Clear();
						}
						else if (textStart)
							throw new Exception("Use \\\" to place double quote at position " + (i+1));
						quoted = !quoted;
						textStart = !textStart;
						continue;
					case '\t':
					case ' ':
						if (quoted)
							argBuild.Append(chr);
						else if (textStart)
						{
							yield return argBuild.ToString();
							textStart = false;
						}
						continue;
					case '\\':
						try
						{
							chr = SpChr(ref input, ref i);
						} catch
						{
							throw new Exception("Invalid special character at position " + (i+1));
						}
						break;
				}
				textStart = true;
				argBuild.Append(chr);
			}
			if (quoted)
				throw new Exception("Missing closing quote at the end");
			if (argBuild.Length > 0)
				yield return argBuild.ToString();
		}

		private static char SpChr(ref string str, ref int index) 
		{
			switch(str[++index])
			{
				case 'n': return '\n';
				case 't': return '\t';
				case 'r': return '\r';
				case 'b': return '\b';
				case 'f': return '\f';
				case '"': return '"';
				case '\\': return '\\';
				case 'u': 
					char[] xa = new char[4];
					str.CopyTo(index+1,xa,0,4);
					index += 4;
					return Convert.ToChar(Convert.ToUInt16(new string(xa), 16));
			}
			return '\0';
		}

		private static void X(string arg, ref StringBuilder builder)
		{
			
		}
	}
}