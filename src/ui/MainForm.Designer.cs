namespace PhpVersionSwitcher
{
	internal partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.notifyIconMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.SuspendLayout();
			// 
			// notifyIcon
			// 
			this.notifyIcon.ContextMenuStrip = this.notifyIconMenu;
			this.notifyIcon.Icon = global::PhpVersionSwitcher.Properties.Resources.Globe;
			this.notifyIcon.Text = "PHP Version Switcher";
			this.notifyIcon.Visible = true;
			this.notifyIcon.MouseUp += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseUp);
			// 
			// notifyIconMenu
			// 
			this.notifyIconMenu.Name = "notifyIconMenu";
			this.notifyIconMenu.Size = new System.Drawing.Size(61, 4);
			// 
			// MainForm
			// 
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Name = "MainForm";
			this.Text = "PHP Version Switcher";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.NotifyIcon notifyIcon;
		private System.Windows.Forms.ContextMenuStrip notifyIconMenu;

	}
}

