using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    /// <summary>
    /// 更改显示的名字
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method|AttributeTargets.Interface | AttributeTargets.Parameter|AttributeTargets.Property)]
    public class QNameAttribute : PropertyAttribute
    {
        public string name; 
        public string visibleControl = "";
        public QNameAttribute()
        {
            order = 0;
        }
        public QNameAttribute(string name, string visibleControl = "") :this()
        {
            this.name = name;
            this.visibleControl = visibleControl;
        }
    }
    /// <summary>
    /// 使数据在inspector视窗不可更改
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class QReadonlyAttribute : PropertyAttribute
	{
        public QReadonlyAttribute() 
        {
        }
    }

	/// <summary>
	/// 将字符传显示为枚举下拉款通过GetKeyListFunc获取所有可选择的字符串
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
	public class QEnumAttribute : PropertyAttribute
	{
		public string funcKey;
		public QEnumAttribute(string GetKeyListFunc = "")
		{
			order = 1;
			this.funcKey = GetKeyListFunc;
		}
	}

}
namespace QTool.Inspector
{ 
	/// <summary>
	/// 将bool显示为按钮Toogle
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class QToggleAttribute : QNameAttribute
	{

		public QToggleAttribute(string name, string showControl = "") : base(name, showControl)
		{
			order = 1;
		}

	}

	/// <summary>
	/// 数值更改时调用changeCallBack函数
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
    public class QOnChangeAttribute : PropertyAttribute
    {
        public bool change;
        public string changeCallBack;
        public QOnChangeAttribute(string changeCallBack)
        {
            this.changeCallBack = changeCallBack;
        }
    }


	/// <summary>
	/// 将int索引显示为toolbar工具栏 数据来源 listMember
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
    public class QToolbarAttribute : QNameAttribute
    {
        public string getList;
        public int pageSize= 0;
        public QToolbarAttribute( string getList="", string showControl = "",int pageSize =0) : base("", showControl)
        {
            this.getList = getList;
			this.pageSize = pageSize;
            if (name.IsNull())
            {
                name = getList;
            }
        }
    }

  
    /// <summary>
    /// 当在scene视窗鼠标事件调用 传入参数Ray为当前鼠标射线
    /// </summary>
    [AttributeUsage(AttributeTargets.Method,AllowMultiple = true)]
    public class QOnSceneInputAttribute : Attribute
    {
        public EventType eventType = EventType.MouseDown;
        public QOnSceneInputAttribute(EventType eventType)
        {
            this.eventType = eventType;
        }
    }
    /// <summary>
    /// inspector状态更改时调用
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class QOnInspectorAttribute : Attribute
    {
		public QInspectorState state;
		public QOnInspectorAttribute(QInspectorState state)
        {
			this.state = state;
		}
    }
	public enum QInspectorState
	{
		OnEnable,
		OnDisable,
	}
    public enum PlayModeState
    {
        //
        // 摘要:
        //     Occurs during the next update of the Editor application if it is in edit mode
        //     and was previously in play mode.
        EnteredEditMode = 0,
        //
        // 摘要:
        //     Occurs when exiting edit mode, before the Editor is in play mode.
        ExitingEditMode = 1,
        //
        // 摘要:
        //     Occurs during the next update of the Editor application if it is in play mode
        //     and was previously in edit mode.
        EnteredPlayMode = 2,
        //
        // 摘要:
        //     Occurs when exiting play mode, before the Editor is in edit mode.
        ExitingPlayMode = 3
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class QOnPlayModeAttribute : Attribute
    {
		public static PlayModeState CurrentrState { set; get; } = PlayModeState.EnteredPlayMode;
		public PlayModeState state;
        public QOnPlayModeAttribute(PlayModeState state)
        {
            this.state = state;
        }

    }
  
    /// <summary>
    /// 选取对象按钮显示 会调用函数CallFunc传入GameObject
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class QSelectObjectButtonAttribute : QNameAttribute
    {
        public QSelectObjectButtonAttribute(string name,  string showControl = "") : base(name, showControl)
        {
        }
    }
   
 
}
