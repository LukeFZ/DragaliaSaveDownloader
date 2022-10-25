namespace NintendoAuth.Popup
{
    partial class OAuthPopup
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OAuthPopup));
            this.printPreviewDialog1 = new System.Windows.Forms.PrintPreviewDialog();
            this.OAuthWebview = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)(this.OAuthWebview)).BeginInit();
            this.SuspendLayout();
            // 
            // printPreviewDialog1
            // 
            this.printPreviewDialog1.AutoScrollMargin = new System.Drawing.Size(0, 0);
            this.printPreviewDialog1.AutoScrollMinSize = new System.Drawing.Size(0, 0);
            this.printPreviewDialog1.ClientSize = new System.Drawing.Size(400, 300);
            this.printPreviewDialog1.Enabled = true;
            this.printPreviewDialog1.Icon = ((System.Drawing.Icon)(resources.GetObject("printPreviewDialog1.Icon")));
            this.printPreviewDialog1.Name = "printPreviewDialog1";
            this.printPreviewDialog1.Visible = false;
            // 
            // OAuthWebview
            // 
            this.OAuthWebview.AllowExternalDrop = true;
            this.OAuthWebview.CreationProperties = null;
            this.OAuthWebview.DefaultBackgroundColor = System.Drawing.Color.White;
            this.OAuthWebview.Location = new System.Drawing.Point(0, 0);
            this.OAuthWebview.Margin = new System.Windows.Forms.Padding(0);
            this.OAuthWebview.Name = "OAuthWebview";
            this.OAuthWebview.Size = new System.Drawing.Size(800, 450);
            this.OAuthWebview.TabIndex = 0;
            this.OAuthWebview.ZoomFactor = 1D;
            // 
            // OAuthPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.OAuthWebview);
            this.Name = "OAuthPopup";
            this.Text = "OAuthPopup";
            ((System.ComponentModel.ISupportInitialize)(this.OAuthWebview)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private PrintPreviewDialog printPreviewDialog1;
        private Microsoft.Web.WebView2.WinForms.WebView2 OAuthWebview;
    }
}