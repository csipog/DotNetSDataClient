﻿// Copyright (c) 1997-2014, SalesLogix NA, LLC. All rights reserved.

using System.Xml.Serialization;

namespace Saleslogix.SData.Client.Framework
{
    /// <summary>
    /// Defines the list of supported media types.
    /// </summary>
    public enum MediaType
    {
        /// <summary>
        /// Text
        /// </summary>
        [XmlEnum(MediaTypeNames.TextMediaType)] Text,

        /// <summary>
        /// HTML
        /// </summary>
        [XmlEnum(MediaTypeNames.HtmlMediaType)] Html,

        /// <summary>
        /// ATOM feed syndication format
        /// </summary>
        [XmlEnum(MediaTypeNames.AtomMediaType)] Atom,

        /// <summary>
        /// ATOM entry syndication format
        /// </summary>
        [XmlEnum(MediaTypeNames.AtomEntryMediaType)] AtomEntry,

        /// <summary>
        /// RSS syndication format
        /// </summary>
        [XmlEnum(MediaTypeNames.RssMediaType)] Rss,

        /// <summary>
        /// XML format
        /// </summary>
        [XmlEnum(MediaTypeNames.XmlMediaType)] Xml,

        /// <summary>
        /// Image PNG format
        /// </summary>
        [XmlEnum(MediaTypeNames.ImagePngMediaType)] ImagePng,

        /// <summary>
        /// Image JPEG format
        /// </summary>
        [XmlEnum(MediaTypeNames.ImageJpegMediaType)] ImageJpeg,

        /// <summary>
        /// Image GIF format
        /// </summary>
        [XmlEnum(MediaTypeNames.ImageGifMediaType)] ImageGif,

        /// <summary>
        /// Image TIFF format
        /// </summary>
        [XmlEnum(MediaTypeNames.ImageTiffMediaType)] ImageTiff,

        /// <summary>
        /// Image BMP format
        /// </summary>
        [XmlEnum(MediaTypeNames.ImageBmpMediaType)] ImageBmp,

        /// <summary>
        /// XLST format
        /// </summary>
        [XmlEnum(MediaTypeNames.XsltMediaType)] Xslt,

        /// <summary>
        /// XSS format
        /// </summary>
        [XmlEnum(MediaTypeNames.CssMediaType)] Css,

        /// <summary>
        /// JSON format
        /// </summary>
        [XmlEnum(MediaTypeNames.JsonMediaType)] Json,

        /// <summary>
        /// BSON format
        /// </summary>
        [XmlEnum(MediaTypeNames.BsonMediaType)] Bson,

        /// <summary>
        /// Form format
        /// </summary>
        [XmlEnum(MediaTypeNames.FormMediaType)] Form,

        /// <summary>
        /// Multipart format
        /// </summary>
        Multipart
    }
}