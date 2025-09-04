using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using TASQSrv.Data;
using TASQSrv.Utilities;
//using TASCommon;
using System.Data.OracleClient;
namespace TASQSrv
{
	
	public partial class TASQSeq : ServiceBase
	{
		clsQueueSeq myTASSEQ;

		public TASQSeq (string InputServiceName)
		{
			myTASSEQ = new clsQueueSeq(InputServiceName);
		}

		protected override void OnStart ( string[] args )
		{
			//ProcessThreadCollection mythreads = Process.GetCurrentProcess().Threads;
			//foreach(ProcessThread pt in mythreads)
			//{
			//    pt.PriorityLevel = ThreadPriorityLevel.BelowNormal;
			//    pt.ProcessorAffinity = (IntPtr)2;
			//    pt.ResetIdealProcessor();
			//}
			this.myTASSEQ.SvcStart(args);
		}

		protected override void OnStop ( )
		{
			this.myTASSEQ.SvcStop();
		}

	}
}
