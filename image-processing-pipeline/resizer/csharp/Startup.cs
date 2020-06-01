// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.IO;
using SixLabors.ImageSharp;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Processing;
using Newtonsoft.Json;
using Common;
using System.Text;

namespace Resizer
{
    public class Startup
    {
        private const string PubSubTopicId = "fileresized";

        private const int ThumbWidth = 400;
        private const int ThumbHeight = 400;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            logger.LogInformation("Service is starting...");

            app.UseRouting();

            var projectId = Environment.GetEnvironmentVariable("PROJECT_ID");
            logger.LogInformation($"Event Adapter: pubsub with projectId '{projectId}' and topicId '{PubSubTopicId}'");
            var eventAdapter = new PubSubEventAdapter(projectId, PubSubTopicId, logger);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    var cloudEvent = await eventAdapter.ReadEvent(context);

                    var cloudEventData = JValue.Parse((string)cloudEvent.Data);

                    // {
                    // "message": {
                    //     "data": "eyJidWNrZXQiOiJldmVudHMtYXRhbWVsLWltYWdlcy1pbnB1dCIsIm5hbWUiOiJiZWFjaC5qcGcifQ==",
                    // },
                    // "subscription": "projects/events-atamel/subscriptions/cre-europe-west1-trigger-resizer-sub-000"
                    // }
                    var data = (string)cloudEventData["message"]["data"];
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(data));
                    logger.LogInformation($"Decoded string: {decoded}");

                    var parsed = JValue.Parse(decoded);
                    var inputBucket = (string)parsed["bucket"];
                    var inputObjectName = (string)parsed["name"];
                    logger.LogInformation($"Input bucket '{inputBucket}' name '{inputObjectName}'");

                    try
                    {
                        using (var inputStream = new MemoryStream())
                        {
                            var client = await StorageClient.CreateAsync();
                            await client.DownloadObjectAsync(inputBucket, inputObjectName, inputStream);
                            logger.LogInformation($"Downloaded '{inputObjectName}' from bucket '{inputBucket}'");

                            using (var outputStream = new MemoryStream())
                            {
                                inputStream.Position = 0; // Reset to read
                                using (Image image = Image.Load(inputStream))
                                {
                                    image.Mutate(x => x
                                        .Resize(ThumbWidth, ThumbHeight)
                                    );
                                    logger.LogInformation($"Resized image '{inputObjectName}' to {ThumbWidth}x{ThumbHeight}");

                                    image.SaveAsPng(outputStream);
                                }

                                var outputBucket = Environment.GetEnvironmentVariable("BUCKET");
                                var outputObjectName = $"{Path.GetFileNameWithoutExtension(inputObjectName)}-{ThumbWidth}x{ThumbHeight}.png";
                                await client.UploadObjectAsync(outputBucket, outputObjectName, "image/png", outputStream);
                                logger.LogInformation($"Uploaded '{outputObjectName}' to bucket '{outputBucket}'");

                                var replyData = JsonConvert.SerializeObject(new {bucket = outputBucket, name = outputObjectName});
                                await eventAdapter.WriteEvent(replyData, context);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error processing {inputObjectName}: " + e.Message);
                        throw e;
                    }
                });
            });
        }
    }
}
