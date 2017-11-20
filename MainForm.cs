/*
 * Created by SharpDevelop.
 * User: rotts
 * Date: 19.06.2013
 * Time: 18:42
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace Scrim
{
    
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using System.IO.Ports;
    using System.IO;
    using System.Xml;
    using System.Security;
    using System.Security.Principal;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.ServiceProcess;
    /// <summary>
    /// Description of MainForm.
    /// </summary>
    /// 
    public enum ServiceStartupType : int

    {

        SERVICE_BOOT_START = 0x00000000, //A device driver started by the system loader. This value is valid only for driver services.

        SERVICE_SYSTEM_START = 0x00000001, //A device driver started by the IoInitSystem function. This value is valid only for driver services.

        SERVICE_AUTO_START = 0x00000002, //A service started automatically by the service control manager during system startup.

        SERVICE_DEMAND_START = 0x00000003, //A service started by the service control manager when a process calls the StartService function.

        SERVICE_DISABLED = 0x00000004 //A service that cannot be started. Attempts to start the service result in the error code ERROR_SERVICE_DISABLED.

    }
    public enum ServiceControlAccessRights : int {

        SC_MANAGER_CONNECT = 0x0001, //Required to connect to the service control manager.

        SC_MANAGER_CREATE_SERVICE = 0x0002, //Required to call the CreateService function to create a service object and add it to the database.

        SC_MANAGER_ENUMERATE_SERVICE= 0x0004, //Required to call the EnumServicesStatusEx function to list the services that are in the database.

        SC_MANAGER_LOCK = 0x0008, //Required to call the LockServiceDatabase function to acquire a lock on the database.

        SC_MANAGER_QUERY_LOCK_STATUS = 0x0010, //Required to call the QueryServiceLockStatus function to retrieve the lock status information for the database

        SC_MANAGER_MODIFY_BOOT_CONFIG= 0x0020, //Required to call the NotifyBootConfigStatus function.

        SC_MANAGER_ALL_ACCESS = 0xF003F //Includes STANDARD_RIGHTS_REQUIRED, in addition to all access rights in this table.

    }
    public enum ServiceAccessRights : int {

        SERVICE_QUERY_CONFIG = 0x0001, //Required to call the QueryServiceConfig and QueryServiceConfig2 functions to query the service configuration.

        SERVICE_CHANGE_CONFIG = 0x0002, //Required to call the ChangeServiceConfig or ChangeServiceConfig2 function to change the service configuration. Because this grants the caller the right to change the executable file that the system runs, it should be granted only to administrators.

        SERVICE_QUERY_STATUS = 0x0004, //Required to call the QueryServiceStatusEx function to ask the service control manager about the status of the service.

        SERVICE_ENUMERATE_DEPENDENTS = 0x0008, //Required to call the EnumDependentServices function to enumerate all the services dependent on the service.

        SERVICE_START = 0x0010, //Required to call the StartService function to start the service.

        SERVICE_STOP = 0x0020, //Required to call the ControlService function to stop the service.

        SERVICE_PAUSE_CONTINUE = 0x0040, //Required to call the ControlService function to pause or continue the service.

        SERVICE_INTERROGATE = 0x0080, //Required to call the ControlService function to ask the service to report its status immediately.

        SERVICE_USER_DEFINED_CONTROL = 0x0100, //Required to call the ControlService function to specify a user-defined control code.

        SERVICE_ALL_ACCESS = 0xF01FF //Includes STANDARD_RIGHTS_REQUIRED in addition to all access rights in this table.

    }
    public enum ServiceType : uint {

        SERVICE_KERNEL_DRIVER = 0x00000001, //Driver service.

        SERVICE_FILE_SYSTEM_DRIVER = 0x00000002, //File system driver service.

        SERVICE_WIN32_OWN_PROCESS = 0x00000010, //Service that runs in its own process.

        SERVICE_WIN32_SHARE_PROCESS = 0x00000020, //Service that shares a process with other services.

        SERVICE_INTERACTIVE_PROCESS = 0x00000100, //The service can interact with the desktop.

        SERVICE_NO_CHANGE = 0xffffffff

    }
    public enum ServiceErrorControl : uint

    {

        SERVICE_ERROR_IGNORE = 0x00000000, //The startup program ignores the error and continues the startup operation.

        SERVICE_ERROR_NORMAL = 0x00000001, //The startup program logs the error in the event log but continues the startup operation.

        SERVICE_ERROR_SEVERE = 0x00000002, //SERVICE_ERROR_CRITICAL = 0x00000003, //The startup program logs the error in the event log, if possible. If the last-known-good configuration is being started, the startup operation fails. Otherwise, the system is restarted with the last-known good configuration.

        SERVICE_ERROR_CRITICAL = 0x00000003, //The startup program logs the error in the event log, if possible. If the last-known-good configuration is being started, the startup operation fails. Otherwise, the system is restarted with the last-known good configuration.

        SERVICE_NO_CHANGE = 0xffffffff

    }
    
    
    public partial class MainForm : Form
    {
        
        private string userName;
        private string userSID;
        private string selectedService;
        private string sddl,sddlu;
        private string settingFileName="wsmgr_settings.xml";
        
        private bool setAccessRights;
        
        private ServiceController[] services;
        private DataTable _tblServices = new DataTable();
        
        public MainForm()
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();
            
            _tblServices.Columns.Add( new DataColumn( "ServiceName", typeof( System.String ) ));
            _tblServices.Columns.Add( new DataColumn( "DisplayName", typeof( System.String ) ));
            _tblServices.Columns.Add( new DataColumn( "Index", typeof( System.Int32 ) )); //used for sorting
            
            //
            // TODO: Add constructor code after the InitializeComponent() call.
            //
            
            readSettings();
            getServices();
        }
        
        public void readSettings()
        {
            if(!File.Exists(settingFileName))
            {
                XmlWriter writer = XmlWriter.Create(@settingFileName);
                writer.WriteStartDocument();
                writer.WriteStartElement("settings");
                
                writer.WriteAttributeString("rewrite","false");
                writer.WriteAttributeString("sid","");
                writer.WriteAttributeString("user","");

                writer.WriteEndElement();
                writer.Close();
            }

            else if(File.Exists(settingFileName))    {
                XmlReader myreader = new XmlTextReader(@settingFileName);

                try {
                    while (myreader.Read())
                    {
                        switch (myreader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (myreader.HasAttributes)
                                {
                                    setAccessRights=bool.Parse(myreader.GetAttribute("rewrite"));
                                    userName=myreader.GetAttribute("user");
                                    userSID=myreader.GetAttribute("sid");
                                    selectedService=myreader.GetAttribute("serviceName");
                                    myreader.Close();
                                }
                                break;
                        }
                    }
                    textBoxProcess.SelectedValue=selectedService;
                }
                
                catch (XmlException e) {
                    MessageBox.Show(string.Format("Fehler: Kann die \"settings.xml\"-Datei nicht lesen:\n\n{0}",e),"Fehler",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }


                if(setAccessRights)    {
                    if(MessageBox.Show(string.Format("Dem Benutzer {0}\nmit der SID {1} sollen die Rechte zum " +
                                                     "Starten und Stoppen des Dienstes gegeben werden.\n\n" +
                                                     "Sind Sie sicher? \n\nSie müssen Administratorbberechtigungen besitzen " +
                                                     "um fortfahren zu können!",userName,userSID),"Benutzerrechte für einen Dienst ändern",
                                       MessageBoxButtons.OKCancel,MessageBoxIcon.Asterisk)== DialogResult.OK)    {
                        
                        if(!IsAdministrator()){
                            if(MessageBox.Show(string.Format("Fehler: Keine Administratorberechtigungen gefunden."),"Administratorberechtigungen benötigt",
                                               MessageBoxButtons.OK,MessageBoxIcon.Error)== DialogResult.OK)    {
                                setAccessRights=false;
                                Application.Exit();
                                Environment.Exit(0);
                            }
                        }
                		sddl=RunProcess(string.Format("sc"),string.Format("sdshow {0}",selectedService.ToLower()),true, false);
                        Trace.WriteLine(sddl);
                        sddl=sddl.Replace("\r\n","");
                        Trace.WriteLine(sddl);
                        
                        if(!sddl.Contains(string.Format("(A;;RPWPCR;;;{0})",userSID)))    {
                            sddl=sddl.Insert(sddl.IndexOf("S:(AU"),string.Format("(A;;RPWPCR;;;{0})",userSID));
                            sddl=RunProcess(string.Format("sc"),string.Format("sdset {0} \"{1}\"",selectedService.ToLower(),sddl),true,false);
                            
                            if(sddl.Contains("ERFOLG")){
                                MessageBox.Show(string.Format("Die Rechte wurden erfolgreich gesetzt\n\n{0}",sddl),"Erfolgreich",
                                                MessageBoxButtons.OK,MessageBoxIcon.Information);
                                setAccessRights=false;
                            }
                            else{
                                MessageBox.Show(string.Format("Fehler beim Setzen der Rechte\n\n{0}",sddl),"Fehler",
                                                MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
                            }
                        }

                        else{
                            if(MessageBox.Show(string.Format("Die zu gebenden Berechtigungen wurden bereits hinzugefügt.\n\nMöchten Sie die Berechtigung wieder entziehen?"),"Keine Aktion erforderlich",
                                               MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.Yes)    {
                                
                                sddlu=sddl;
                                sddlu=sddlu.Replace(string.Format("(A;;RPWPCR;;;{0})",userSID),"");
                                sddlu=RunProcess(string.Format("sc"),string.Format("sdset {0} \"{1}\"",selectedService.ToLower(),sddlu),true,false);
                                
                                if(sddlu.Contains("ERFOLG")){
                                    MessageBox.Show("Die Rechte wurden erfolgreich entzogen","Erfolgreich",
                                                    MessageBoxButtons.OK,MessageBoxIcon.Information);
                                    setAccessRights=false;
                                    if(System.IO.File.Exists(settingFileName)) {
                                        try
                                        {
                                            System.IO.File.Delete(@settingFileName);
                                        }
                                        catch (System.IO.IOException e)
                                        {
                                            Console.WriteLine(e.Message);
                                            return;
                                        }
                                    }
                                }
                                else{
                                    MessageBox.Show(string.Format("Fehler beim Setzen der Rechte\n\n{0}",sddlu),"Fehler",
                                                    MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
                                }
                                
                            }
                            
                            setAccessRights = false;
                            Environment.Exit(0);
                        }
                    }
                    Environment.Exit(0);
                }
            }
        }
        
        public bool saveSettings()    {
            
            XmlWriter writer = XmlWriter.Create(@settingFileName);
            writer.WriteStartDocument();
            writer.WriteStartElement("settings");

            writer.WriteAttributeString("rewrite", "true");
            writer.WriteAttributeString("user",userName);
            writer.WriteAttributeString("sid",userSID);
            writer.WriteAttributeString("serviceName",selectedService);
            
            if(textBoxProcess.Text=="<Select A Service>")    {
                MessageBox.Show("bitte erst einen Prozessnamen angeben","Fehler",MessageBoxButtons.OK,MessageBoxIcon.Information);
                writer.WriteAttributeString("processName","");
                writer.WriteEndElement();
                writer.Close();
                return true;
            }
            
            else    {
                writer.WriteAttributeString("processName",textBoxProcess.Text);
                writer.WriteEndElement();
                writer.Close();
            }
            return false;
        }
        
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            
            if (null != identity)
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            
            return false;
        }

        public static string RunProcess(string name, string arguments, bool redirect, bool asAdministrator)
        {
            string path = Path.GetDirectoryName(name);
            
            if (String.IsNullOrEmpty(path))
            {
                path = Environment.CurrentDirectory;
            }
            
            ProcessStartInfo info = new ProcessStartInfo
            {
                UseShellExecute=true,
                WorkingDirectory = path,
                FileName = name,
                Arguments = arguments
            };
            
            if(redirect)    {
                info.UseShellExecute=false;
                info.RedirectStandardOutput=true;
                info.CreateNoWindow=true;
            }
            if ((asAdministrator)&(!IsAdministrator()))    {
                info.Verb = "runas";
            }
            
            try
            {
                Trace.WriteLine(info.Arguments);
                Process p = new Process();
                p.StartInfo=info;
                p.Start();
                if(redirect)    {
                    return string.Format("{0}",p.StandardOutput.ReadToEnd());
                }
                
                else
                    return "success";
            }
            
            catch (Win32Exception ex)
            {
                Trace.WriteLine(ex);
            }
            
            return null;
        }
        
        public void getServices()
        {
            // get list of Windows services
            services = ServiceController.GetServices();

            foreach(ServiceController controller in services )
            {
                DataRow row = _tblServices.NewRow();
                row["ServiceName"] = controller.ServiceName;
                row["DisplayName"] = controller.DisplayName;
                row["Index"] = 1;

                _tblServices.Rows.Add( row );
            }
            
            //add a dummy row
            DataRow blankRow = _tblServices.NewRow();
            blankRow["ServiceName"] = "empty";
            blankRow["DisplayName"] = "<Select A Service>";
            blankRow["Index"] = 0;
            _tblServices.Rows.Add( blankRow );

            //use a DataView to sort the entries. I use the "Index" Column to force my blank record to the top
            DataView view = new DataView( _tblServices, string.Empty, "Index, DisplayName", DataViewRowState.CurrentRows );
            textBoxProcess.DataSource = view;
            textBoxProcess.ValueMember = "ServiceName";
            textBoxProcess.DisplayMember = "DisplayName";
        }
        
        void TextBoxProcessSelectedIndexChanged(object sender, System.EventArgs e)
        {
            if((string)textBoxProcess.SelectedValue!="empty"){
                ServiceController test = new ServiceController((string)textBoxProcess.SelectedValue);
                selectedService = test.ServiceName;
            }
        }
        
        void Button1Click(object sender, System.EventArgs e)
        {
            if (selectedService!=null)
            {
                
                NTAccount f = new NTAccount(string.Format("{0}",System.Security.Principal.WindowsIdentity.GetCurrent().Name));
                string sidString = (string.Format("{0}",System.Security.Principal.WindowsIdentity.GetCurrent().User));
                
                userName=string.Format("{0}",f);
                userSID=string.Format("{0}",sidString);

                if(!saveSettings())    {
                    if(MessageBox.Show(string.Format("Dem Momentan angemeldeten Benutzer {0}\nmit der SID {1} werden die Rechte zum " +
                                                     "starten und stoppen des Dienstes \"{2}\" gegeben.\n\n" +
                                                     "Sind Sie sicher? \n\nDas Programm wird beendet und Sie müssen es mit " +
                                                     "Administratorberechtigungen neu starten!",f,sidString,selectedService),"Benutzerrechte für einen Dienst ändern",
                                       MessageBoxButtons.OKCancel,MessageBoxIcon.Asterisk)== DialogResult.OK)    {
                        
                        setAccessRights = true;
                        Application.Exit();
                    }
                }
            }
            
            else{
                MessageBox.Show("bitte erst einen Prozessnamen angeben","Fehler",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
        }
        
        void Button2Click(object sender, System.EventArgs e)
        {
            if(System.IO.File.Exists(settingFileName)) {
                try
                {
                    System.IO.File.Delete(@settingFileName);
                }
                catch (System.IO.IOException ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
            }
            this.Close();
        }
        
    }

}
