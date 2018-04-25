#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Factory;
using Dapplo.HttpExtensions.JsonNet;
using Greenshot.Addon.Tfs.Entities;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Newtonsoft.Json.Linq;

namespace Greenshot.Addon.Tfs
{
    /// <summary>
    /// This capsulates the TFS api calls
    /// </summary>
    [Export]
    public class TfsClient
    {
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly ITfsConfiguration _tfsConfiguration;
        private readonly HttpBehaviour _tfsHttpBehaviour;

        [ImportingConstructor]
        public TfsClient(
            ICoreConfiguration coreConfiguration,
            ITfsConfiguration tfsConfiguration,
            INetworkConfiguration networkConfiguration)
        {
            _coreConfiguration = coreConfiguration;
            _tfsConfiguration = tfsConfiguration;

            _tfsHttpBehaviour = new HttpBehaviour
            {
                HttpSettings = networkConfiguration,
                JsonSerializer = new JsonNetJsonSerializer()
            };
        }

        public bool CanUpdate => _tfsConfiguration.TfsUri != null && !string.IsNullOrEmpty(_tfsConfiguration.ApiKey);
        public IDictionary<long, WorkItem> WorkItems { get; } = new Dictionary<long, WorkItem>();

        public async Task UpdateWorkItems()
        {
            if (!CanUpdate)
            {
                return;
            }
            var workItems = await GetOwnWorkitems();
            foreach (var workItem in workItems.Items)
            {
                WorkItems[workItem.Id] = workItem;
            }
        }
        /// <summary>
        /// Retrieve the own workitems
        /// </summary>
        /// <returns>WorkItemList</returns>
        public async Task<WorkItemList> GetOwnWorkitems()
        {
            _tfsHttpBehaviour.MakeCurrent();
            Uri apiUri = _tfsConfiguration.TfsUri.AppendSegments("_apis").ExtendQuery("api-version", "3.0");
            var client = HttpClientFactory.Create(_tfsConfiguration.TfsUri).SetBasicAuthorization("", _tfsConfiguration.ApiKey);

            var workitemsQueryUri = apiUri.AppendSegments("wit", "wiql");

            var wiql = new JObject { { "query", "Select [System.Id] FROM WorkItems WHERE [System.AssignedTo] = @me" } };

            var queryResult = await client.PostAsync<HttpResponse<WorkItemQueryResult, string>>(workitemsQueryUri, wiql);
            if (queryResult.HasError)
            {
                throw new Exception(queryResult.ErrorResponse);
            }

            var workItemsUri = apiUri.AppendSegments("wit", "workItems").ExtendQuery("ids", string.Join(",",queryResult.Response.Items.Select(item => item.Id)));
            var result = await client.GetAsAsync<HttpResponse<WorkItemList, string>>(workItemsUri);
            if (result.HasError)
            {
                throw new Exception(result.ErrorResponse);
            }

            return result.Response;
        }

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/rest/api/vsts/wit/attachments/create">here</a>
        /// </summary>
        /// <param name="surface">ISurface to attach</param>
        /// <returns></returns>
        public async Task<CreateAttachmentResult> CreateAttachment(ISurface surface)
        {
            _tfsHttpBehaviour.MakeCurrent();

            var client = HttpClientFactory.Create(_tfsConfiguration.TfsUri).SetBasicAuthorization("", _tfsConfiguration.ApiKey);
            Uri apiUri = _tfsConfiguration.TfsUri.AppendSegments("_apis").ExtendQuery("api-version", "3.0");

            var filename = surface.GenerateFilename(_coreConfiguration, _tfsConfiguration);
            var attachmentUri = apiUri.AppendSegments("wit", "attachments").ExtendQuery("fileName", filename);
            using (var imageStream = new MemoryStream())
            {
                surface.WriteToStream(imageStream, _coreConfiguration, _tfsConfiguration);
                imageStream.Position = 0;
                using (var content = new StreamContent(imageStream))
                {
                    content.SetContentType("application/octet-stream");
                    var createAttachmentresult = await client.PostAsync<HttpResponse<CreateAttachmentResult, string>>(attachmentUri, content);
                    if (createAttachmentresult.HasError)
                    {
                        throw new Exception(createAttachmentresult.ErrorResponse);
                    }
                    return createAttachmentresult.Response;
                }
            }
        }

        /// <summary>
        /// Link the WorkItem and the attachment that was created
        /// </summary>
        /// <param name="workItem">WorkItem</param>
        /// <param name="attachmentResult">CreateAttachmentResult</param>
        /// <param name="comment">string with optional comment</param>
        public async Task LinkAttachment(WorkItem workItem, CreateAttachmentResult attachmentResult, string comment = "Attached screenshot from Greenshot")
        {
            _tfsHttpBehaviour.MakeCurrent();
            var client = HttpClientFactory.Create(_tfsConfiguration.TfsUri).SetBasicAuthorization("", _tfsConfiguration.ApiKey);

            Uri apiUri = _tfsConfiguration.TfsUri.AppendSegments("_apis").ExtendQuery("api-version", "3.0");
            // https://docs.microsoft.com/en-us/rest/api/vsts/wit/work%20items/update#add_an_attachment
            var linkAttachmentUri = apiUri.AppendSegments("wit", "workItems", workItem.Id);
            var linkAttachmentRequest = new List<Operation>
            {
                new Operation
                {
                    OperationType = "add",
                    Path = "/relations/-",
                    Value = new Value
                    {
                        Relation = "AttachedFile",
                        Url = attachmentResult.Url,
                        Attributes = new Attributes
                        {
                            Comment = comment
                        }
                    }
                }
            };

            var content = HttpContentFactory.Create(linkAttachmentRequest);
            content.SetContentType("application/json-patch+json");
            var result = await client.PatchAsync<HttpResponse<string, string>>(linkAttachmentUri, content);
            if (result.HasError)
            {
                throw new Exception(result.ErrorResponse);
            }
        }
    }
}
