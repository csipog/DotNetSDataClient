﻿// Copyright (c) 1997-2013, SalesLogix NA, LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Saleslogix.SData.Client.Content;
using Saleslogix.SData.Client.Framework;
using Saleslogix.SData.Client.Utilities;

#if !NET_2_0 && !NET_3_5
using System.Threading.Tasks;
#endif

namespace Saleslogix.SData.Client
{
    public static class SDataClientExtensions
    {
#if !PCL && !NETFX_CORE && !SILVERLIGHT
        public static SDataResource Get(this ISDataClient client, string key, string path, IEnumerable<string> include = null, IEnumerable<string> select = null, int? precedence = null)
        {
            Guard.ArgumentNotNull(client, "client");
            return client.Execute<SDataResource>(GetGetParameters(key, path, include, select, precedence)).Content;
        }

        public static T Get<T>(this ISDataClient client, string key, string path = null, IEnumerable<string> include = null, IEnumerable<string> select = null, int? precedence = null)
        {
            Guard.ArgumentNotNull(client, "client");
            return client.Execute<T>(GetGetParameters(key, path ?? GetPath<T>(client), include, select, precedence)).Content;
        }
#endif

#if !NET_2_0 && !NET_3_5
        public static Task<SDataResource> GetAsync(this ISDataClient client, string key, string path, IEnumerable<string> include = null, IEnumerable<string> select = null, int? precedence = null, CancellationToken cancel = default(CancellationToken))
        {
            Guard.ArgumentNotNull(client, "client");
            return client.ExecuteAsync<SDataResource>(GetGetParameters(key, path, include, select, precedence), cancel)
                .ContinueWith(task => task.Result.Content, cancel);
        }

        public static Task<T> GetAsync<T>(this ISDataClient client, string key, string path = null, IEnumerable<string> include = null, IEnumerable<string> select = null, int? precedence = null, CancellationToken cancel = default(CancellationToken))
        {
            Guard.ArgumentNotNull(client, "client");
            return client.ExecuteAsync<T>(GetGetParameters(key, path ?? GetPath<T>(client), include, select, precedence), cancel)
                .ContinueWith(task => task.Result.Content, cancel);
        }
#endif

        private static SDataParameters GetGetParameters(string key, string path, IEnumerable<string> include, IEnumerable<string> select, int? precedence)
        {
            Guard.ArgumentNotNullOrEmptyString(path, "path");
            Guard.ArgumentNotNullOrEmptyString(key, "key");
            return new SDataParameters
                {
                    Path = path,
                    Selector = SDataUri.FormatConstant(key),
                    Include = include != null ? string.Join(",", include.ToArray()) : null,
                    Select = select != null ? string.Join(",", select.ToArray()) : null,
                    Precedence = precedence
                };
        }

#if !PCL && !NETFX_CORE && !SILVERLIGHT
        public static SDataResource Post(this ISDataClient client, SDataResource content, string path)
        {
            return client.Execute<SDataResource>(GetPostParameters(client, content, path)).Content;
        }

        public static T Post<T>(this ISDataClient client, T content, string path = null)
        {
            return client.Execute<T>(GetPostParameters(client, content, path ?? GetPath<T>(client))).Content;
        }
#endif

#if !NET_2_0 && !NET_3_5
        public static Task<SDataResource> PostAsync(this ISDataClient client, SDataResource content, string path, CancellationToken cancel = default(CancellationToken))
        {
            return client.ExecuteAsync<SDataResource>(GetPostParameters(client, content, path), cancel)
                .ContinueWith(task => task.Result.Content, cancel);
        }

        public static Task<T> PostAsync<T>(this ISDataClient client, T content, string path = null, CancellationToken cancel = default(CancellationToken))
        {
            return client.ExecuteAsync<T>(GetPostParameters(client, content, path ?? GetPath<T>(client)), cancel)
                .ContinueWith(task => task.Result.Content, cancel);
        }
#endif

        private static SDataParameters GetPostParameters<T>(ISDataClient client, T content, string path)
        {
            Guard.ArgumentNotNull(client, "client");
            Guard.ArgumentNotNull(content, "content");
            Guard.ArgumentNotNullOrEmptyString(path, "path");
            return new SDataParameters
                {
                    Method = HttpMethod.Post,
                    Path = path,
                    Content = content,
                    ContentType = client.Format ?? MediaType.Json
                };
        }

#if !PCL && !NETFX_CORE && !SILVERLIGHT
        public static SDataResource Put(this ISDataClient client, SDataResource content, string path = null)
        {
            return client.Execute<SDataResource>(GetPutParameters(client, content, path)).Content;
        }

        public static T Put<T>(this ISDataClient client, T content, string path = null)
        {
            return client.Execute<T>(GetPutParameters(client, content, path ?? GetPath<T>(client))).Content;
        }
#endif

#if !NET_2_0 && !NET_3_5
        public static Task<SDataResource> PutAsync(this ISDataClient client, SDataResource content, string path, CancellationToken cancel = default(CancellationToken))
        {
            return client.ExecuteAsync<SDataResource>(GetPutParameters(client, content, path), cancel)
                .ContinueWith(task => task.Result.Content, cancel);
        }

        public static Task<T> PutAsync<T>(this ISDataClient client, T content, string path = null, CancellationToken cancel = default(CancellationToken))
        {
            return client.ExecuteAsync<T>(GetPutParameters(client, content, path ?? GetPath<T>(client)), cancel)
                .ContinueWith(task => task.Result.Content, cancel);
        }
#endif

        private static SDataParameters GetPutParameters<T>(ISDataClient client, T content, string path)
        {
            Guard.ArgumentNotNull(client, "client");
            Guard.ArgumentNotNull(content, "content");
            Guard.ArgumentNotNullOrEmptyString(path, "path");
            return new SDataParameters
                {
                    Method = HttpMethod.Put,
                    Path = path,
                    Selector = GetSelector(content),
                    Content = content,
                    ContentType = client.Format ?? MediaType.Json,
                    ETag = GetETag(content)
                };
        }

#if !PCL && !NETFX_CORE && !SILVERLIGHT
        public static void Delete(this ISDataClient client, SDataResource content, string path = null)
        {
            Guard.ArgumentNotNull(client, "client");
            client.Execute(GetDeleteParameters(content, path));
        }

        public static void Delete<T>(this ISDataClient client, T content, string path = null)
        {
            Guard.ArgumentNotNull(client, "client");
            client.Execute(GetDeleteParameters(content, path ?? GetPath<T>(client)));
        }
#endif

#if !NET_2_0 && !NET_3_5
        public static Task DeleteAsync(this ISDataClient client, SDataResource content, string path = null, CancellationToken cancel = default(CancellationToken))
        {
            Guard.ArgumentNotNull(client, "client");
            return client.ExecuteAsync(GetDeleteParameters(content, path), cancel);
        }

        public static Task DeleteAsync<T>(this ISDataClient client, T content, string path = null, CancellationToken cancel = default(CancellationToken))
        {
            Guard.ArgumentNotNull(client, "client");
            return client.ExecuteAsync(GetDeleteParameters(content, path ?? GetPath<T>(client)), cancel);
        }
#endif

        private static SDataParameters GetDeleteParameters<T>(T content, string path)
        {
            Guard.ArgumentNotNull(content, "content");
            Guard.ArgumentNotNullOrEmptyString(path, "path");
            return new SDataParameters
                {
                    Method = HttpMethod.Delete,
                    Path = path,
                    Selector = GetSelector(content),
                    ETag = GetETag(content)
                };
        }

        private static string GetPath<T>(ISDataClient client)
        {
            return SDataResourceAttribute.GetPath(typeof (T)) ?? client.NamingScheme.GetName(typeof (T).GetTypeInfo());
        }

        private static string GetSelector(object content)
        {
            return SDataUri.FormatConstant(ContentHelper.GetProtocolValue<string>(content, SDataProtocolProperty.Key));
        }

        private static string GetETag(object content)
        {
            return ContentHelper.GetProtocolValue<string>(content, SDataProtocolProperty.ETag);
        }
    }
}