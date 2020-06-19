﻿using Sdl.Community.XLIFF.Manager.FileTypeSupport.XLIFF.Model;
using Sdl.Community.XLIFF.Manager.Model;
using Sdl.Core.Globalization;
using Sdl.FileTypeSupport.Framework.IntegrationApi;

namespace Sdl.Community.XLIFF.Manager.FileTypeSupport.SDLXLIFF
{
	public class SdlxliffWriter
	{
		private readonly SegmentBuilder _segmentBuilder;
		private readonly IFileTypeManager _fileTypeManager;

		public SdlxliffWriter(IFileTypeManager fileTypeManager, SegmentBuilder segmentBuilder)
		{
			_fileTypeManager = fileTypeManager;
			_segmentBuilder = segmentBuilder;
		}

		public bool UpdateFile(Xliff xliff, string filePathInput, string filePathOutput,
			bool overWriteTranslations, ConfirmationStatus confirmationStatusTranslationUpdated,
			ConfirmationStatus confirmationStatusTranslationNotUpdated,
			ConfirmationStatus confirmationStatusNotImported, 
			string originSystem)
		{
			var converter = _fileTypeManager.GetConverterToDefaultBilingual(filePathInput, filePathOutput, null);
			var contentWriter = new ContentWriter(xliff, _segmentBuilder, overWriteTranslations, 
				confirmationStatusTranslationUpdated,
				confirmationStatusTranslationNotUpdated,
				confirmationStatusNotImported, 
				originSystem);

			converter.AddBilingualProcessor(contentWriter);
			converter.SynchronizeDocumentProperties();

			converter.Parse();

			return true;
		}
	}
}
