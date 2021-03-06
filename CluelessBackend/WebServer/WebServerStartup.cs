using System.Collections.Generic;
using System.Threading.Tasks;
using CluelessBackend.GlobalServices;
using CluelessNetwork.BackendNetworkInterfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CluelessBackend.WebServer
{
    public class WebServerStartup
    {
        private WebsocketManager _websocketProcessor;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var gameInstanceService = new GameInstanceService();
            var chatService = new ChatService(gameInstanceService);
            var cluelessNetworkServer = new CluelessNetworkServer(gameInstanceService);
            _websocketProcessor = new WebsocketManager(cluelessNetworkServer);
            Program.SingletonServices = new List<object>() { gameInstanceService, chatService, cluelessNetworkServer };
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var websocket = await context.WebSockets.AcceptWebSocketAsync();
                        var socketFinishedTcs = new TaskCompletionSource();
                        _websocketProcessor.AddSocket(websocket, socketFinishedTcs);
                        await socketFinishedTcs.Task;
                    }
                }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
            });
            Program.WebsocketServerIsReady = true;
        }
    }
}