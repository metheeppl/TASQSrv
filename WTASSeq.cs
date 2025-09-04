using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TASSeqSrv.Data ;
using TASSeqSrv.Utilities;
using System.IO;
using TASCommon;

namespace TASSeqSrv
{
    public partial class WTASSeq : Form
    {
#region variable declaration    
        internal enum DevType {term=1,meter,reader};
		internal enum IOTagT { TERMCOMMAND = 1, TERMCMDPARAM, OPERATINGMODE, TERMLAYER, TERMITEMID, TERMHANDLE};
		internal enum IOTagR { TAGID = 1 };
		internal enum IOTagC { REMAINBATCH = 1, LASTSTOP, ACTUALPERM, ERRORCODE, STATE, COMMAND, BATCHQNTY, TRANXCTNQNTY, REMAINTRANXCTN, TEMPERATURE, LOADED, FLOWCOUNTER2, AVGTEMP, ONLINE, FLOWRATE1};
        internal string sLog = string.Empty;
		internal string sLastLog = string.Empty;
		internal bool bDataConn = false;
		internal int iWCnt=0;
		internal dsTASSQ.CRDR_DRAWSQRow dsq;
		internal TSSettings tasset;
		internal int iOrdShow, iNoDatShow;
#endregion
        public WTASSeq()
        {
            InitializeComponent();
			tasset = new TSSettings("TASSEQ");
			// Read timer setting
			this.iOrdShow = int.Parse(tasset.GetSet("INFOREADER.ORDERSHOW", "40"));
			this.iNoDatShow = int.Parse(tasset.GetSet("INFOREADER.NODATASHOW", "10"));
			this.tmWatchDog.Interval = 100;
			this.tmSQStep.Interval = 250;
			this.tmRdrSQ.Interval = 400;
        }
        private void WTASSeq_Load(object sender, EventArgs e)
        {
			this.dEVICEIO_R_VALTableAdapter.Fill(this.dsTASSQ.DEVICEIO_R_VAL);
			DebugText("Application Started");
			DebugText("found " + this.dsTASSQ.DEVICEIO_R_VAL.Count.ToString() + " IO/read tags");
			this.lOADING_SQTableAdapter.Fill(this.dsTASSQ.LOADING_SQ);
			DebugText("found " + this.dsTASSQ.LOADING_SQ.Count.ToString() + " loading seq.");
			this.cREADER_SQTableAdapter.Fill(this.dsTASSQ.CREADER_SQ);
			DebugText("found " + this.dsTASSQ.CREADER_SQ.Count.ToString() + " card reader seq.");
			this.tmWatchDog.Enabled = true;
			this.tmSQStep.Enabled = true;
			this.tmRdrSQ.Enabled = true;
		}
		private void WTASSeq_FormClosing ( object sender, FormClosingEventArgs e )
		{
			this.DebugText("Application exit");
			//if(this.bDataConn)
			//{
			//    string sPswd = misc.InputBox("Input password for "+this.Text+" closing", this.Text, "", '*');
			//    e.Cancel = !(sPswd.ToUpper().Trim() == "TASTAS");
			//}
		}
#region Timers
		private void tmWatchDog_Tick ( object sender, EventArgs e )
		{
			switch(iWCnt)
			{
			case 0:
				this.tmWatchDog.Interval = (int)Properties.Settings.Default.tmWdog;
				this.DebugText("Watchdog timer started");
				break;
			case 1:
			case 6:
			case 2:
			case 4:
				break;
			case 8:
				this.iOrdShow = int.Parse(tasset.GetSet("INFOREADER.ORDERSHOW", this.iOrdShow.ToString()));
				this.iNoDatShow = int.Parse(tasset.GetSet("INFOREADER.NODATASHOW", this.iNoDatShow.ToString()));
				break;
			case 10:
				this.bDataConn = (misc.ConnErr() == string.Empty);
				break;
			}
			iWCnt = (iWCnt >= 11) ? 1 : iWCnt + 1;
		}
		private void tmSQStep_Tick ( object sender, EventArgs e )
		{
			int iSeqTm = (int)Properties.Settings.Default.tmSeqStep;
			if(this.tmSQStep.Interval != iSeqTm)
			{
				this.DebugText("Loading seq. timer started");
				this.tmSQStep.Interval = iSeqTm;
			}
			if(this.bDataConn)
			{
				this.dEVICEIO_R_VALTableAdapter.Fill(this.dsTASSQ.DEVICEIO_R_VAL);
				this.lOADING_SQTableAdapter.Fill(this.dsTASSQ.LOADING_SQ);
				foreach(DataRow drow in this.dsTASSQ.LOADING_SQ.Rows)
				{
					dsTASSQ.LOADING_SQRow drw = (dsTASSQ.LOADING_SQRow)drow;
					if(drw.ENABLED == 0) { drw.MSG = "Bay is in manual mode"; }
					if((drw.METER_DEVID > 0) && (drw.TERM_DEVID > 0) && (drw.RDR_DEVID > 0))
						StepSequent(drw);
				}
				try
				{
					if(this.dsTASSQ.LOADING_SQ.GetChanges() != null) this.lOADING_SQTableAdapter.Update(this.dsTASSQ.LOADING_SQ);
				}
				catch(Exception e1)
				{
					DebugText("UpdLoadSQ:" + e1.Message);
				}
			}
		}
		private void tmRdrSQ_Tick ( object sender, EventArgs e )
		{
			int iRdrTm = (int)Properties.Settings.Default.tmRdrSQ;
			if(this.tmRdrSQ.Interval != iRdrTm)
			{
				this.DebugText("Reader seq. timer started");
				this.tmRdrSQ.Interval = iRdrTm;
			}
			if(this.bDataConn)
			{
				if(this.dsTASSQ.CREADER_SQ.GetChanges() != null) this.cREADER_SQTableAdapter.Update(this.dsTASSQ.CREADER_SQ);
				this.cREADER_SQTableAdapter.Fill(this.dsTASSQ.CREADER_SQ);
				foreach(DataRow drow in this.dsTASSQ.CREADER_SQ.Rows)
				{
					RDR_StepSequent((Data.dsTASSQ.CREADER_SQRow)drow);
				}
				try
				{
					if(this.dsTASSQ.CREADER_SQ.GetChanges() != null) this.cREADER_SQTableAdapter.Update(this.dsTASSQ.CREADER_SQ);
				}
				catch(Exception e1)
				{
					DebugText("UpdCardSQ:" + e1.Message);
				}
			}
		}
#endregion Timers
#region Debug text
		internal void DebugText(string txtErr)
        {
            DebugText(txtErr, false, 0);
        }
		internal void DebugText (string txtErr, int iBay )
		{
			DebugText(txtErr, false, iBay);
		}
        internal void DebugText(string txtErr,bool bAlert,int iBay)
        {
			string stxtErr = (iBay == 0) ? txtErr : ((iBay > 7)?"RDR":"BAY") + iBay.ToString() + " " + txtErr;
			if(stxtErr!=sLastLog)
			{
				//if (bAlert) MessageBox.Show(stxtErr);
				//sLog = sLog + DateTime.Now.ToLongTimeString() + " " + stxtErr + "\r\n";
				if (bDataConn) this.lOADING_SQTableAdapter.InsertLog(stxtErr.PadRight(150).Trim());
				sLastLog=stxtErr;			
			}
			else
			{
				//sLog = sLog + ".";

			}			
        }
#endregion //Debug text
		internal void StepSequent ( dsTASSQ.LOADING_SQRow drow )
		{
			//bool bOnline = bool.Parse(ReadIO(drow, IOTagC.ONLINE));
			//if(bOnline)
			//{
				Data.dsTASSQ.CREADER_SQRow crdr = this.dsTASSQ.CREADER_SQ.FindByRDRID(drow.RDR_DEVID);
				string stmp = string.Empty;
				bool bSync = true;
				bool bAuto = (drow.ENABLED != 0); //true if seq. is enable
				bool bOpMode = (ReadIO(drow, IOTagT.OPERATINGMODE) == "0"); //true if term is auto mode
				string MSG = drow.MSG;
				int CURSTEP = (int)drow.SQ_STEP;
				int NEXTSTEP = CURSTEP;
				int iBayID = (int)drow.BAYID;
				int iBCState = int.Parse(ReadIO(drow, IOTagC.STATE));
				int iBCCmd = int.Parse(ReadIO(drow, IOTagC.COMMAND));
				int iBCLastStop = int.Parse(ReadIO(drow, IOTagC.LASTSTOP));
				stmp = ReadIO(drow, IOTagC.REMAINBATCH);
				Decimal decBCRem = misc.DecParse(ReadIO(drow, IOTagC.REMAINBATCH),0,30000);
				string sBCSts=string.Empty;
				#region SwitchCURSTEP
				switch(CURSTEP)
				{
#region Loading with card
				case 100:
					MSG = "Start loading process";
					drow.SQ_RDRREQ = 0;
					crdr.SQ_STEP = 100;
					crdr.WAIT_TAGID = drow.CARD_TAGID;
					crdr.TAGID = string.Empty;
					crdr.SQ_RESULT = 0;
					crdr.MSG3 = drow.ORDER_QNTY.ToString();
					crdr.MSG2 = drow.LICPLATE;
					crdr.MSG1 = this.mF_CARDSTA.GetLABEL(drow.CARD_TAGID.Trim());
					if(iBCState != 3) WriteIO(drow, IOTagC.COMMAND, "2");
					NEXTSTEP = 110;
					break;
				case 110:
					// wait for tagid from reader @bay and match for loading privilege
					MSG = "Wait for card TAGID";
					if(crdr.SQ_RESULT != 0)
					{
						if(crdr.SQ_RESULT > 0 && crdr.LABEL != "NO CARD" && crdr.LABEL != "NO MATCH")
						{
							// Clear card tagid at reader.
							drow.TM_CHECKIN = DateTime.Now;
							drow.SQ_RDRREQ = crdr.SQ_RESULT;
							// do something after tag matched
							if(iBCState != 3) WriteIO(drow, IOTagC.COMMAND, "2");
							NEXTSTEP = 120;
							MSG = "Matched with CARD# " + crdr.LABEL;
						}
						else
						{

							MSG = "Wrong card with CARD# " + crdr.LABEL;
							crdr.SQ_RESULT = 0;
							//NEXTSTEP = 112;
							drow.ALERT = 1;
						}
					}
					else if(crdr.SQ_STEP == 0) crdr.SQ_STEP = 100;
					break;
				case 120:
					MSG = "Wait for BC STATE=Idle(3)";
					if(iBCState == 3)
					{
						WriteIO(drow, IOTagC.TRANXCTNQNTY, drow.ORDER_QNTY.ToString());
						NEXTSTEP = 130;
					}
					break;
				case 130:
					MSG = "Wait for TXn_Qnty wrote";
					if(drow.ORDER_QNTY == misc.DecParse(ReadIO(drow, IOTagC.REMAINTRANXCTN)))
					{
						MSG = "TXn_Qnty wrote and send CMD=SET TXn(5)";
						NEXTSTEP = 140;
						WriteIO(drow, IOTagC.COMMAND, "5");
					}
					break;
				case 140:
					MSG = "Wait for BC STATE=I/P Btch Qnty(5)";
					if(iBCState == 5)
					{
						WriteIO(drow, IOTagC.BATCHQNTY, drow.ORDER_QNTY.ToString());
						NEXTSTEP = 150;
					}
					break;
				case 150:
					MSG = "Wait for Btch_Qnty wrote";
					if(misc.DecParse(ReadIO(drow, IOTagC.BATCHQNTY)) == drow.ORDER_QNTY)
					{
						MSG = "Wait for loading complete condition";
						NEXTSTEP = 0;
						WriteIO(drow, IOTagC.COMMAND, "3");
					}
					break;
#endregion //Loading with card
#region BC reset
				case 500:
					if(iBCState != 8)
					{
						WriteIO(drow, IOTagC.COMMAND, "2");
						NEXTSTEP = 520;
						MSG = "Send reset command to batch controller";
					}
					else
					{
						NEXTSTEP = 0;
					}
					break;
				case 510:
					if(iBCState != 3)
					{
						NEXTSTEP = 520;
						if((iBCState == 15) && (iBCCmd != 11))
							WriteIO(drow, IOTagC.COMMAND, "11");
						else if((iBCState == 20) && (iBCCmd != 10))
							WriteIO(drow, IOTagC.COMMAND, "10");
						else NEXTSTEP = CURSTEP;
						MSG = "Batch controller is resetting";
					}
					else
					{
						NEXTSTEP = 0;
						MSG = string.Empty;
					}
					break;
				case 520:
					if(iBCState != 3)
					{
						if(drow.STATE != iBCState) NEXTSTEP = 510;
					}
					else
					{
						NEXTSTEP = 0;
						MSG = string.Empty;
					}
					break;
#endregion //BC reset
				case 610:// force complete
					drow.COMPLETED = 1;
					NEXTSTEP = 0;
					break;
				case 640: // reset BC
					WriteIO(drow, IOTagC.COMMAND, "2");
					NEXTSTEP = 0;
					break;
				}
				#endregion //SwitchCURSTEP
				#region SwitchState
				switch(iBCState)
				{
				case 3:
					bSync = false;
					if(drow.STATE != 3)
					{
						drow.TM_START = DateTime.MinValue;
						drow.TM_READY = DateTime.MinValue;
						drow.TM_FINISH = DateTime.MinValue;
						drow.COMPLETED = 0;
					}
					sBCSts = "Batch controller resetted";
					break;
				case 5:
					sBCSts = "Batch quantity inputing";
					break;
				case 6:
					sBCSts = "Batch controller out of order";
					bSync = false;
					break;
				case 7:
					sBCSts = "Batch controller ready for start";
					if(drow.TM_READY == DateTime.MinValue)
					{
						drow.TM_READY = DateTime.Now;
						drow.PRESET_QNTY = misc.DecParse(ReadIO(drow, IOTagC.BATCHQNTY), 0, 30000);
						drow.TM_FINISH = DateTime.MinValue;
						MSG += "[ready timestamp]";
					}
					else bSync = false;
					break;
				case 8:
					sBCSts = "Batch controller loading";
					if(ReadIO(drow, IOTagC.ACTUALPERM) != "0") sBCSts = "Batch controller wait for permission";
					if(drow.TM_START == DateTime.MinValue && decBCRem<misc.DecParse(ReadIO(drow, IOTagC.BATCHQNTY),0,30000))
					{
						drow.TM_START = DateTime.Now;
						MSG += "[start timestamp]";
						NEXTSTEP = 0;
						crdr.SQ_STEP = 0;
					}
					break;
				case 9:
					bSync = false;
					sBCSts = "Batch controller was manual stop";
					break;
				case 10:
					sBCSts = "Batch controller finish";
					break;
				case 11:
					sBCSts = "Batch controller was interrupted/finish";
					break;
				case 12:
					sBCSts = "Batch controller was aborted";
					break;
				case 15:
				case 20:
					bSync = false;
					sBCSts = "Batch controller is reseting..";
					break;
				}
				#endregion //SwitchState
				if(bSync)
				{
					try
					{
						drow.BATCH_REMAIN = decBCRem;
						drow.ACTUAL_QNTY = misc.DecParse(ReadIO(drow, IOTagC.LOADED), 0, 30000);
						drow.DEDUCT_QNTY = misc.DecParse(ReadIO(drow, IOTagC.FLOWCOUNTER2), 0, 3000);
						drow.AVGTEMP = misc.DecParse(ReadIO(drow, IOTagC.AVGTEMP), 0, 50);
					}
					catch(Exception ex)
					{
						this.DebugText("SyncIO2LdSQ:" + ex.Message, iBayID);
					}
				}
				if(drow.COMPLETED == 1)
				{
					if(drow.ORDER_ID > 0 && ((int)this.lOADING_SQTableAdapter.CountCmpltLoadg(drow.ORDER_ID) >= 1))
					{
						drow.PREVORDER = drow.ORDER_ID;
						drow.ORDER_ID = 0;
						drow.CARD_TAGID = string.Empty;
						drow.ORDER_QNTY = 0;
						drow.LICPLATE = string.Empty;
					}
				}
				else if((iBCState == 10 || iBCLastStop == 2) && misc.DecParse(ReadIO(drow, IOTagC.FLOWRATE1))<(decimal)0.1)
				{
					if(drow.TM_FINISH == DateTime.MinValue) drow.TM_FINISH = DateTime.Now;
					drow.COMPLETED = 1;
					this.DebugText("[finish timestamp]" + drow.ORDER_ID, iBayID);
					MSG = "Loading finish";
				}
				if(drow.ORDER_ID == 0 && drow.NEXTORDER > 0 && bAuto && (drow.COMPLETED == 1 || iBCState==3))
				{
					dsTASSQ.PUORDERSDataTable dtNew = this.puordersTA.GetDataByID(drow.NEXTORDER);
					if(dtNew != null)
					{
						if(dtNew.Rows.Count > 0)
						{
							dsTASSQ.PUORDERSRow drNew = (dsTASSQ.PUORDERSRow)dtNew[0];
							drow.ORDER_ID = drow.NEXTORDER;
							//drow.NEXTORDER = 0;
							drow.CARD_TAGID = drNew.CARD_TAGID;
							drow.ORDER_QNTY = drNew.QNTY;
							drow.LICPLATE = drNew.LICPLATE;
							drow.PRESET_QNTY = 0;
							drow.TM_CHECKIN = DateTime.MinValue;
							drow.TM_START = DateTime.MinValue;
							drow.TM_READY = DateTime.MinValue;
							drow.TM_FINISH = DateTime.MinValue;
							NEXTSTEP = 100;
							WriteIO(drow, IOTagC.COMMAND, "2");
						}
						else MSG = "New order data not found";
					}
					else MSG = "New order date not found";
				}
				if(drow.STATE != iBCState)
				{
					drow.STATE = (decimal)iBCState;
					if(!bAuto)
						drow.MSG = "Seq in MAN mode, " + sBCSts;
				}
				if((CURSTEP + NEXTSTEP) > 0)
				{
					drow.SQ_STEP = bAuto ? NEXTSTEP :0;
					if(drow.MSG != MSG) this.DebugText(CURSTEP.ToString() + ":" + MSG, iBayID);
					if(CURSTEP != NEXTSTEP)
					{
						this.DebugText("Step changed (" + CURSTEP.ToString() + ">" +
						NEXTSTEP.ToString() + ")", iBayID);
					}
					
				}
				if(drow.MSG != MSG) drow.MSG = MSG;
				if(bAuto ^ bOpMode)
				{
					drow.ENABLED = (bOpMode) ? 1 : 0;
					if(!bOpMode)
					{
						WriteIO(drow, DevType.term, "TERMCOMMAND", "11");
						drow.SQ_STEP = 0;
						crdr.SQ_STEP = 0;
					}
				}

			//}
		}
		internal void RDR_StepSequent ( Data.dsTASSQ.CREADER_SQRow crdr )
		{
			string stmp = string.Empty;
			string MSG = crdr.MSG;
			int CURSTEP = (int)crdr.SQ_STEP;
			int NEXTSTEP = CURSTEP;
			if(CURSTEP != 0)
			{
				switch((int)crdr.SQ_STEP)
				{
				#region Bay reader
				case 100:
					MSG = "Start bay card reader";
					crdr.SQ_RESULT = 0;
					crdr.CUSCNT = 100;   // Start with draw seq. # 100
					crdr.RET_STEP = 130; // Return to next step
					NEXTSTEP = 600;      // Gosub to Cust. Window Drawing
					RDR_WriteIO(crdr, IOTagR.TAGID, string.Empty);
					break;
				case 120: // restart step
					crdr.SQ_RESULT = 0;
					RDR_WriteIO(crdr, IOTagR.TAGID, string.Empty);
					RDR_WriteIO(crdr, IOTagT.TERMITEMID, "8000");
					NEXTSTEP = 125; 
					break;
				case 125:
					RDR_WriteIO(crdr, IOTagT.TERMCOMMAND, "1");
					NEXTSTEP = 130;
					break;
				case 130:// common wait tagid
					//MSG = "Wait for card TAGID was cleared";
					//if(crdr.TAGID == string.Empty)
					//{
					RDR_WriteIO(crdr, IOTagR.TAGID, string.Empty);
					NEXTSTEP = 140;
					//}
					break;
				case 140:
					MSG = "Wait for card TAGID";
					if(crdr.LABEL != string.Empty)
					if(crdr.LABEL.Substring(0,2)!="NO")
					{
						NEXTSTEP = 150;
						if(crdr.WAIT_TAGID.Length > 10) NEXTSTEP = 160;		//reject first if wait for tag
						if(crdr.WAIT_TAGID == crdr.TAGID) NEXTSTEP = 150;	//accept if tag match
					}
					break;
				case 150:
					MSG = "Card accepted";
					crdr.SQ_RESULT = 1;
					RDR_WriteIO(crdr, IOTagT.TERMITEMID, "8100");
					NEXTSTEP = 170;
					break;
				case 160:
					MSG = "Card rejected";
					crdr.SQ_RESULT = -1;
					RDR_WriteIO(crdr, IOTagT.TERMITEMID, "8200");
					NEXTSTEP = 170;
					break;
				case 170://show accept/reject window
					RDR_WriteIO(crdr, IOTagT.TERMCOMMAND, "1");
					NEXTSTEP = 180;
					break;
				case 180://wait for delay scan
					if(crdr.ELAPSED>20)
					{
						MSG = (crdr.SQ_RESULT == 1) ? "Idle w/ accept" : "Idle w/ reject";
						NEXTSTEP = 120;
						if(crdr.SQ_RESULT >0)
						{
							if(crdr.WAIT_TAGID.Length > 0)
							{
								NEXTSTEP = 0;
								RDR_WriteIO(crdr, IOTagT.TERMCOMMAND, "11");
							}
						}
					}
					break;
				#endregion //Bay reader
				#region Admin reader
				case 200:
					MSG = "Start Admin card reader";
					crdr.SQ_RESULT = 0;
					RDR_WriteIO(crdr, IOTagT.TERMLAYER, "2"); // Set display layer
					RDR_WriteIO(crdr, IOTagR.TAGID, string.Empty);
					NEXTSTEP = 210;
					break;
				case 210:
					crdr.CUSCNT = 200;   // Start with draw seq. # 200
					crdr.RET_STEP = 220; // Return to next step
					NEXTSTEP = 600;      // Gosub to Cust. Window Drawing
					break;
				case 220://show window
					MSG = "Wait for card TAGID was cleared";
					//if(crdr.TAGID == string.Empty) NEXTSTEP = 230;
					RDR_WriteIO(crdr, IOTagR.TAGID, string.Empty);
					NEXTSTEP = 230;
					break;
				case 230:
					MSG = "Wait for card TAGID";
					if(crdr.LABEL != string.Empty)
					if(crdr.LABEL != "NO CARD")
						if(crdr.LABEL != "NO MATCH")
						{
							crdr.MSG1 = crdr.LABEL;
							dsTASSQ.VW_CARD_INFODataTable dt = this.vW_CARD_INFOTA.GetDataByTAG(crdr.TAGID);
							crdr.SQ_RESULT = -1;
							crdr.RET_STEP = 240;
							if(dt != null)
							{
								if(dt.Count > 0)
								{
									crdr.MSG2 = dt[0].LICPLATE;
									crdr.MSG3 = dt[0].QNTY.ToString();
									crdr.MSG4 = dt[0].ASSIGNEDBAY.ToString();
									crdr.MSG5 = dt[0].STATUS;
									crdr.SQ_RESULT = 1;
									crdr.RET_STEP = 250;
								}
							}
							crdr.CUSCNT = ((crdr.SQ_RESULT > 0) ? 210 : 220);
							NEXTSTEP = 600;
						}
						else
						{
							crdr.MSG1 = "UNKNOWN";
							crdr.SQ_RESULT = -1;
							crdr.CUSCNT = 220;
							crdr.RET_STEP = 240;
							NEXTSTEP = 600;
						}
					break;
				case 240:
					MSG = "Delay for NO DATA display";
					if(crdr.ELAPSED>this.iNoDatShow)NEXTSTEP = 200;
					break;
				case 250:
					MSG = "Delay for ORDER display";
					if(crdr.ELAPSED >this.iOrdShow) NEXTSTEP = 200;
					break;

				#endregion //Admin readder
				#region Custom display drawing
				case 600:
					MSG = "Start custom display drawing sq.";
					RDR_WriteIO(crdr, IOTagT.TERMLAYER, "2"); // Set display layer
					NEXTSTEP = 610;
					break;
				case 610:
					MSG = "Load drawing seq. from DB";
					this.crdR_DRAWSQTA.Fill(this.dsTASSQ.CRDR_DRAWSQ);
					if(this.dsTASSQ.CRDR_DRAWSQ.Count > 1)
					{
						NEXTSTEP = 614;
					}
					else
					{
						NEXTSTEP = (int)crdr.RET_STEP;
						MSG = "No drawing seq. in DB";
					}
					break;
				case 614://draw sq.
					MSG = "Draw windows in mem...";
					dsq = this.dsTASSQ.CRDR_DRAWSQ.FindBySEQ(crdr.CUSCNT);
					if(dsq != null)
					{
						NEXTSTEP = 616;
						string sCmdParam = dsq.CMDPARAM;
						if(sCmdParam.Length > 0)
						{
							string[] asCPrm = sCmdParam.Split('%');
							int iParted = asCPrm.GetLength(0);
							if(iParted > 1)
							{
								sCmdParam = asCPrm[0];
								switch(asCPrm[1])
								{
								case "TITLE":
									asCPrm[1] = crdr.TITLE;
									break;
								case "MSG1":
									asCPrm[1] = crdr.MSG1;
									break;
								case "MSG2":
									asCPrm[1] = crdr.MSG2;
									break;
								case "MSG3":
									asCPrm[1] = crdr.MSG3;
									break;
								case "MSG4":
									asCPrm[1] = crdr.MSG4;
									break;
								case "MSG5":
									asCPrm[1] = crdr.MSG5;
									break;
								}
								sCmdParam += asCPrm[1].TrimEnd();
								if(iParted > 2) sCmdParam += asCPrm[2];
							}
							RDR_WriteIO(crdr, IOTagT.TERMCMDPARAM, sCmdParam);
						}
						if(dsq.ITEMNO >= 8000 && dsq.ITEMNO < 10000)
						{
							RDR_WriteIO(crdr, IOTagT.TERMITEMID, dsq.ITEMNO.ToString());
						}
						else if (sCmdParam.Length==0)
						{
							RDR_WriteIO(crdr, IOTagT.TERMCOMMAND, dsq.COMMAND.ToString());
							if(dsq.NEXTSEQ == 0)
								NEXTSTEP = (int)crdr.RET_STEP;
							else
							{
								crdr.CUSCNT = dsq.NEXTSEQ;
								NEXTSTEP = 614;
							}
						}
					}
					else NEXTSTEP = (int)crdr.RET_STEP;
					break;
				case 616:
					dsq = this.dsTASSQ.CRDR_DRAWSQ.FindBySEQ(crdr.CUSCNT);
					if(dsq != null)
					{
						RDR_WriteIO(crdr, IOTagT.TERMCOMMAND, dsq.COMMAND.ToString());
						if(dsq.NEXTSEQ == 0)
							NEXTSTEP = (int)crdr.RET_STEP;
						else
						{
							NEXTSTEP = 614;
							crdr.CUSCNT = dsq.NEXTSEQ;
						}
					}
					else NEXTSTEP = (int)crdr.RET_STEP;
					break;
#endregion //Custom display drawing
				}
				crdr.SQ_STEP = NEXTSTEP;
				if(crdr.MSG.Trim() != MSG.Trim()) this.DebugText(CURSTEP.ToString() + ":" + MSG, (int)crdr.RDRID);
				if(CURSTEP != NEXTSTEP)
				{
					crdr.ELAPSED = 0;
				}
				else
				{
					if(crdr.ELAPSED++ >= 999)
					{
						crdr.ELAPSED = 0;
						MSG = "Long time waiting at step :" + NEXTSTEP.ToString();
					}
				}
				crdr.MSG = MSG.Trim();
			}
			//else
			//	if (crdr.RDRID<10) crdr.SQ_STEP=200;
			// Always read TAGID and convert to card#
			stmp = RDR_ReadIO(crdr, IOTagR.TAGID);
			string sCurTAGID = (stmp.Length > 7) ? stmp.Substring(7) : string.Empty;
			if(sCurTAGID != crdr.TAGID)
			{
				if(sCurTAGID == string.Empty)
				{
					crdr.TAGID = string.Empty;
					crdr.LABEL = "NO CARD";
					crdr.TAGID_TIME = DateTime.MinValue;
				}
				else if(sCurTAGID.Length > 3)
				{
					crdr.TAGID = sCurTAGID;
					crdr.TAGID_TIME = DateTime.Now;
					stmp = this.mF_CARDSTA.GetLABEL(sCurTAGID);
					crdr.LABEL = (stmp == string.Empty || stmp == null) ? "NO MATCH" : stmp;
				}
			}

		}
#region Read/WriteIO
		internal string ReadIO ( Data.dsTASSQ.LOADING_SQRow bay, IOTagT iotagT )
		{
			return ReadIO(bay, DevType.term, Enum.GetName(typeof(IOTagT), iotagT));
		}
		internal string ReadIO ( Data.dsTASSQ.LOADING_SQRow bay, IOTagC iotagC )
		{
			return ReadIO(bay, DevType.meter, Enum.GetName(typeof(IOTagC), iotagC));
		}
		internal string ReadIO (Data.dsTASSQ.LOADING_SQRow bay, DevType dtypInp, string sIOName )
		{
			int iDevID = 0;
			switch(dtypInp)
			{
			case DevType.term:
				iDevID = (int)bay.TERM_DEVID;
				break;
			case DevType.meter:
				iDevID = (int)bay.METER_DEVID;
				break;
			case DevType.reader:
				iDevID = (int)bay.RDR_DEVID;
				break;
			}
			return ReadDeviceIO(iDevID, sIOName);
		}
		internal bool WriteIO ( Data.dsTASSQ.LOADING_SQRow bay, IOTagC iotagC,string sIOValue)
		{
			return WriteIO(bay, DevType.meter, Enum.GetName(typeof(IOTagC), iotagC), sIOValue);
		}
		internal bool WriteIO ( Data.dsTASSQ.LOADING_SQRow bay, DevType dtypInp, string sIOName, string sIOValue )
		{
			int iDevID = 0;
			switch(dtypInp)
			{
			case DevType.term:
				iDevID = (int)bay.TERM_DEVID;
				break;
			case DevType.meter:
				iDevID = (int)bay.METER_DEVID;
				break;
			case DevType.reader:
				iDevID = (int)bay.RDR_DEVID;
				break;
			}
			return WriteDeviceIO(iDevID, sIOName, sIOValue);
		}
		internal string RDR_ReadIO (Data.dsTASSQ.CREADER_SQRow crdr,IOTagT iotagT)
		{
			return RDR_ReadIO (crdr,DevType.term,Enum.GetName(typeof(IOTagT),iotagT));
		}
		internal string RDR_ReadIO (Data.dsTASSQ.CREADER_SQRow crdr,IOTagR iotagR)
		{
			return RDR_ReadIO (crdr,DevType.reader,Enum.GetName(typeof(IOTagR),iotagR));
		}
		internal string RDR_ReadIO (Data.dsTASSQ.CREADER_SQRow crdr, DevType dtypInp, string sIOName )
		{
			int iDevID = 0;
			switch(dtypInp)
			{
			case DevType.term:
				iDevID = (int)crdr.TERM_DEVID;
				break;
			case DevType.reader:
				iDevID = (int)crdr.RDR_DEVID;
				break;
			}
			return ReadDeviceIO(iDevID, sIOName);
		}
		internal bool RDR_WriteIO (Data.dsTASSQ.CREADER_SQRow crdr,IOTagT iotagT,string sIOValue)
		{
			return RDR_WriteIO (crdr,DevType.term,Enum.GetName(typeof(IOTagT),iotagT),sIOValue);
		}
		internal bool RDR_WriteIO (Data.dsTASSQ.CREADER_SQRow crdr,IOTagR iotagR,string sIOValue)
		{
			return RDR_WriteIO(crdr, DevType.reader, Enum.GetName(typeof(IOTagR), iotagR), sIOValue);
		}
		internal bool RDR_WriteIO (Data.dsTASSQ.CREADER_SQRow crdr, DevType dtypInp, string sIOName, string sIOValue )
		{
			int iDevID = 0;
			switch(dtypInp)
			{
			case DevType.term:
				iDevID = (int)crdr.TERM_DEVID;
				break;
			case DevType.reader:
				iDevID = (int)crdr.RDR_DEVID;
				break;
			}
			return WriteDeviceIO(iDevID, sIOName, sIOValue);
		}
		internal string ReadDeviceIO ( int iDevID, string sIOName )
		{
			dsTASSQ.DEVICEIO_R_VALRow drowfnd = this.dsTASSQ.DEVICEIO_R_VAL.FindByDEVICEIDIONAME(iDevID, sIOName);
			string retVal = string.Empty;
			if(drowfnd != null)
			{
				retVal = drowfnd.IOVALUE;
			}
			else
			{
				DebugText("READIO():Fail to find IO:" + sIOName + "@DEV:" + iDevID.ToString());
			}
			return retVal;
		}
		internal bool WriteDeviceIO ( int iDevID, string sIOName, string sIOValue )
		{
			bool bReturn = false;
			try
			{
				this.dEVICEIO_R_VALTableAdapter.UpdateIO(sIOValue, iDevID, sIOName);
				bReturn = true;
			}
			catch
			{
				DebugText("WRITEIO():Failed w/ IO:" + sIOName + "@DEV:" + iDevID.ToString());
			}
			return bReturn;
		}
#endregion //Read/WriteIO
	}
}