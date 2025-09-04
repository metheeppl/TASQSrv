using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OracleClient;

namespace TASQSrv.Utilities
{
	class misc
	{
		public static string ConnErr ( OracleConnection oc )
		{
			string sRet = string.Empty;
			try
			{
				//OracleConnection oc = new OracleConnection();
				//oc.ConnectionString = global::TASQSrv.Properties.Settings.Default.ConnectionString;
				if(oc.State == System.Data.ConnectionState.Closed || oc.State == System.Data.ConnectionState.Broken)
					oc.Open();
			}
			catch(Exception ex)
			{
				sRet = ex.Message;
			}
			return sRet;
		}
		public static string CPad ( string sinpt, int ilen )
		{
			int inpLen = sinpt.Length;
			if(inpLen >= ilen)
			{
				return sinpt.PadLeft(ilen);
			}
			else
			{
				int iRPad = (int)((ilen + inpLen) / 2);
				return sinpt.PadLeft(iRPad);
			}
		}
		public static decimal DecParse ( string sinpt )
		{
			return DecParse(sinpt, Decimal.MinValue, Decimal.MaxValue);
		}
		public static decimal DecParse ( string sinpt ,Decimal decMin,Decimal decMax)
		{
			Single fInpt;
			if(!float.TryParse(sinpt, out fInpt))
			{
				return (Decimal)(-1); 
			}
			else
			{
				return DecParse(fInpt, decMin, decMax);
			}
		}
		public static decimal DecParse (float fInpt, Decimal decMin, Decimal decMax )
		{
			Decimal decInpt = (Decimal)fInpt;
			if(decInpt > decMax) return decMax;
			else if(decInpt < decMin) return decMin;
			else return decInpt;
		}
	}
}
