namespace ModbusRtuMaster
{
    partial class FormMain
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마십시오.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lbl7000 = new System.Windows.Forms.Label();
            this.lbl1000 = new System.Windows.Forms.Label();
            this.lbl1001 = new System.Windows.Forms.Label();
            this.lbl1002 = new System.Windows.Forms.Label();
            this.btnSet = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(64, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "0x7000 : ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(64, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "0x1000 : ";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(64, 94);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "0x1001 : ";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(64, 115);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "0x1002 : ";
            // 
            // lbl7000
            // 
            this.lbl7000.Location = new System.Drawing.Point(124, 52);
            this.lbl7000.Name = "lbl7000";
            this.lbl7000.Size = new System.Drawing.Size(49, 12);
            this.lbl7000.TabIndex = 4;
            this.lbl7000.Text = "0";
            this.lbl7000.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lbl1000
            // 
            this.lbl1000.Location = new System.Drawing.Point(124, 73);
            this.lbl1000.Name = "lbl1000";
            this.lbl1000.Size = new System.Drawing.Size(49, 12);
            this.lbl1000.TabIndex = 5;
            this.lbl1000.Text = "0";
            this.lbl1000.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lbl1001
            // 
            this.lbl1001.Location = new System.Drawing.Point(124, 94);
            this.lbl1001.Name = "lbl1001";
            this.lbl1001.Size = new System.Drawing.Size(49, 12);
            this.lbl1001.TabIndex = 6;
            this.lbl1001.Text = "0";
            this.lbl1001.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lbl1002
            // 
            this.lbl1002.Location = new System.Drawing.Point(124, 115);
            this.lbl1002.Name = "lbl1002";
            this.lbl1002.Size = new System.Drawing.Size(49, 12);
            this.lbl1002.TabIndex = 7;
            this.lbl1002.Text = "0";
            this.lbl1002.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // btnSet
            // 
            this.btnSet.Location = new System.Drawing.Point(66, 160);
            this.btnSet.Name = "btnSet";
            this.btnSet.Size = new System.Drawing.Size(107, 23);
            this.btnSet.TabIndex = 8;
            this.btnSet.Text = "0x1001 SET";
            this.btnSet.UseVisualStyleBackColor = true;
            this.btnSet.Click += new System.EventHandler(this.btnSet_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(66, 189);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(107, 23);
            this.btnClear.TabIndex = 9;
            this.btnClear.Text = "0x1001 CLEAR";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // FormMain
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(241, 262);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnSet);
            this.Controls.Add(this.lbl1002);
            this.Controls.Add(this.lbl1001);
            this.Controls.Add(this.lbl1000);
            this.Controls.Add(this.lbl7000);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormMain";
            this.Text = "MODBUS RTU MASTER";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lbl7000;
        private System.Windows.Forms.Label lbl1000;
        private System.Windows.Forms.Label lbl1001;
        private System.Windows.Forms.Label lbl1002;
        private System.Windows.Forms.Button btnSet;
        private System.Windows.Forms.Button btnClear;
    }
}

