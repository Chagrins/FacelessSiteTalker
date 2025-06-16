using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.PropertyGridInternal;
using Microsoft.Toolkit.Uwp.Notifications;

namespace FacelessSiteTalker;

public class WindowsSystemManager : SystemManager
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;
        
    IntPtr consoleHandle;
    
    public ContextMenuStrip contextMenu;
    public ToolStripMenuItem miExit;
    public NotifyIcon notificationIcon;
    
    public override void SendNotification(string title, string message, string action, string id)
    {
        new ToastContentBuilder()
            .AddArgument("action", action)
            .AddArgument("id", id)
            .AddText(title)
            .AddText(message)
            .Show();
    }

    public override void ShowWindow(bool show)
    {
        if (consoleHandle == IntPtr.Zero)
        {
            consoleHandle = GetConsoleWindow();
        }
        
        ShowWindow(consoleHandle, show ? SW_SHOW : SW_HIDE);
    }

    public override void Dock()
    {
        Thread notifyThread = new Thread(
            delegate()
            {
                contextMenu = new ContextMenuStrip();
                miExit = new ToolStripMenuItem("Exit");
                contextMenu.Items.Add(miExit);

                Icon serviceIcon = FSTResources.TrayIcon;
                notificationIcon = new NotifyIcon()
                {
                    Icon = serviceIcon,
                    ContextMenuStrip = contextMenu,
                    Text = "Main"
                };
                miExit.Click += new EventHandler(MenuExitClick);

                notificationIcon.Visible = true;
                Application.Run();
            }
        );

        notifyThread.Start();
    }

    public override void ChangeIcon(string newIcon)
    {
        Object? newIconObject = FSTResources.ResourceManager.GetObject(newIcon, FSTResources.Culture);
        if (newIconObject == null)
            return;
        
        notificationIcon.Icon = newIconObject as Icon;
    }
    
    void MenuExitClick(object? sender, EventArgs e)
    {
        notificationIcon.Dispose();
        Application.Exit();
    }
}