﻿// Copyright (c) Sage (UK) Limited 2010. All rights reserved.
// This code may not be copied or used, except as set out in a written licence agreement
// between the user and Sage (UK) Limited, which specifically permits the user to use this
// code. Please contact Sage (UK) if you do not have such a licence. Sage will take
// appropriate legal action against those who make unauthorised use of this code.

using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using Sage.SData.Client.Atom;

namespace Sage.SData.Client.Framework
{
    /// <summary>
    /// TODO
    /// </summary>
    public class SDataResponse
    {
        private readonly HttpStatusCode _statusCode;
        private readonly MediaType _contentType;
        private readonly string _eTag;
        private readonly string _location;
        private readonly object _content;

        internal SDataResponse(WebResponse response, string redirectLocation)
        {
            var httpResponse = response as HttpWebResponse;
            _statusCode = httpResponse != null ? httpResponse.StatusCode : 0;
            _contentType = MediaTypeNames.GetMediaType(response.ContentType);
            _eTag = response.Headers[HttpResponseHeader.ETag];
            _location = response.Headers[HttpResponseHeader.Location] ?? redirectLocation;

            if (_statusCode != HttpStatusCode.NoContent)
            {
                using (var stream = response.GetResponseStream())
                {
                    _content = LoadContent(stream, _contentType);
                }
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get { return _statusCode; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public MediaType ContentType
        {
            get { return _contentType; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public string ETag
        {
            get { return _eTag; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public string Location
        {
            get { return _location; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public object Content
        {
            get { return _content; }
        }

        private static object LoadContent(Stream stream, MediaType contentType)
        {
            switch (contentType)
            {
                case MediaType.Atom:
                    return LoadFeedContent(stream);
                case MediaType.AtomEntry:
                    return LoadEntryContent(stream);
                default:
                    return LoadOtherContent(stream, contentType);
            }
        }

        private static AtomFeed LoadFeedContent(Stream stream)
        {
            var feed = new AtomFeed();
            feed.Load(stream);
            return feed;
        }

        private static AtomEntry LoadEntryContent(Stream stream)
        {
            var entry = new AtomEntry();
            entry.Load(stream);
            return entry;
        }

        private static object LoadOtherContent(Stream stream, MediaType contentType)
        {
            if (contentType == MediaType.Xml)
            {
                using (var memory = new MemoryStream())
                {
                    int num;
                    var buffer = new byte[0x1000];

                    while ((num = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        memory.Write(buffer, 0, num);
                    }

                    memory.Seek(0, SeekOrigin.Begin);
                    var content = LoadTrackingContent(memory);

                    if (content != null)
                    {
                        return content;
                    }

                    memory.Seek(0, SeekOrigin.Begin);
                    return LoadStringContent(memory);
                }
            }

            return LoadStringContent(stream);
        }

        private static SDataTracking LoadTrackingContent(Stream stream)
        {
            var serializer = new XmlSerializer(typeof (SDataTracking));

            try
            {
                return (SDataTracking) serializer.Deserialize(stream);
            }
            catch (XmlException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            return null;
        }

        private static string LoadStringContent(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}