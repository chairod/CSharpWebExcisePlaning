﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ExisePlaningConsole
{
	using System.Data.Linq;
	using System.Data.Linq.Mapping;
	using System.Data;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Linq.Expressions;
	using System.ComponentModel;
	using System;
	
	
	[global::System.Data.Linq.Mapping.DatabaseAttribute(Name="ExcisePlaningTest")]
	public partial class ExcisePlaningDbDataContext : System.Data.Linq.DataContext
	{
		
		private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();
		
    #region Extensibility Method Definitions
    partial void OnCreated();
    partial void InsertT_PERSONNEL_SSO_PREPARE(T_PERSONNEL_SSO_PREPARE instance);
    partial void UpdateT_PERSONNEL_SSO_PREPARE(T_PERSONNEL_SSO_PREPARE instance);
    partial void DeleteT_PERSONNEL_SSO_PREPARE(T_PERSONNEL_SSO_PREPARE instance);
    partial void InsertT_DEPARTMENT(T_DEPARTMENT instance);
    partial void UpdateT_DEPARTMENT(T_DEPARTMENT instance);
    partial void DeleteT_DEPARTMENT(T_DEPARTMENT instance);
    #endregion
		
		public ExcisePlaningDbDataContext() : 
				base(global::ExisePlaningConsole.Properties.Settings.Default.ExcisePlaningTestConnectionString, mappingSource)
		{
			OnCreated();
		}
		
		public ExcisePlaningDbDataContext(string connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public ExcisePlaningDbDataContext(System.Data.IDbConnection connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public ExcisePlaningDbDataContext(string connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public ExcisePlaningDbDataContext(System.Data.IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public System.Data.Linq.Table<T_PERSONNEL_SSO_PREPARE> T_PERSONNEL_SSO_PREPAREs
		{
			get
			{
				return this.GetTable<T_PERSONNEL_SSO_PREPARE>();
			}
		}
		
		public System.Data.Linq.Table<T_DEPARTMENT> T_DEPARTMENTs
		{
			get
			{
				return this.GetTable<T_DEPARTMENT>();
			}
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.T_PERSONNEL_SSO_PREPARE")]
	public partial class T_PERSONNEL_SSO_PREPARE : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private string _CARD_NUMBER;
		
		private short _DEFAULT_ROLE_ID;
		
		private int _DEFAULT_DEP_ID;
		
		private short _DEFAULT_PERSON_TYPE_ID;
		
		private System.Nullable<short> _DEFAULT_LEVEL_ID;
		
		private short _DEFAULT_POSITION_ID;
		
		private string _DEFAULT_SEX_TYPE;
		
		private short _DEFAULT_ACC_TYPE;
		
		private string _DEFAULT_EMAIL_ADDR;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnCARD_NUMBERChanging(string value);
    partial void OnCARD_NUMBERChanged();
    partial void OnDEFAULT_ROLE_IDChanging(short value);
    partial void OnDEFAULT_ROLE_IDChanged();
    partial void OnDEFAULT_DEP_IDChanging(int value);
    partial void OnDEFAULT_DEP_IDChanged();
    partial void OnDEFAULT_PERSON_TYPE_IDChanging(short value);
    partial void OnDEFAULT_PERSON_TYPE_IDChanged();
    partial void OnDEFAULT_LEVEL_IDChanging(System.Nullable<short> value);
    partial void OnDEFAULT_LEVEL_IDChanged();
    partial void OnDEFAULT_POSITION_IDChanging(short value);
    partial void OnDEFAULT_POSITION_IDChanged();
    partial void OnDEFAULT_SEX_TYPEChanging(string value);
    partial void OnDEFAULT_SEX_TYPEChanged();
    partial void OnDEFAULT_ACC_TYPEChanging(short value);
    partial void OnDEFAULT_ACC_TYPEChanged();
    partial void OnDEFAULT_EMAIL_ADDRChanging(string value);
    partial void OnDEFAULT_EMAIL_ADDRChanged();
    #endregion
		
		public T_PERSONNEL_SSO_PREPARE()
		{
			OnCreated();
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_CARD_NUMBER", DbType="NVarChar(50) NOT NULL", CanBeNull=false, IsPrimaryKey=true)]
		public string CARD_NUMBER
		{
			get
			{
				return this._CARD_NUMBER;
			}
			set
			{
				if ((this._CARD_NUMBER != value))
				{
					this.OnCARD_NUMBERChanging(value);
					this.SendPropertyChanging();
					this._CARD_NUMBER = value;
					this.SendPropertyChanged("CARD_NUMBER");
					this.OnCARD_NUMBERChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEFAULT_ROLE_ID", DbType="SmallInt NOT NULL")]
		public short DEFAULT_ROLE_ID
		{
			get
			{
				return this._DEFAULT_ROLE_ID;
			}
			set
			{
				if ((this._DEFAULT_ROLE_ID != value))
				{
					this.OnDEFAULT_ROLE_IDChanging(value);
					this.SendPropertyChanging();
					this._DEFAULT_ROLE_ID = value;
					this.SendPropertyChanged("DEFAULT_ROLE_ID");
					this.OnDEFAULT_ROLE_IDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEFAULT_DEP_ID", DbType="Int NOT NULL")]
		public int DEFAULT_DEP_ID
		{
			get
			{
				return this._DEFAULT_DEP_ID;
			}
			set
			{
				if ((this._DEFAULT_DEP_ID != value))
				{
					this.OnDEFAULT_DEP_IDChanging(value);
					this.SendPropertyChanging();
					this._DEFAULT_DEP_ID = value;
					this.SendPropertyChanged("DEFAULT_DEP_ID");
					this.OnDEFAULT_DEP_IDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEFAULT_PERSON_TYPE_ID", DbType="SmallInt NOT NULL")]
		public short DEFAULT_PERSON_TYPE_ID
		{
			get
			{
				return this._DEFAULT_PERSON_TYPE_ID;
			}
			set
			{
				if ((this._DEFAULT_PERSON_TYPE_ID != value))
				{
					this.OnDEFAULT_PERSON_TYPE_IDChanging(value);
					this.SendPropertyChanging();
					this._DEFAULT_PERSON_TYPE_ID = value;
					this.SendPropertyChanged("DEFAULT_PERSON_TYPE_ID");
					this.OnDEFAULT_PERSON_TYPE_IDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEFAULT_LEVEL_ID", DbType="SmallInt")]
		public System.Nullable<short> DEFAULT_LEVEL_ID
		{
			get
			{
				return this._DEFAULT_LEVEL_ID;
			}
			set
			{
				if ((this._DEFAULT_LEVEL_ID != value))
				{
					this.OnDEFAULT_LEVEL_IDChanging(value);
					this.SendPropertyChanging();
					this._DEFAULT_LEVEL_ID = value;
					this.SendPropertyChanged("DEFAULT_LEVEL_ID");
					this.OnDEFAULT_LEVEL_IDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEFAULT_POSITION_ID", DbType="SmallInt NOT NULL")]
		public short DEFAULT_POSITION_ID
		{
			get
			{
				return this._DEFAULT_POSITION_ID;
			}
			set
			{
				if ((this._DEFAULT_POSITION_ID != value))
				{
					this.OnDEFAULT_POSITION_IDChanging(value);
					this.SendPropertyChanging();
					this._DEFAULT_POSITION_ID = value;
					this.SendPropertyChanged("DEFAULT_POSITION_ID");
					this.OnDEFAULT_POSITION_IDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEFAULT_SEX_TYPE", DbType="NVarChar(1) NOT NULL", CanBeNull=false)]
		public string DEFAULT_SEX_TYPE
		{
			get
			{
				return this._DEFAULT_SEX_TYPE;
			}
			set
			{
				if ((this._DEFAULT_SEX_TYPE != value))
				{
					this.OnDEFAULT_SEX_TYPEChanging(value);
					this.SendPropertyChanging();
					this._DEFAULT_SEX_TYPE = value;
					this.SendPropertyChanged("DEFAULT_SEX_TYPE");
					this.OnDEFAULT_SEX_TYPEChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEFAULT_ACC_TYPE", DbType="SmallInt NOT NULL")]
		public short DEFAULT_ACC_TYPE
		{
			get
			{
				return this._DEFAULT_ACC_TYPE;
			}
			set
			{
				if ((this._DEFAULT_ACC_TYPE != value))
				{
					this.OnDEFAULT_ACC_TYPEChanging(value);
					this.SendPropertyChanging();
					this._DEFAULT_ACC_TYPE = value;
					this.SendPropertyChanged("DEFAULT_ACC_TYPE");
					this.OnDEFAULT_ACC_TYPEChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEFAULT_EMAIL_ADDR", DbType="NVarChar(200)")]
		public string DEFAULT_EMAIL_ADDR
		{
			get
			{
				return this._DEFAULT_EMAIL_ADDR;
			}
			set
			{
				if ((this._DEFAULT_EMAIL_ADDR != value))
				{
					this.OnDEFAULT_EMAIL_ADDRChanging(value);
					this.SendPropertyChanging();
					this._DEFAULT_EMAIL_ADDR = value;
					this.SendPropertyChanged("DEFAULT_EMAIL_ADDR");
					this.OnDEFAULT_EMAIL_ADDRChanged();
				}
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.T_DEPARTMENT")]
	public partial class T_DEPARTMENT : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private int _DEP_ID;
		
		private System.Nullable<int> _AREA_ID;
		
		private short _DEP_AUTHORIZE;
		
		private string _DEP_NAME;
		
		private string _DEP_SHORT_NAME;
		
		private string _DEP_CODE;
		
		private bool _CAN_REQUEST_BUDGET;
		
		private string _WORKING_AREA;
		
		private string _ADDRESS;
		
		private short _SORT_INDEX;
		
		private short _ACTIVE;
		
		private System.DateTime _CREATED_DATETIME;
		
		private int _USER_ID;
		
		private System.Nullable<System.DateTime> _UPDATED_DATETIME;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnDEP_IDChanging(int value);
    partial void OnDEP_IDChanged();
    partial void OnAREA_IDChanging(System.Nullable<int> value);
    partial void OnAREA_IDChanged();
    partial void OnDEP_AUTHORIZEChanging(short value);
    partial void OnDEP_AUTHORIZEChanged();
    partial void OnDEP_NAMEChanging(string value);
    partial void OnDEP_NAMEChanged();
    partial void OnDEP_SHORT_NAMEChanging(string value);
    partial void OnDEP_SHORT_NAMEChanged();
    partial void OnDEP_CODEChanging(string value);
    partial void OnDEP_CODEChanged();
    partial void OnCAN_REQUEST_BUDGETChanging(bool value);
    partial void OnCAN_REQUEST_BUDGETChanged();
    partial void OnWORKING_AREAChanging(string value);
    partial void OnWORKING_AREAChanged();
    partial void OnADDRESSChanging(string value);
    partial void OnADDRESSChanged();
    partial void OnSORT_INDEXChanging(short value);
    partial void OnSORT_INDEXChanged();
    partial void OnACTIVEChanging(short value);
    partial void OnACTIVEChanged();
    partial void OnCREATED_DATETIMEChanging(System.DateTime value);
    partial void OnCREATED_DATETIMEChanged();
    partial void OnUSER_IDChanging(int value);
    partial void OnUSER_IDChanged();
    partial void OnUPDATED_DATETIMEChanging(System.Nullable<System.DateTime> value);
    partial void OnUPDATED_DATETIMEChanged();
    #endregion
		
		public T_DEPARTMENT()
		{
			OnCreated();
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEP_ID", AutoSync=AutoSync.OnInsert, DbType="Int NOT NULL IDENTITY", IsPrimaryKey=true, IsDbGenerated=true)]
		public int DEP_ID
		{
			get
			{
				return this._DEP_ID;
			}
			set
			{
				if ((this._DEP_ID != value))
				{
					this.OnDEP_IDChanging(value);
					this.SendPropertyChanging();
					this._DEP_ID = value;
					this.SendPropertyChanged("DEP_ID");
					this.OnDEP_IDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_AREA_ID", DbType="Int")]
		public System.Nullable<int> AREA_ID
		{
			get
			{
				return this._AREA_ID;
			}
			set
			{
				if ((this._AREA_ID != value))
				{
					this.OnAREA_IDChanging(value);
					this.SendPropertyChanging();
					this._AREA_ID = value;
					this.SendPropertyChanged("AREA_ID");
					this.OnAREA_IDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEP_AUTHORIZE", DbType="SmallInt NOT NULL")]
		public short DEP_AUTHORIZE
		{
			get
			{
				return this._DEP_AUTHORIZE;
			}
			set
			{
				if ((this._DEP_AUTHORIZE != value))
				{
					this.OnDEP_AUTHORIZEChanging(value);
					this.SendPropertyChanging();
					this._DEP_AUTHORIZE = value;
					this.SendPropertyChanged("DEP_AUTHORIZE");
					this.OnDEP_AUTHORIZEChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEP_NAME", DbType="NVarChar(190) NOT NULL", CanBeNull=false)]
		public string DEP_NAME
		{
			get
			{
				return this._DEP_NAME;
			}
			set
			{
				if ((this._DEP_NAME != value))
				{
					this.OnDEP_NAMEChanging(value);
					this.SendPropertyChanging();
					this._DEP_NAME = value;
					this.SendPropertyChanged("DEP_NAME");
					this.OnDEP_NAMEChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEP_SHORT_NAME", DbType="NVarChar(100)")]
		public string DEP_SHORT_NAME
		{
			get
			{
				return this._DEP_SHORT_NAME;
			}
			set
			{
				if ((this._DEP_SHORT_NAME != value))
				{
					this.OnDEP_SHORT_NAMEChanging(value);
					this.SendPropertyChanging();
					this._DEP_SHORT_NAME = value;
					this.SendPropertyChanged("DEP_SHORT_NAME");
					this.OnDEP_SHORT_NAMEChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_DEP_CODE", DbType="NVarChar(20)")]
		public string DEP_CODE
		{
			get
			{
				return this._DEP_CODE;
			}
			set
			{
				if ((this._DEP_CODE != value))
				{
					this.OnDEP_CODEChanging(value);
					this.SendPropertyChanging();
					this._DEP_CODE = value;
					this.SendPropertyChanged("DEP_CODE");
					this.OnDEP_CODEChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_CAN_REQUEST_BUDGET", DbType="Bit NOT NULL")]
		public bool CAN_REQUEST_BUDGET
		{
			get
			{
				return this._CAN_REQUEST_BUDGET;
			}
			set
			{
				if ((this._CAN_REQUEST_BUDGET != value))
				{
					this.OnCAN_REQUEST_BUDGETChanging(value);
					this.SendPropertyChanging();
					this._CAN_REQUEST_BUDGET = value;
					this.SendPropertyChanged("CAN_REQUEST_BUDGET");
					this.OnCAN_REQUEST_BUDGETChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_WORKING_AREA", DbType="NVarChar(200)")]
		public string WORKING_AREA
		{
			get
			{
				return this._WORKING_AREA;
			}
			set
			{
				if ((this._WORKING_AREA != value))
				{
					this.OnWORKING_AREAChanging(value);
					this.SendPropertyChanging();
					this._WORKING_AREA = value;
					this.SendPropertyChanged("WORKING_AREA");
					this.OnWORKING_AREAChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_ADDRESS", DbType="NVarChar(250)")]
		public string ADDRESS
		{
			get
			{
				return this._ADDRESS;
			}
			set
			{
				if ((this._ADDRESS != value))
				{
					this.OnADDRESSChanging(value);
					this.SendPropertyChanging();
					this._ADDRESS = value;
					this.SendPropertyChanged("ADDRESS");
					this.OnADDRESSChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_SORT_INDEX", DbType="SmallInt NOT NULL")]
		public short SORT_INDEX
		{
			get
			{
				return this._SORT_INDEX;
			}
			set
			{
				if ((this._SORT_INDEX != value))
				{
					this.OnSORT_INDEXChanging(value);
					this.SendPropertyChanging();
					this._SORT_INDEX = value;
					this.SendPropertyChanged("SORT_INDEX");
					this.OnSORT_INDEXChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_ACTIVE", DbType="SmallInt NOT NULL")]
		public short ACTIVE
		{
			get
			{
				return this._ACTIVE;
			}
			set
			{
				if ((this._ACTIVE != value))
				{
					this.OnACTIVEChanging(value);
					this.SendPropertyChanging();
					this._ACTIVE = value;
					this.SendPropertyChanged("ACTIVE");
					this.OnACTIVEChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_CREATED_DATETIME", DbType="DateTime NOT NULL")]
		public System.DateTime CREATED_DATETIME
		{
			get
			{
				return this._CREATED_DATETIME;
			}
			set
			{
				if ((this._CREATED_DATETIME != value))
				{
					this.OnCREATED_DATETIMEChanging(value);
					this.SendPropertyChanging();
					this._CREATED_DATETIME = value;
					this.SendPropertyChanged("CREATED_DATETIME");
					this.OnCREATED_DATETIMEChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_USER_ID", DbType="Int NOT NULL")]
		public int USER_ID
		{
			get
			{
				return this._USER_ID;
			}
			set
			{
				if ((this._USER_ID != value))
				{
					this.OnUSER_IDChanging(value);
					this.SendPropertyChanging();
					this._USER_ID = value;
					this.SendPropertyChanged("USER_ID");
					this.OnUSER_IDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_UPDATED_DATETIME", DbType="DateTime")]
		public System.Nullable<System.DateTime> UPDATED_DATETIME
		{
			get
			{
				return this._UPDATED_DATETIME;
			}
			set
			{
				if ((this._UPDATED_DATETIME != value))
				{
					this.OnUPDATED_DATETIMEChanging(value);
					this.SendPropertyChanging();
					this._UPDATED_DATETIME = value;
					this.SendPropertyChanged("UPDATED_DATETIME");
					this.OnUPDATED_DATETIMEChanged();
				}
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
#pragma warning restore 1591