using System;
using System.Collections.Generic;
using System.Text;

namespace TASQSrv
{
	public class TASSettings
	{
		internal DSComTableAdapters.TASSETTINGSTableAdapter taset;
		internal DSComTableAdapters.TASSTATUSTableAdapter tasts;
		internal string sDOMAIN;
		public TASSettings (string DOMAIN)
		{
			this.sDOMAIN = DOMAIN;
			this.taset = new TASQSrv.DSComTableAdapters.TASSETTINGSTableAdapter();
			this.tasts = new TASQSrv.DSComTableAdapters.TASSTATUSTableAdapter();
		}
		public string Domain
		{
			get
			{
				return this.sDOMAIN;
			}
		}
		public DSCom.TASSTATUSDataTable GetStatusTab ( string LikePattern )
		{
			return this.tasts.GetData(LikePattern);
		}
		public DSCom.TASSETTINGSDataTable GetSetTab ( string LikePattern )
		{
			return this.taset.GetData(LikePattern);
		}
		public string GetSet (string SetName,string DefaultValue)
		{
			string stmp=string.Empty;
			try
			{
				stmp = (this.taset.GetValue(SetName)).ToString();
			}
			catch
			{
				stmp = string.Empty;
			}
			return (stmp == string.Empty) ? DefaultValue : stmp;
		}
		public string GetStatus ( string SetName )
		{
			string stmp = string.Empty;
			try
			{
				stmp = this.tasts.GetStatus(SetName).ToString();
			}
			catch
			{
				stmp = string.Empty;
			}
			return string.Empty;
		}
		public void SetSet ( string SetName , string SetValue)
		{
			try
			{
				this.taset.SetValue(SetValue, SetName);
			}
			catch
			{ }
		}
		public void SetStatus ( string SetName, string SetValue )
		{
			try
			{
				if(this.tasts.Count(SetName) > 0)
					this.tasts.SetStatus(SetValue, SetName);
				else
					this.tasts.Insert(SetName, SetValue);
			}
			catch
			{ }
		}

	}
}
