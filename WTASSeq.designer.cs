namespace TASSeqSrv
{
    partial class WTASSeq
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WTASSeq));
			this.tmWatchDog = new System.Windows.Forms.Timer(this.components);
			this.tmSQStep = new System.Windows.Forms.Timer(this.components);
			this.tmRdrSQ = new System.Windows.Forms.Timer(this.components);
			this.dsTASSQ = new TASSeqSrv.Data.dsTASSQ();
			this.dEVICEIO_R_VALTableAdapter = new TASSeqSrv.Data.dsTASSQTableAdapters.DEVICEIO_R_VALTableAdapter();
			this.lOADING_SQTableAdapter = new TASSeqSrv.Data.dsTASSQTableAdapters.LOADING_SQTableAdapter();
			this.cREADER_SQTableAdapter = new TASSeqSrv.Data.dsTASSQTableAdapters.CREADER_SQTableAdapter();
			this.puordersTA = new TASSeqSrv.Data.dsTASSQTableAdapters.PUORDERSTableAdapter();
			this.crdR_DRAWSQTA = new TASSeqSrv.Data.dsTASSQTableAdapters.CRDR_DRAWSQTableAdapter();
			this.vW_CARD_INFOTA = new TASSeqSrv.Data.dsTASSQTableAdapters.VW_CARD_INFOTableAdapter();
			this.mF_CARDSTA = new TASSeqSrv.Data.dsTASSQTableAdapters.MF_CARDSTableAdapter();
			((System.ComponentModel.ISupportInitialize)(this.dsTASSQ)).BeginInit();
			this.SuspendLayout();
			// 
			// tmWatchDog
			// 
			this.tmWatchDog.Interval = 1000;
			this.tmWatchDog.Tick += new System.EventHandler(this.tmWatchDog_Tick);
			// 
			// tmSQStep
			// 
			this.tmSQStep.Tick += new System.EventHandler(this.tmSQStep_Tick);
			// 
			// tmRdrSQ
			// 
			this.tmRdrSQ.Interval = 1000;
			this.tmRdrSQ.Tick += new System.EventHandler(this.tmRdrSQ_Tick);
			// 
			// dsTASSQ
			// 
			this.dsTASSQ.DataSetName = "dsTASSQ";
			this.dsTASSQ.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
			// 
			// dEVICEIO_R_VALTableAdapter
			// 
			this.dEVICEIO_R_VALTableAdapter.ClearBeforeFill = true;
			// 
			// lOADING_SQTableAdapter
			// 
			this.lOADING_SQTableAdapter.ClearBeforeFill = true;
			// 
			// cREADER_SQTableAdapter
			// 
			this.cREADER_SQTableAdapter.ClearBeforeFill = true;
			// 
			// puordersTA
			// 
			this.puordersTA.ClearBeforeFill = true;
			// 
			// crdR_DRAWSQTA
			// 
			this.crdR_DRAWSQTA.ClearBeforeFill = true;
			// 
			// vW_CARD_INFOTA
			// 
			this.vW_CARD_INFOTA.ClearBeforeFill = true;
			// 
			// mF_CARDSTA
			// 
			this.mF_CARDSTA.ClearBeforeFill = true;
			// 
			// WTASSeq
			// 
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WTASSeq";
			this.Opacity = 0.8;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "TAS Sequence";
			this.TopMost = true;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WTASSeq_FormClosing);
			this.Load += new System.EventHandler(this.WTASSeq_Load);
			((System.ComponentModel.ISupportInitialize)(this.dsTASSQ)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion

		private Data.dsTASSQ dsTASSQ;
		private Data.dsTASSQTableAdapters.DEVICEIO_R_VALTableAdapter dEVICEIO_R_VALTableAdapter;
		private Data.dsTASSQTableAdapters.LOADING_SQTableAdapter lOADING_SQTableAdapter;
        private System.Windows.Forms.Timer tmWatchDog;
		private System.Windows.Forms.Timer tmSQStep;
		private TASSeqSrv.Data.dsTASSQTableAdapters.CREADER_SQTableAdapter cREADER_SQTableAdapter;
		private System.Windows.Forms.Timer tmRdrSQ;
		private TASSeqSrv.Data.dsTASSQTableAdapters.PUORDERSTableAdapter puordersTA;
		private TASSeqSrv.Data.dsTASSQTableAdapters.CRDR_DRAWSQTableAdapter crdR_DRAWSQTA;
		private TASSeqSrv.Data.dsTASSQTableAdapters.VW_CARD_INFOTableAdapter vW_CARD_INFOTA;
		private TASSeqSrv.Data.dsTASSQTableAdapters.MF_CARDSTableAdapter mF_CARDSTA;
    }
}

