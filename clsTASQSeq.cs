using System;
using System.Data;
using System.Diagnostics;
using System.Timers;
using TASQSrv.Data;
using TASQSrv.Utilities;
using TASQSrv.Data.dsQueueTableAdapters;
using TASQSrv;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
namespace TASQSrv
{
    internal class clsQueueSeq
	{
        internal const string QS_Queued = "QUEUED";
        internal const string QS_Calling = "CALLING";
        internal const string QS_DryRun = "DRYRUN";
        internal const string QS_Ready = "READY";
        internal const string QS_Loading = "LOADING";
        internal const string QS_Loaded = "LOADED";
        internal const string QS_Finish = "FINISH";
        internal const string QS_Cancel = "CANCEL";
        internal const string BS_Empty = "EMPTY";
        internal const string BS_Calling = "CALLING";
        internal const string BS_DryRun = "DRYRUN";
        internal const string BS_Ready = "READY";
        internal const string BS_Loading = "LOADING";
        internal const string BS_Loaded = "LOADED";
        internal const string ION_FL01 = "FL01";
        internal const string ION_WGH = "DT04";
        internal const string ION_Enabled = "ENABLED";
        internal const string ION_STEP = "ET01";
        internal const string ION_QNTY = "DT09";
        internal const int MAX_PROG = 499;

		#region variable declaration
		internal System.Data.OracleClient.OracleConnection oraconn;
		internal string sServiceName = "ServiceName";
		internal string sLog = string.Empty;
		internal string sLastLog = string.Empty;
        internal string sLastError = string.Empty;
		internal bool bDataConn = false;
        internal bool bTestMode = false;
        internal bool bDebugMode = true;
		internal int iWCnt = 0;
		internal int iSeqHB = 0;
		internal bool bIOBusy = false;
        internal string[] LASTDBGMSG= { "", "", "", "", "" };
        internal int iQCnt = 0;
        //private static string CrLf = "\r\n";
		internal const int DefTmSQ = 500;
		#endregion

		private Timer tmSQStep;
        private Timer tmWDG;
        private dsQueue DSQueue;
        private QueueTableAdapter QueueTA;
        private DEVICEIOTableAdapter DEVICEIOTA;
        private T_QUEUETableAdapter T_QUEUETA;
        private T_METER_QTableAdapter T_METER_QTA;
        private PerformanceCounter cpuCounter;
        private int iLastBay;
        private float fCPULoad;
        private TASSettings tasset;

        private void InitializeComponent ( )
		{
			this.tmSQStep = new Timer();
            this.tmWDG = new Timer();
            this.DSQueue = new dsQueue();
            this.QueueTA = new QueueTableAdapter();
            this.DEVICEIOTA = new DEVICEIOTableAdapter();
            this.T_QUEUETA = new T_QUEUETableAdapter();
            this.T_METER_QTA = new T_METER_QTableAdapter();
            this.cpuCounter = new PerformanceCounter();
            // 
            // tm...
            // 
            this.tmSQStep.Elapsed += new ElapsedEventHandler(this.tmSQStep_Tick);
            this.tmWDG.Elapsed += new ElapsedEventHandler(this.tmWDG_Tick);
            // 
            this.DSQueue.DataSetName = "DSQueue";
            this.DSQueue.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
			// 
			// ...TA
			// 
			this.DEVICEIOTA.ClearBeforeFill = true;
            this.T_QUEUETA.ClearBeforeFill = true;
            this.T_METER_QTA.ClearBeforeFill = true;
            //
            // Performace Counter
            //
            this.cpuCounter.CategoryName = "Process";
            this.cpuCounter.CounterName = "% Processor Time";
            this.cpuCounter.InstanceName = this.sServiceName;
        }

		internal clsQueueSeq (string InputServiceName)
		{
			InitializeComponent();
            sServiceName = InputServiceName;
			this.tmSQStep.Interval = DefTmSQ;
			this.tasset = new TASSettings(sServiceName);
		}
        internal event EventHandler DebugMessageEvent;

		internal void SvcStart ( string[] args )
		{
            this.oraconn = this.T_METER_QTA.Connection;
			this.oraconn.StateChange += new StateChangeEventHandler(oraconn_StateChange);
			this.bDataConn = (misc.ConnErr(this.oraconn) == string.Empty);
			//GetSettings();
			DebugText("Application Started");
			try
			{
				this.DEVICEIOTA.Fill(this.DSQueue.DEVICEIO);
                this.T_QUEUETA.Fill(this.DSQueue.T_QUEUE);
                this.T_METER_QTA.Fill(this.DSQueue.T_METER_Q);
                this.bDataConn = true;
			}
			catch(Exception ex)
			{
				DebugText(ex.ToString());
			}

			DebugText("found " + this.DSQueue.DEVICEIO.Count.ToString() + " IO/read tags.");
            DebugText("found " + this.DSQueue.T_METER_Q.Count.ToString() + " queue seq.");
            DebugText("found " + this.DSQueue.T_QUEUE.Count.ToString() + " active queue.");
            iQCnt = this.DSQueue.T_QUEUE.Count;
			this.tmSQStep.Enabled = true;
		}

		internal void SvcStop ( )
		{
			DebugText("Application exit");
            this.tmSQStep.Enabled = false;
            this.tmWDG.Enabled = false;
            this.oraconn.Close();
		}

		void oraconn_StateChange ( object sender, StateChangeEventArgs e )
		{
			this.bDataConn = (e.CurrentState == ConnectionState.Open);
			DebugText("DB Connection changed to :" + e.CurrentState.ToString());
			if(this.bDataConn)
			{
				this.tasset.SetStatus(this.sServiceName + ".ConnSince", DateTime.Now.ToLongTimeString());
                this.tmWDG.Enabled = true;
			}
		}


		private void GetSettings ( )
		{
			//this.ErrorShow = int.Parse(tasset.GetSet("ERRORSHOW", "6"));
			//this.MinTagLenght = int.Parse(tasset.GetSet("MINTAGIDLENGHT", "4"));
			//this.AfterBatchDelay = int.Parse(tasset.GetSet("AFTERBATCHDELAY", "10"));
		}
		#region Timers
		private void tmSQStep_Tick ( object sender, ElapsedEventArgs e )
		{
			if(this.tmSQStep.Interval == DefTmSQ)
			{
				DebugText("Queue seq. timer started");
				this.tmSQStep.Interval = Properties.Settings.Default.tmSeqStep;
			}
			this.tmSQStep.Enabled = false;
			
			if(this.bDataConn)
			{
				try
				{
					this.DEVICEIOTA.Fill(this.DSQueue.DEVICEIO);
				}
				catch(Exception e1)
				{
					DebugErrText("FillDevIO:" + e1.ToString());
				}
				// Queue seq.
                try
                {
                    this.T_METER_QTA.Fill(this.DSQueue.T_METER_Q);
                }
                catch (Exception e1)
                {
                    DebugErrText("FillMeterQ:" + e1.ToString());
                }
                try
                {
                    this.T_QUEUETA.Fill(this.DSQueue.T_QUEUE);
                }
                catch (Exception e1)
                {
                    DebugErrText("FillQueue1:" + e1.ToString());
                }
                if(this.DSQueue.T_QUEUE.Count!=iQCnt)
                {
                    DebugText("found " + this.DSQueue.T_QUEUE.Count.ToString() + " active queue.");
                    iQCnt = this.DSQueue.T_QUEUE.Count;
                }
                try
				{
					foreach(dsQueue.T_METER_QRow drw in this.DSQueue.T_METER_Q.Rows)
					{
						if((drw.METER_ID > 0))
							StepSequent(drw);
					}
				}
				catch(Exception e1)
				{
					DebugErrText("CalcMeterQ:" + e1.ToString());
				}
				try
				{
                    if (this.DSQueue.T_METER_Q.GetChanges() != null) this.T_METER_QTA.Update(this.DSQueue.T_METER_Q);
				}
				catch(Exception e1)
				{
					DebugErrText("UpdMeterQ:" + e1.ToString());
				}
                try
                {
                    if (this.DSQueue.T_QUEUE.GetChanges() != null) this.T_QUEUETA.Update(this.DSQueue.T_QUEUE);
                }
                catch (Exception e1)
                {
                    DebugErrText("UpdQueue1:" + e1.ToString());
                }
                try
                { 
                    WaitTMCalc();
                }
                catch (Exception e1)
                {
                    DebugErrText("CalcWaitTM:" + e1.ToString());
                }
                try
                {
                    if (this.DSQueue.T_QUEUE.GetChanges() != null) this.T_QUEUETA.Update(this.DSQueue.T_QUEUE);
                }
                catch (Exception e1)
                {
                    DebugErrText("UpdQueue2:" + e1.ToString());
                }
            }
			this.tmSQStep.Enabled = true;
			iSeqHB = (iSeqHB >= 9999) ? 1 : iSeqHB + 1;
		}
        private void tmWDG_Tick ( object sender, ElapsedEventArgs e )
        {
            switch (iWCnt)
            {
                case 0:
                    this.tmWDG.Interval = (int)Properties.Settings.Default.tmWdog;
                    DebugText("Watch Dog timer started");
                    break;
                case 4:
                case 8:
                    this.bDataConn = (misc.ConnErr(this.oraconn) == string.Empty);
                    break;
                case 12:
                    this.tasset.SetStatus(this.sServiceName.Substring(0,5) + ".TimeStamp", DateTime.Now.ToString());
                    break;
                case 1:
                case 5:
                case 9:
                    this.fCPULoad = this.cpuUsage();
                    break;
                case 2:
                case 6:
                case 10:
                    float ftmp = this.cpuUsage();
                    this.fCPULoad = (ftmp + this.fCPULoad) / 2;
                    if (this.fCPULoad > 5)
                    {
                        this.DebugErrText(string.Format($"TASQu Service CPU Usage = {this.fCPULoad.ToString()}"));
                    }
                    break;
            }
            iWCnt = (iWCnt >= 12) ? 1 : iWCnt + 1;
        }
        private float cpuUsage()
        {
            if (this.bTestMode) return 1;
            try
            {
                return this.cpuCounter.NextValue();
            }
            catch (Exception e)
            {
                this.DebugErrText(e.ToString());
                return (float)-1.0;
            }
        }
        #endregion Timers
        #region Debug text
        internal void DebugText ( string txtLog )
		{
			DebugText(txtLog, 0);
		}
        internal void DebugText ( string txtLog, int iBay )
		{
			if(!(txtLog == sLastLog && iLastBay==iBay))
			{
                string sCat=(iBay == 0) ? this.sServiceName : ((iBay <=4) ? "BAY" : "DEV") + iBay.ToString();
				try
				{
                    if (bDataConn) this.QueueTA.InsertLog(sCat, txtLog.PadRight(150).Trim());
				}
				catch
				{ }
                if(bDebugMode) DebugMessageEvent((new string[] { sCat, txtLog }), new EventArgs());
				sLastLog = txtLog;
                iLastBay = iBay;
			}
		}
		internal void DebugErrText ( string txtErr )
		{
            if (txtErr != sLastError)
            {
                try
                {
                    this.DebugText("!! " + txtErr);
                    EventLog.WriteEntry(sServiceName, txtErr, EventLogEntryType.Error); 
                    sLastError = txtErr;
                }
                catch (Exception e)
                {
                    this.DebugText("!! " + e.ToString());
                }
            }
		}
		#endregion //Debug text
		internal void StepSequent ( dsQueue.T_METER_QRow bayq )
		{

			int iBayID = (int)bayq.METER_ID;
			bool bBAuto = (bayq.Q_AUTO == "y"); //true if queue seq. is enable
            bool bBDryRun = (bayq.DRYRUN == "y");
            bool bBMaint = (bayq.MAINTENANCE == "y");
            int SQSTEP = int.Parse(ReadIO(bayq, ION_STEP)) % 100;
            bool iomaint = int.Parse(ReadIO(bayq, ION_Enabled)) == 0;// maintenance=(enable==0)
            if (iomaint != bBMaint)
                bayq.MAINTENANCE = (iomaint) ? "y" : "n";
            decimal WGH = decimal.Parse(ReadIO(bayq, ION_WGH));
            string MSG1 = string.Empty;
            string MSG2 = string.Empty;
			string DEBUG_MSG = string.Empty;
            decimal QNTY;
            if (!bBMaint)
            {
                dsQueue.T_QUEUERow qrow=null;
                switch (bayq.STATUS)
                {
// *** Empty Status ***
                    case BS_Empty:
                        if (bBAuto)// Auto Assign
                        {
                            if (bayq.Q_ID == 0)
                            {
                                if (bayq.SQ_NUMBER != 1)
                                {
                                    bayq.SQ_NUMBER = 1;
                                    bayq.SQ_TIMER = 0;
                                    bayq.DRYRUN = "n";
                                    bayq.PROG_Q1 = 0;
                                    bayq.PROG_Q2 = 0;
                                    bayq.PROG_T1 = 0;
                                    bayq.PROG_T2 = 0;
                                    DEBUG_MSG = "Empty Timer Started";
                                }// init timer
                                else
                                    bayq.SQ_TIMER = bayq.SQ_TIMER + 1;

                                if (bayq.SQ_TIMER > bayq.SET_AUTO_DELAY && bayq.SQ_NUMBER == 1)
                                {
                                    qrow = null;
                                    foreach(dsQueue.T_QUEUERow row in DSQueue.T_QUEUE.Rows)
                                    {
                                        if (row.Q_STATUS == "QUEUED")
                                        { qrow = row; break; }
                                    }

                                    if (qrow != null)
                                    {
                                        bayq.Q_ID = qrow.Q_ID;
                                        bayq.STATUS = BS_Calling;
                                        bayq.DRYRUN = qrow.DRYRUN;
                                        DEBUG_MSG = "Auto-Assign Event";
                                        qrow.METER_ID = iBayID;
                                        qrow.Q_STATUS = QS_Calling;
                                        qrow.WAIT_TM = 0;
                                    }
                                }// timer>10
                            }

                        }// Auto Assign
                        else
                            DEBUG_MSG = "Bay in Manual Queue Mode";
                        break;
// *** Calling Status ***
                    case BS_Calling:
                        if (WGH > bayq.SET_START_WEIGHT)
                        {
                            if (bayq.SQ_NUMBER != 4)
                            {
                                bayq.SQ_NUMBER = 4;
                                bayq.SQ_TIMER = 0;
                                DEBUG_MSG = "Ready Timer Started";
                            }// init timer
                            else
                                bayq.SQ_TIMER = bayq.SQ_TIMER + 1;

                            if (bayq.SQ_TIMER > 5 && bayq.SQ_NUMBER == 4)
                            {
                                bayq.STATUS = (bBDryRun) ? BS_DryRun : BS_Ready;
                                bayq.SQ_TARE = 0;
                                DEBUG_MSG = "Ready/DryRun Status";
                                qrow = DSQueue.T_QUEUE.FindByQ_ID(bayq.Q_ID);
                                if (qrow != null)
                                {
                                    bayq.DRYRUN = qrow.DRYRUN;
                                    bayq.STATUS = (qrow.DRYRUN=="y") ? BS_DryRun : BS_Ready;
                                    qrow.Q_STATUS = bayq.STATUS;
                                    qrow.TS_PARK = DateTime.Now;
                                    qrow.WAIT_TM = 0;
                                    DEBUG_MSG = "Parking Event";
                                }
                            }
                        }
                        else
                            bayq.SQ_TIMER = 0;
                        break;
// *** Ready Status ***
                    case BS_Ready:
                        QNTY = decimal.Parse(ReadIO(bayq, ION_QNTY));
                        string FL01 = ReadIO(bayq, "FL01");
                        // Loading
                        if (SQSTEP==3)
                        {
                            bayq.STATUS = BS_Loading;
                            bayq.SQ_START = DateTime.Now;
                            DEBUG_MSG = "(Loading)Batch Start Event";
                            qrow = DSQueue.T_QUEUE.FindByQ_ID(bayq.Q_ID);
                            if (qrow != null)
                            {
                                if (QNTY > 1000 && bayq.PROG_Q2 == 0)
                                {
                                    float qnty = (float)QNTY;
                                    float rate = (float)bayq.SET_FLOW_RATE;
                                    int loadtime = (int)(qnty / rate) + 1;
                                    bayq.PROG_Q1 = 0;
                                    bayq.PROG_Q2 = QNTY;
                                    bayq.PROG_T1 = 0;
                                    bayq.PROG_T2 = (decimal)loadtime;
                                }
                                if (bayq.SQ_TARE == 0)
                                {
                                    bayq.SQ_TARE = WGH;
                                }
                                qrow.Q_STATUS = QS_Loading;
                            }
                        }
                        // Card In
                        else if (SQSTEP==1 && FL01=="1")
                        {
                            qrow = DSQueue.T_QUEUE.FindByQ_ID(bayq.Q_ID);
                            if (qrow != null)
                            {
                                if (QNTY > 1000 && bayq.PROG_Q2==0)
                                {
                                    float qnty = (float)QNTY;
                                    float rate = (float)bayq.SET_FLOW_RATE;
                                    int loadtime = (int)(qnty / rate) + 1;
                                    bayq.PROG_Q1 = 0;
                                    bayq.PROG_Q2 = QNTY;
                                    bayq.PROG_T1 = 0;
                                    bayq.PROG_T2 = (decimal)loadtime;
                                }
                                if (bayq.SQ_TARE == 0)
                                {
                                    bayq.SQ_TARE = WGH;
                                }

                            }
                            DEBUG_MSG = "Card In Event";
                        }
                        // Verify
                        else if (SQSTEP == 2)
                        {
                            qrow = DSQueue.T_QUEUE.FindByQ_ID(bayq.Q_ID);
                            if (qrow != null)
                            {
                                if (QNTY > 1000 && bayq.PROG_Q2==0)
                                {
                                    float qnty = (float)QNTY;
                                    float rate = (float)bayq.SET_FLOW_RATE;
                                    int loadtime = (int)(qnty / rate) + 1;
                                    bayq.PROG_Q1 = 0;
                                    bayq.PROG_Q2 = QNTY;
                                    bayq.PROG_T1 = 0;
                                    bayq.PROG_T2 = (decimal)loadtime;
                                }
                                if (bayq.SQ_TARE == 0)
                                {
                                    bayq.SQ_TARE = WGH;
                                }
                            }
                            DEBUG_MSG = "Verify Event";
                        }
                        // Cancel
                        else if (SQSTEP==7)
                        {
                            bayq.STATUS = BS_Loaded;
                            DEBUG_MSG = "(Canceled)Cancel Event";
                            qrow = DSQueue.T_QUEUE.FindByQ_ID(bayq.Q_ID);
                            if (qrow != null)
                            {
                                qrow.Q_STATUS = QS_Loaded;
                            }
                        }
                        break;
// *** Loading Status ***
                    case BS_Loading:
                        // Gross Event
                        if (SQSTEP==5)
                        {
                            bayq.STATUS = BS_Loaded;
                            DEBUG_MSG = "(Loaded)Gross Weigh Event";
                            qrow = DSQueue.T_QUEUE.FindByQ_ID(bayq.Q_ID);
                            if (qrow != null)
                            {
                                qrow.Q_STATUS = QS_Loaded;
                            }
                        }
                        // Cancel Event
                        else if (SQSTEP==7)
                        {
                            bayq.STATUS = BS_Loaded;
                            DEBUG_MSG = "(Cancled)Cancel Event";
                            qrow = DSQueue.T_QUEUE.FindByQ_ID(bayq.Q_ID);
                            if (qrow != null)
                            {
                                qrow.Q_STATUS = QS_Loaded;
                            }
                        }
                        // Batch End Event
                        else if (SQSTEP==4)
                        {
                            DEBUG_MSG = "(Loading)Batch End Event";
                        }
                        // Loading with zero weight
                        else if (WGH < bayq.SET_FINISH_WEIGHT)
                        {
                            if (bayq.SQ_NUMBER != 5)
                            {
                                bayq.SQ_NUMBER = 5;
                                bayq.SQ_TIMER = 0;
                                DEBUG_MSG = "No weight timer started";
                            }// init timer
                            else
                                bayq.SQ_TIMER = bayq.SQ_TIMER + 1;
                            if (bayq.SQ_TIMER > 120 && bayq.SQ_NUMBER == 5)
                            {
                                DEBUG_MSG = "Loading without weight";
                                bayq.SQ_TIMER = 0;
                            }
                        }
                        // Loading + no event
                        else
                        {
                            QNTY = decimal.Parse(ReadIO(bayq, ION_QNTY));
                            if (QNTY > 1000 && bayq.PROG_Q2 == 0)
                            {
                                bayq.PROG_Q2 = QNTY;
                            }
                            bayq.PROG_Q1 = WGH - bayq.SQ_TARE;
                            int t1 = (int)(DateTime.Now.Subtract(bayq.SQ_START)).TotalMinutes;
                            if (t1 > 2)
                            {
                                float rate = (float)bayq.PROG_Q1 / t1;
                                t1= (t1 > MAX_PROG) ? MAX_PROG : t1;
                                if (rate < 50) rate = 50;
                                int t2 = t1 + (int)(((float)(bayq.PROG_Q2 - bayq.PROG_Q1)) / rate);
                                t2 = (t2 < t1) ? t1 : t2;
                                bayq.PROG_T2 = (t2 > MAX_PROG) ? MAX_PROG : t2 ;
                            }
                            bayq.PROG_T1 = t1 ;
                            if(bayq.PROG_Q1>100)
                                if (bayq.SQ_NUMBER != 3)
                                // Batch count
                                {
                                    bayq.CNT = bayq.CNT + 1;
                                    bayq.SQ_NUMBER = 3;
                                    DEBUG_MSG = "Loading count";
                                }
                        }
                        break;
// *** Loaded Status ***
                    case BS_Loaded:
                        if (WGH < bayq.SET_FINISH_WEIGHT)
                        {
                            if (bayq.SQ_NUMBER != 2)
                            {
                                bayq.SQ_NUMBER = 2;
                                bayq.SQ_TIMER = 0;
                                DEBUG_MSG = "Finish Timer Started";
                            }// init timer
                            else
                                bayq.SQ_TIMER = bayq.SQ_TIMER + 1;

                            if (bayq.SQ_TIMER > 5 && bayq.SQ_NUMBER == 2)
                            {
                                bayq.PROG_Q1 = 0;
                                bayq.PROG_Q2 = 0;
                                bayq.PROG_T1 = 0;
                                bayq.PROG_T2 = 0;
                                bayq.STATUS = BS_Empty;
                                bayq.P_Q_ID = bayq.Q_ID;
                                DEBUG_MSG = "Empty Status";
                                qrow = DSQueue.T_QUEUE.FindByQ_ID(bayq.Q_ID);
                                if (qrow != null)
                                {
                                    qrow.Q_STATUS = QS_Finish;
                                    qrow.TS_EXIT = DateTime.Now;
                                }
                                bayq.Q_ID = 0;
                                bayq.DRYRUN = "n";
                            }
                        }
                        else
                            bayq.SQ_TIMER = 0;
                        break;
                }// Switch B Status
            }// Maintenance
            else
                DEBUG_MSG = "Bay in Maintenance Mode";

            if (bayq.SQ_TIMER > 9999) bayq.SQ_TIMER = 999;

            if (!DEBUG_MSG.Equals(string.Empty))
                if (DEBUG_MSG != this.LASTDBGMSG[iBayID])
                {
                    DebugText(DEBUG_MSG, iBayID);
                    bayq.DEBUG_MSG = DEBUG_MSG;
                    this.LASTDBGMSG[iBayID]=DEBUG_MSG;
                }

		}
        internal void WaitTMCalc()
        {
            int[] iwait = new int[4];
            int usablebay = 4;
            foreach (dsQueue.T_METER_QRow b in DSQueue.T_METER_Q.Rows)
            {
                int imeterid = (int)b.METER_ID;
                if (imeterid>0)
                {
                    if (b.MAINTENANCE == "y")
                    {
                        iwait[imeterid - 1] = 99;
                        usablebay--;
                    }
                    else if( b.DRYRUN == "y")
                        iwait[imeterid - 1] = 20;
                    else if ((b.PROG_Q2 > 0) && (b.PROG_Q1 >= (b.PROG_Q2 - 200)) || b.STATUS == BS_Loaded)
                        iwait[imeterid - 1] = 5;
                    else if (b.PROG_Q2 > 5000)
                        iwait[imeterid - 1] = 5 + (int)b.PROG_T2 - (int)b.PROG_T1;
                    else if (b.STATUS == BS_Ready)
                        iwait[imeterid - 1] = 55;
                    else if (b.STATUS == BS_Calling)
                        iwait[imeterid - 1] = 60;
                    else if (b.STATUS == BS_Empty)
                        iwait[imeterid - 1] = (b.Q_AUTO == "y") ? 0 : 60;
                    if (iwait[imeterid - 1] < 5)
                        iwait[imeterid - 1] = 5;
                }
            }
            Array.Sort(iwait);
            int i = 0;
            int iwt=0;
            usablebay = (usablebay == 0) ? 1 : usablebay;
            foreach (dsQueue.T_QUEUERow q in DSQueue.T_QUEUE.Rows)
            {
                if(q.Q_STATUS==QS_Queued)
                { 
                    iwt = iwait[(i % usablebay)] + 60 * (int)(i/usablebay);
                    if(q.WAIT_TM!=iwt)q.WAIT_TM = iwt;
                    i++;
                }
            }
        }
        #region Read/WriteIO
        internal string ReadIO ( Data.dsQueue.T_METER_QRow bay, string sIOName )
		{
            return ReadIO(bay.DEVICEID, sIOName);
		}
		internal string ReadIO ( decimal iDevID, string sIOName )
		{
			dsQueue.DEVICEIORow drowfnd = this.DSQueue.DEVICEIO.FindByDEVICEIDIONAME(iDevID, sIOName);
            
			if(drowfnd != null)
			{
				return drowfnd.IOVALUE;
			}
			else
			{
				DebugText("READIO():Fail to find IO:" + sIOName + "@DEV:" + iDevID.ToString());
				return string.Empty;
			}
		}
        //internal bool WriteIO(Data.dsQueue.T_METER_QRow bay, string sIOName, string sIOValue)
        //{
        //    return WriteIO(bay.DEVICEID, sIOName, sIOValue);
        //}
        //internal bool WriteIO ( decimal iDevID, string sIOName, string sIOValue )
        //{
        //    bool bReturn = false;
        //    try
        //    {
        //        this.DEVICEIOTA.UpdateIO(sIOValue, 0, iDevID, sIOName);
        //        bReturn = true;
        //    }
        //    catch
        //    {
        //        DebugText("WRITEIO():Failed w/ IO:" + sIOName + "@DEV:" + iDevID.ToString());
        //    }
        //    return bReturn;
        //}
		#endregion //Read/WriteIO
	}
}
