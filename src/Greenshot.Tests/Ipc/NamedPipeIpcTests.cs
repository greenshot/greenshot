/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Helpers.Ipc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    public class NamedPipeIpcTests
    {
        [Fact]
        public void EnvelopeSerialization_OpenFile_MatchesAdr002Schema()
        {
            string testFilePath = @"C:\Users\Test\Pictures\screenshot.png";
            var envelope = IpcEnvelope.CreateOpenFile(testFilePath);

            string json = JsonConvert.SerializeObject(envelope, Formatting.Indented);
            JObject jobj = JObject.Parse(json);

            Assert.Equal(1, jobj.Value<int>("version"));
            Assert.Equal("open_with", jobj.Value<string>("source"));
            Assert.Equal(testFilePath, jobj.Value<string>("raw_input"));

            JObject parsed = jobj.Value<JObject>("parsed");
            Assert.NotNull(parsed);
            Assert.Equal("open_file", parsed.Value<string>("action"));

            JObject parameters = parsed.Value<JObject>("parameters");
            Assert.NotNull(parameters);
            Assert.Equal(testFilePath, parameters.Value<string>("path"));

            // Round trip
            var deserialized = JsonConvert.DeserializeObject<IpcEnvelope>(json);
            Assert.NotNull(deserialized);
            Assert.Equal(1, deserialized.Version);
            Assert.Equal("open_with", deserialized.Source);
            Assert.Equal(testFilePath, deserialized.RawInput);
            Assert.NotNull(deserialized.Parsed);
            Assert.Equal("open_file", deserialized.Parsed.Action);
            Assert.Equal(testFilePath, deserialized.Parsed.Parameters["path"]);
        }

        [Fact]
        public void EnvelopeSerialization_ExitAndReload_MatchAdr002Schema()
        {
            var exitEnvelope = IpcEnvelope.CreateExit();
            string exitJson = JsonConvert.SerializeObject(exitEnvelope);
            var exitObj = JObject.Parse(exitJson);
            Assert.Equal(1, exitObj.Value<int>("version"));
            Assert.Equal("cli", exitObj.Value<string>("source"));
            Assert.Equal("--exit", exitObj.Value<string>("raw_input"));
            Assert.Equal("exit", exitObj["parsed"]?.Value<string>("action"));

            var reloadEnvelope = IpcEnvelope.CreateReloadConfig();
            string reloadJson = JsonConvert.SerializeObject(reloadEnvelope);
            var reloadObj = JObject.Parse(reloadJson);
            Assert.Equal(1, reloadObj.Value<int>("version"));
            Assert.Equal("cli", reloadObj.Value<string>("source"));
            Assert.Equal("--reload", reloadObj.Value<string>("raw_input"));
            Assert.Equal("reload_config", reloadObj["parsed"]?.Value<string>("action"));
        }

        [Fact]
        public void NamedPipeEndpoint_GeneratesPipeNameWithUserSid()
        {
            string userSid = NamedPipeEndpoint.GetUserSid();
            Assert.NotNull(userSid);
            Assert.StartsWith("S-", userSid);

            string expectedSid = WindowsIdentity.GetCurrent().User?.Value;
            Assert.Equal(expectedSid, userSid);

            string pipeName = NamedPipeEndpoint.GetPipeName();
            Assert.Equal("Greenshot_" + userSid, pipeName);

            var security = NamedPipeEndpoint.CreateServerSecurity();
            Assert.NotNull(security);
            Assert.True(security.AreAccessRulesProtected);
        }

        [Fact]
        public async Task NamedPipe_EndToEndTransmission_Succeeds()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<IpcEnvelope>();

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.MessageReceived += (s, e) =>
                {
                    tcs.TrySetResult(e);
                };
                server.Start();

                string filePath = @"C:\TestPath\capture.png";
                var envelope = IpcEnvelope.CreateOpenFile(filePath);

                bool sent = NamedPipeClient.SendMessage(testPipeName, envelope, timeoutMs: 3000);
                Assert.True(sent, "Client should successfully connect and send message to server.");

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(4000));
                Assert.Same(tcs.Task, completedTask);

                var receivedEnvelope = await tcs.Task;
                Assert.NotNull(receivedEnvelope);
                Assert.Equal(1, receivedEnvelope.Version);
                Assert.Equal("open_with", receivedEnvelope.Source);
                Assert.Equal(filePath, receivedEnvelope.RawInput);
                Assert.NotNull(receivedEnvelope.Parsed);
                Assert.Equal("open_file", receivedEnvelope.Parsed.Action);
                Assert.True(receivedEnvelope.Parsed.Parameters.TryGetValue("path", out var path));
                Assert.Equal(filePath, path);
            }
        }
    }
}
