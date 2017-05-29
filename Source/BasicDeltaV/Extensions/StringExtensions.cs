﻿using KSP.Localization;

namespace BasicDeltaV
{
	public static class StringExtensions
	{
		public static string LocalizeBodyName(this string input)
		{
			return Localizer.Format("<<1>>", input);
		}
	}
}
