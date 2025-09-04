using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TASQSrv
{
	public partial class frmTest : Form
	{
        private clsQueueSeq myTASSEQ;
        private string[] myargs;
        private string sDBG = string.Empty;
        private delegate void SetTBTextCallBack(TextBox TB, string TBText);

		public frmTest (string InputServiceName)
		{
			InitializeComponent();
			myTASSEQ = new clsQueueSeq(InputServiceName);
            myTASSEQ.bTestMode = true;
            myTASSEQ.bDebugMode = true;
            myTASSEQ.DebugMessageEvent += new EventHandler(myTASSEQ_DebugMessageEvent);
		}

        void myTASSEQ_DebugMessageEvent(object NewDebugMsg, EventArgs e)
        {
            sDBG = string.Format("{0:T}:{1}:{2}\r\n{3}", DateTime.Now, ((string[])NewDebugMsg)[0], ((string[])NewDebugMsg)[1], sDBG);
            this.SetTBText(this.textBox1,sDBG);
        }

        private void SetTBText(TextBox tb, string txt)
        {
            if (tb.InvokeRequired)
            {
                SetTBTextCallBack d = new SetTBTextCallBack(SetTBText);
                this.Invoke(d, new object[] { tb, txt });
            }
            else
            {
                tb.Text = txt;
            }
        }

		private void btnStart_Click ( object sender, EventArgs e )
		{
		    btnStart.Enabled = false;
			this.myTASSEQ.SvcStart(null);
		}

		private void btnStop_Click ( object sender, EventArgs e )
		{
            btnStart.Enabled = true;
			this.myTASSEQ.SvcStop();
		}

	}
}