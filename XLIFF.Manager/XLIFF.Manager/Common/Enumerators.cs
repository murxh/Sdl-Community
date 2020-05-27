﻿namespace Sdl.Community.XLIFF.Manager.Common
{
	public class Enumerators
	{
		public enum Action
		{
			None = 0,
			Export = 1,
			Import = 2
		}

		public enum Status
		{
			None = 0,
			Ready = 1,
			Success = 2,
			Error = 3,
			Warning
		}

		public enum XLIFFSupport
		{
			xliff12sdl = 0,
			xliff12polyglot = 1
			// TODO spport for this format will come later on in the development cycle
			//xliff20sdl = 2
		}
	}
}