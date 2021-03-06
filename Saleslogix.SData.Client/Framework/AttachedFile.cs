﻿// Copyright (c) 1997-2014, SalesLogix NA, LLC. All rights reserved.

using System;
using System.IO;
using Saleslogix.SData.Client.Mime;
using Saleslogix.SData.Client.Utilities;

namespace Saleslogix.SData.Client.Framework
{
    /// <summary>
    /// Represents a file that's been attached to a request or response.
    /// </summary>
    public class AttachedFile
    {
        private readonly string _contentType;
        private readonly string _fileName;
        private readonly Stream _stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachedFile"/> class.
        /// </summary>
        /// <param name="part">A multipart MIME part containing the attached file.</param>
        public AttachedFile(MimePart part)
        {
            Guard.ArgumentNotNull(part, "part");
            _contentType = part.ContentType;
            _fileName = GetFileName(part.ContentDisposition);
            _stream = part.Content;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachedFile"/> class.
        /// </summary>
        /// <param name="contentType">The MIME content type of an attached file.</param>
        /// <param name="fileName">The file name of an attached file.</param>
        /// <param name="stream">The <see cref="Stream"/> object that points to the content of an attached file.</param>
        public AttachedFile(string contentType, string fileName, Stream stream)
        {
            _contentType = contentType;
            _fileName = fileName;
            _stream = stream;
        }

        private static string GetFileName(string disposition)
        {
            var contentDisposition = ContentDisposition.Parse(disposition);
            var fileName = contentDisposition["filename"];
            if (fileName != null)
            {
                return fileName;
            }

            fileName = contentDisposition["filename*"];
            if (fileName == null)
            {
                return null;
            }

            var pos = fileName.IndexOf("''", StringComparison.Ordinal);
            if (pos >= 0)
            {
                fileName = fileName.Substring(pos + 2);
            }

            return Uri.UnescapeDataString(fileName);
        }

        /// <summary>
        /// Gets the MIME content type of an attached file.
        /// </summary>
        public string ContentType
        {
            get { return _contentType; }
        }

        /// <summary>
        /// Gets the file name of an attached file.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
        }

        /// <summary>
        /// Gets the <see cref="Stream"/> object that points to the content of an attached file.
        /// </summary>
        public Stream Stream
        {
            get { return _stream; }
        }
    }
}