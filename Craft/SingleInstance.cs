using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Reactive.Disposables;
using System.IO;
using Newtonsoft.Json;
using Craft.Utils;

namespace Craft
{
    /// <summary>
    /// Interface App should implement to allow indicating when the instance should be activated
    /// </summary>
    public interface ISingleInstanceApp
    {
        Task OnSignalFromAnotherInstance(SingleInstance.Message message);
    }

    /// <summary>
    /// This class checks to make sure that only one instance of
    /// this application is running at a time.
    /// </summary>
    /// <remarks>
    /// Based on http://blogs.microsoft.co.il/arik/2010/05/28/wpf-single-instance-application/
    /// with modifications to use NamedPipes so it works with .NET Core
    ///
    /// Note: this class should be used with some caution, because it does no
    /// security checking. For example, if one instance of an app that uses this class
    /// is running as Administrator, any other instance, even if it is not
    /// running as Administrator, can activate it with command line arguments.
    /// For most apps, this will not be much of an issue.
    /// </remarks>
    public static class SingleInstance
    {
        /// <summary>
        /// Application mutex that will be taken by the first instance of the application
        /// </summary>
        private static Mutex singleInstanceMutex;

        /// <summary>
        /// Disposable that will stop listening for messages from other apps when disposed
        /// </summary>
        private static IDisposable otherInstanceListener;

        /// <summary>
        /// Checks if the instance of the application attempting to start is the first instance.
        /// If not, activates the first instance.
        /// </summary>
        /// <returns>True if this is the first instance of the application.</returns>
        public static bool InitializeAsFirstInstance(string appGuid, IList<string> commandLineArgs)
        {
            // we don't want to be trying to signal an app running under a different users account on the same machine
            // so make sure that our mutex & channels are set up with per-user info
            string mutexName = "Local\\" + Uri.EscapeDataString($"{Environment.UserDomainName}_{Environment.UserName}_{appGuid}");
            string userIdentifier = Uri.EscapeDataString($"{Environment.UserDomainName}_{Environment.UserName}");

            // For Pipe Naming considerations see https://docs.microsoft.com/en-us/windows/win32/ipc/pipe-names
            // The max length for a pipe name string is 256 chars
            // 9 chars Local pipe should start with "\\.\pipe\"
            // 11 chars "PlanGridApp"
            // 1 char "_"
            // 36 chars Our app's GUID
            // 1 char "_"
            // 32 chars Domain and user name may be long (we don't know) so use the MD5 hash of user info
            // -----
            // 90 chars, well under the limit
            string pipeName = $"\\\\.\\pipe\\PlanGridApp_{appGuid}_{Md5Hash(userIdentifier)}";

            // Create mutex based on unique application Id to check if this is the first instance of the application.
            singleInstanceMutex = new Mutex(true, mutexName, out bool firstInstance);

            if (firstInstance)
            {
                Console.WriteLine("First instance: creating named pipe server");
                otherInstanceListener = CreateNamedPipeServer(pipeName);
            }
            else
            {
                Console.WriteLine("Another instance running: signaling first instance");
                SignalFirstInstance(pipeName, commandLineArgs);

                // for testing purposes, the second instance will wait on the first
                // so you can start the app in visual studio and then trigger it from git command line
                singleInstanceMutex.Close();
                while (true)
                {
                    singleInstanceMutex = new Mutex(true, mutexName, out bool otherClosed);
                    if (otherClosed)
                    {
                        break;
                    }
                    singleInstanceMutex.Close();
                    Thread.Sleep(250);
                }
            }

            return firstInstance;

            string Md5Hash(string input)
            {
                MD5 md5 = MD5.Create();
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);

                var hashString = new StringBuilder();
                foreach (byte @byte in hash)
                {
                    _ = hashString.Append(@byte.ToString("X2"));
                }

                return hashString.ToString();
            }
        }

        /// <summary>
        ///     Starts a new pipe server that listens for messages from other application instances
        /// </summary>
        private static IDisposable CreateNamedPipeServer(string pipeName)
        {
            var disposables = new CompositeDisposable();

            // Create pipe and start the async connection wait
            var serverStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.In,
                maxNumberOfServerInstances: 1,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                inBufferSize: 0,
                outBufferSize: 0);

            disposables.Add(serverStream);

            bool continueListening = true;

            disposables.Add(new ActionOnDispose(() => continueListening = false));

            // Begin async wait for connections
            serverStream.BeginWaitForConnection(OnConnection, serverStream);

            return disposables;

            void OnConnection(IAsyncResult iAsyncResult)
            {
                try
                {
                    // End waiting for the connection
                    serverStream.EndWaitForConnection(iAsyncResult);

                    // Read data from client
                    string clientText;
                    using (var sr = new StreamReader(serverStream))
                    {
                        clientText = sr.ReadToEnd();
                    }

                    var payload = JsonConvert.DeserializeObject<Message>(clientText);


                    // let the app know that a second instance has given us their command line arguments
                    if (Application.Current == null)
                    {
                        return;
                    }

                    Application.Current.Dispatcher.InvokeAsync(async () => await ((ISingleInstanceApp)Application.Current).OnSignalFromAnotherInstance(payload), DispatcherPriority.Normal);
                }
                catch (Exception)
                {
                    // ignore any problems reading the pipe or the json payload, etc
                    // just reset for the next connection
                }
                finally
                {
                    // Close the original pipe (we will create a new one each time)
                    serverStream.Dispose();

                    // Create a new pipe server for next connection
                    if (continueListening)
                    {
                        disposables.Add(CreateNamedPipeServer(pipeName));
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up single-instance code, clearing shared resources, mutexes, etc.
        /// </summary>
        public static void Cleanup()
        {
            if (singleInstanceMutex != null)
            {
                singleInstanceMutex.Close();
                singleInstanceMutex = null;
            }

            if (otherInstanceListener != null)
            {
                otherInstanceListener.Dispose();
                otherInstanceListener = null;
            }
        }

        /// <summary>
        /// Contact the previously running instance of the app, and let them know what the startup arguments were for this instance.
        /// </summary>
        private static void SignalFirstInstance(string pipeName, IList<string> args)
        {
            try
            {
                // Collect the data we want to send to the already running application
                var message = new Message()
                {
                    Args = args.ToList()
                };

                // we will send UTF-8 encoding of JSON serilization
                string json = JsonConvert.SerializeObject(message);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                // Send the message
                using var namedPipeClientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
                namedPipeClientStream.Connect(timeout: 3000); // Maximum wait 3 seconds
                namedPipeClientStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to signal first instance " + ex);
                Console.WriteLine(pipeName + " " + string.Join(", ", args ?? new List<string>()));

                // go ahead and throw the error, this instance of the app will be closing anyway
                // by throwing, the error will show up in EventViewer etc.
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }


        /// <summary>
        /// The data that we send from one instance of our app to another
        /// </summary>
        [DataContract]
        public class Message
        {
            [DataMember(Name = "args")]
            public List<string> Args { get; set; } = new List<string>();
        }
    }
}