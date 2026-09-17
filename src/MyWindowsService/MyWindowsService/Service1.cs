using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Couchbase;
using Couchbase.Configuration.Client;

namespace MyWindowsService
{
    public partial class Service1 : ServiceBase
    {
        // Cambia este valor entre builds para demostrar visualmente una "actualización" del servicio.
        private const string VERSION = "v1.1";
        private const string HttpPrefix = "http://localhost:8090/";

        bool firstRun;
        private HttpListener _httpListener;
        private Thread _httpListenerThread;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            firstRun = true;

            // Arranca el listener HTTP primero: listener.Start() solo abre el socket
            // (no bloquea) y el loop de escucha corre en su propio hilo.
            StartHttpServer();

            // El logging a Couchbase implica llamadas de red síncronas (ClusterHelper.Initialize
            // + Upsert). Si el servidor Couchbase no responde, esto puede colgarse mucho más
            // tiempo que el timeout de arranque de SCM y dejar el servicio en START_PENDING.
            // Se despacha en background para que OnStart() retorne de inmediato.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Log(
                    LogToCouchbase(new List<string> { "OnStart:", DateTime.Now.ToString() })
                );
            });
        }

        protected override void OnStop()
        {
            // Debe permanecer síncrono: listener.Stop() es rápido y SCM espera que
            // OnStop() cierre los recursos del servicio de inmediato.
            StopHttpServer();

            // Mismo problema que en OnStart(): LogToCouchbase() bloquea en red. Se
            // despacha en background para que OnStop() retorne de inmediato y no
            // deje el servicio pegado en STOP_PENDING.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Log(
                     LogToCouchbase(new List<string> { "OnStop:", DateTime.Now.ToString() })
                 );
            });
        }

        private void StartHttpServer()
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(HttpPrefix);
                _httpListener.Start();

                _httpListenerThread = new Thread(HttpListenerLoop)
                {
                    IsBackground = true
                };
                _httpListenerThread.Start();
            }
            catch (Exception ex)
            {
                Log(new List<string> { "StartHttpServer Exception:", ex.Message, ex.StackTrace });
            }
        }

        private void StopHttpServer()
        {
            try
            {
                if (_httpListener != null)
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                }
            }
            catch (Exception ex)
            {
                Log(new List<string> { "StopHttpServer Exception:", ex.Message, ex.StackTrace });
            }
        }

        private void HttpListenerLoop()
        {
            while (_httpListener != null && _httpListener.IsListening)
            {
                try
                {
                    var context = _httpListener.GetContext();
                    HandleRequest(context);
                }
                catch (HttpListenerException)
                {
                    // Listener fue detenido (OnStop) - salir del loop limpiamente.
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log(new List<string> { "HttpListenerLoop Exception:", ex.Message, ex.StackTrace });
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                string html =
                    "<html><head><title>MyWindowsService</title></head><body>" +
                    "<h1>MyWindowsService está corriendo</h1>" +
                    "<p>Fecha y hora del servidor: " + DateTime.Now.ToString() + "</p>" +
                    "<p>Versión: " + VERSION + "</p>" +
                    "</body></html>";

                byte[] buffer = Encoding.UTF8.GetBytes(html);

                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Log(new List<string> { "HandleRequest Exception:", ex.Message, ex.StackTrace });
            }
        }

        private List<string> Log(List<string> lines)
        {
            try
            {
                File.AppendAllLines("c:\\MyWindowsService.log.txt", lines);
            }
            catch (Exception ex)
            {
                lines.AddRange(
                    new[] {
                        "Excpetion:",
                        ex.Message,
                        ex.StackTrace
                    });
            }

            return lines;
        }

        private List<string> LogToCouchbase(List<string> lines)
        {
            try
            {
                if (firstRun)
                {
                    var config = new ClientConfiguration
                    {
                        Servers = new List<Uri> { new Uri("http://10.0.0.4:8091") }
                    };

                    ClusterHelper.Initialize(config);

                    firstRun = false;
                }

                // this will overwrite any old log lines!
                var result =
                    ClusterHelper
                    .GetBucket("default")
                    .Upsert<dynamic>(
                        "MyWindowsService.log.txt",
                        new
                        {
                            id = "MyWindowsService.log.txt",
                            log = string.Join("\n", lines)
                        }
                    );

                lines.AddRange(
                new[] {
                        "Couchbase result: ",
                        result.Success.ToString(),
                        "Document Key: ",
                        "MyWindowsService.log.txt"
                });
            }
            catch (Exception ex)
            {
                lines.AddRange(
                new[] {
                        "Excpetion:",
                        ex.Message,
                        ex.StackTrace
                });
            }

            return lines;
        }
    }
}