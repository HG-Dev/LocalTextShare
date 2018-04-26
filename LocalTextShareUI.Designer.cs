namespace LocalTextShare
{
    partial class LocalTextShareUI
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
            this.testTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // testTextBox
            // 
            this.testTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.testTextBox.Font = new System.Drawing.Font("Arial Narrow", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.testTextBox.Location = new System.Drawing.Point(12, 13);
            this.testTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.testTextBox.Name = "testTextBox";
            this.testTextBox.Size = new System.Drawing.Size(839, 26);
            this.testTextBox.TabIndex = 0;
            this.testTextBox.Text = "Enter text here";
            this.testTextBox.TextChanged += new System.EventHandler(this.testTextBox_TextChanged);
            // 
            // LocalTextShareUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(863, 362);
            this.Controls.Add(this.testTextBox);
            this.Font = new System.Drawing.Font("Arial", 9F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "LocalTextShareUI";
            this.Text = "Local Text Broadcaster";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox testTextBox;
    }
}

