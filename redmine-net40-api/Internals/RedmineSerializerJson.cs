﻿/*
   Copyright 2011 - 2016 Adrian Popescu.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using Redmine.Net.Api.JSonConverters;
using Redmine.Net.Api.Types;
using Version = Redmine.Net.Api.Types.Version;

namespace Redmine.Net.Api.Internals
{
    internal static partial class RedmineSerializer
    {
        private static readonly Dictionary<Type, JavaScriptConverter> jsonConverters = new Dictionary<Type, JavaScriptConverter>
        {
            {typeof (Issue), new IssueConverter()},
            {typeof (Project), new ProjectConverter()},
            {typeof (User), new UserConverter()},
            {typeof (UserGroup), new UserGroupConverter()},
            {typeof (News), new NewsConverter()},
            {typeof (Query), new QueryConverter()},
            {typeof (Version), new VersionConverter()},
            {typeof (Attachment), new AttachmentConverter()},
            {typeof (IssueRelation), new IssueRelationConverter()},
            {typeof (TimeEntry), new TimeEntryConverter()},
            {typeof (IssueStatus),new IssueStatusConverter()},
            {typeof (Tracker),new TrackerConverter()},
            {typeof (TrackerCustomField),new TrackerCustomFieldConverter()},
            {typeof (IssueCategory), new IssueCategoryConverter()},
            {typeof (Role), new RoleConverter()},
            {typeof (ProjectMembership), new ProjectMembershipConverter()},
            {typeof (Group), new GroupConverter()},
            {typeof (GroupUser), new GroupUserConverter()},
            {typeof (Error), new ErrorConverter()},
            {typeof (IssueCustomField), new IssueCustomFieldConverter()},
            {typeof (ProjectTracker), new ProjectTrackerConverter()},
            {typeof (Journal), new JournalConverter()},
            {typeof (TimeEntryActivity), new TimeEntryActivityConverter()},
            {typeof (IssuePriority), new IssuePriorityConverter()},
            {typeof (WikiPage), new WikiPageConverter()},
            {typeof (Detail), new DetailConverter()},
            {typeof (ChangeSet), new ChangeSetConverter()},
            {typeof (Membership), new MembershipConverter()},
            {typeof (MembershipRole), new MembershipRoleConverter()},
            {typeof (IdentifiableName), new IdentifiableNameConverter()},
            {typeof (Permission), new PermissionConverter()},
            {typeof (IssueChild), new IssueChildConverter()},
            {typeof (ProjectIssueCategory), new ProjectIssueCategoryConverter()},
            {typeof (Watcher), new WatcherConverter()},
            {typeof (Upload), new UploadConverter()},
            {typeof (ProjectEnabledModule), new ProjectEnabledModuleConverter()},
            {typeof (CustomField), new CustomFieldConverter()},
            {typeof (CustomFieldRole), new CustomFieldRoleConverter()},
            {typeof (CustomFieldPossibleValue), new CustomFieldPossibleValueConverter()}
        };

        public static Dictionary<Type, JavaScriptConverter> JsonConverters { get { return jsonConverters; } }

        public static string JsonSerializer<T>(T type) where T : new()
        {
            var serializer = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
            serializer.RegisterConverters(new[] { jsonConverters[typeof(T)] });
            var jsonString = serializer.Serialize(type);
            return jsonString;
        }

        /// <summary>
        /// JSON Deserialization
        /// </summary>
        public static List<T> JsonDeserializeToList<T>(string jsonString, string root) where T : class, new()
        {
            int totalCount;
            return JsonDeserializeToList<T>(jsonString, root, out totalCount);
        }

        /// <summary>
        /// JSON Deserialization
        /// </summary>
        public static List<T> JsonDeserializeToList<T>(string jsonString, string root, out int totalCount) where T : class,new()
        {
            var result = JsonDeserializeToList(jsonString, root, typeof(T), out totalCount);

            return result == null ? null : ((ArrayList)result).OfType<T>().ToList();
        }

        public static T JsonDeserialize<T>(string jsonString, string root) where T : new()
        {
            var type = typeof(T);
            var result = JsonDeserialize(jsonString, type, root);
            if (result == null) return default(T);

            return (T)result;
        }

        public static object JsonDeserialize(string jsonString, Type type, string root)
        {
            if (string.IsNullOrEmpty(jsonString)) return null;

            var serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new[] { jsonConverters[type] });

            var dic = serializer.Deserialize<Dictionary<string, object>>(jsonString);
            if (dic == null) return null;

            object obj;
            if (dic.TryGetValue(root ?? type.Name.ToLowerInvariant(), out obj))
            {
                var deserializedObject = serializer.ConvertToType(obj, type);

                return deserializedObject;
            }
            return null;
        }

        private static void AddToList(JavaScriptSerializer serializer, IList list, Type type, object obj)
        {
            foreach (var item in (ArrayList)obj)
            {
                if (item is ArrayList)
                {
                    AddToList(serializer, list, type, item);
                }
                else
                {
                    var o = serializer.ConvertToType(item, type);
                    list.Add(o);
                }
            }
        }

        private static object JsonDeserializeToList(string jsonString, string root, Type type, out int totalCount)
        {
            totalCount = 0;
            if (string.IsNullOrEmpty(jsonString)) return null;

            var serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new[] { jsonConverters[type] });
            var dic = serializer.Deserialize<Dictionary<string, object>>(jsonString);
            if (dic == null) return null;

            object obj, tc;

            if (dic.TryGetValue(RedmineKeys.TOTAL_COUNT, out tc)) totalCount = (int)tc;

            if (dic.TryGetValue(root.ToLowerInvariant(), out obj))
            {
                var arrayList = new ArrayList();
                if (type == typeof(Error))
                {
                    string info = null;
                    foreach (var item in (ArrayList)obj)
                    {
                        var innerArrayList = item as ArrayList;
                        if (innerArrayList != null)
                        {
                            info = innerArrayList.Cast<object>().Aggregate(info, (current, item2) => current + (item2 as string + " "));
                        }
                        else
                            info += item as string + " ";
                    }
                    var err = new Error { Info = info };
                    arrayList.Add(err);
                }
                else
                {
                    AddToList(serializer, arrayList, type, obj);
                }
                return arrayList;
            }
            return null;
        }
    }
}