# QInspector
>通过特性标识的方式进行编辑器拓展

- QName 更改显示的名字  控制是否显示
- QReadOnly 使数据在inspector视窗不可更改
- QEnum 将字符传显示为枚举下拉款通过GetKeyListFunc获取所有可选择的字符串
- QToggle 将bool显示为按钮开关
- QOnChange 数值更改时调用changeCallBack函数
- QGroup 简单的纵向Gourp布局
- QToolbar 将int索引显示为toolbar工具栏 数据来源 listMember
- QOnSceneInput 当在scene视窗鼠标事件调用 传入参数Ray为当前鼠标射线
- QOnInspector inspector编辑器状态更改时调用
- QOnPlayMode EditorModeState发生更改时调用