using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Configuration.Install;
using System.Diagnostics;
using System;


namespace TASQSrv
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
        static string ServiceName = "TASQueue";
        static void Main (string[] args )
		{
            if (args.GetLength(0) > 0)
            {
               EventLogManager.RegisterCustomEventSource(ServiceName,"Application");
                if (args[0].IndexOf("-r") != -1 || args[0].IndexOf("-s") != -1)
                {
                    IDictionary ht = new Hashtable();
                    AssemblyInstaller installer = new AssemblyInstaller();
                    installer.Path = Process.GetCurrentProcess().MainModule.FileName;
                    installer.UseNewContext = true;
                    installer.Install(ht);
                    installer.Commit(ht);
                    Console.WriteLine(ServiceName + " registered.");
                    
                }
                else if (args[0].IndexOf("-u") != -1)
                {
                    IDictionary ht = new Hashtable();
                    AssemblyInstaller installer = new AssemblyInstaller();
                    installer.Path = Process.GetCurrentProcess().MainModule.FileName;
                    installer.UseNewContext = true;
                    installer.Uninstall(ht);
                    Console.WriteLine(ServiceName + " unregistered.");
                    
                }
                else if (args[0].IndexOf("-t") != -1)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new frmTest(ServiceName));
                }
                else 
                    Console.WriteLine("invalid switch entered");
                //Console.ReadKey();
                return;
            }
            else
            {

				ServiceBase[] ServicesToRun;
				ServicesToRun = new ServiceBase[] { new TASQSeq(ServiceName) };
				ServiceBase.Run(ServicesToRun);
			}
		}
    }
    public class EventLogManager
    {
        public static void RegisterCustomEventSource(string sourceName, string logName)
        {
            try
            {
                if (!EventLog.SourceExists(sourceName))
                {
                    // Create a new EventSourceCreationData object
                    EventSourceCreationData sourceData = new EventSourceCreationData(sourceName, logName);

                    // Optionally, set resource files for localized messages
                    // sourceData.CategoryResourceFile = "path_to_category_resource_file.dll";
                    // sourceData.MessageResourceFile = "path_to_message_resource_file.dll";
                    // sourceData.ParameterResourceFile = "path_to_parameter_resource_file.dll";

                    EventLog.CreateEventSource(sourceData);
                    Console.WriteLine($"Event source '{sourceName}' registered successfully in log '{logName}'.");
                }
                else
                {
                    Console.WriteLine($"Event source '{sourceName}' already exists.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering event source: {ex.Message}");
            }
        }
    }
}