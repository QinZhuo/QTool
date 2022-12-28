# QFlowGraph   
- >  流程图工具 超轻量化的可视化流程编辑工具

***
## 相比于其他可视化工具最重要的特点
### 轻量化 
- 这个工具是为了程序编辑好函数后给策划编辑用  所以不会生成大量不需要使用的节点
- 只需要创建好静态类和静态函数 只会生成对应的节点 
- 程序可以通过良好的封装 可以让策划只关心表层逻辑 不用使用大量基础节点


### 异步逻辑支持
- 协程与Task多线程 异步函数可以直接生成对应的节点 像其他节点一样使用不需要进行特殊配置 

### 运行时编辑测试
- 运行时可以直接编辑对应流程图 右键任意节点直接运行测试 

### 运行时创建
- 不通过Editor编辑器窗口也可以通过代码创建QFlowGraph 直接更改对应参数 连接端口

### 字符串序列化储存
- 储存是通过为序列化文本储存 文本支持热更新 可通过反序列化直接生成流程图对象

### 运行进度的储存与读取(未开发完)
- 在运行过程中 可以储存当前运行状态 并在合适的时机读取运行进度
***
## 如何安装
- 使用前需要安装git
- 在开源网站复制以.git结尾的克隆地址
- 可以直接使用git拉取代码复制到自己的项目中
- 也可以是使用 Unity的PackageManager 中 Add from git URL 来以插件包的方式安装

***
## 如何使用
### 基础节点
在流图中点击右键 点击基础分类中的按钮 来创建对应的基础节点
#### 基础->起点
* Start 此节点在流图中只能存在一个 他的节点Id为Start 通过graph.Run()运行流图时默认以此节点开始运行流图
* Event 此节点在流图中的Id与填写的EventKey一直 可通过graph.Run(eventKey) 来以对应的事件节点为起点运行流图

#### 基础->运算
* 运算分类中包含常用的加减乘除等基础运算

#### 基础->数值
* 设置变量 以key-value的格式设置流图中对应的变量值 如果没有会自动创建
* 获取变量 通过key值来获取对应的变量值

#### 基础->分支
* 判断分支 以boolValue的值决定继续行走的分支 
* 异步分支 并行运行多条分支
* 全部完成 将多个分支合并为一条分支 当所有分支全部运行完成时才会继续运行之后的分支

### 拓展节点

* QFlowGraph 只能通过静态类中的公开静态函数来拓展节点
* 如下来创建一个静态类

```
	[QCommandType("QFlowNode测试")]
	public static class QFlowNodeTest
	{
	    public static void TestNode(int a)
	    {

	    }
	}
```
* 同时使用`[QCommandType("QFlowNode测试")]`特性来初始化所有静态函数为节点
* 会自动生成函数名对应的节点 普通参数会自动生成为输入端口 out 参数会生成为输出端口 有返回值时会生成为输出端口

* 可以使用Task多线程与协程写法来创建带有延迟逻辑的节点 如下

```
	public static async Task<string> TaskWaitReturnTest(int time=1,string strValue="wkejw")
	{
	    await Task.Delay(time*1000);
	    return strValue;
	}

	public static IEnumerator CoroutineWaitTest(float time)
	{
	    yield return new WaitForSeconds(time);
	}
```
* 在函数中 添加QFlowNode格式的参数名为This的参数可以获取到当前节点的信息
* 参数类型如果为QFlow  将会自动识别为流程端口

```
	public static void AsyncTest(QFlowNode This, [QOutputPort]QFlow One, [QOutputPort] QFlow Tow)
	{
	    This.SetNetFlowPort(nameof(One));
	    This.RunPort(nameof( Tow));
	}
```
### 使用不同的特性来自定义节点的高级逻辑
#### ViewName
* 可通过此特性来更改 类名 函数名 参数名 等在流图中的实际显示名

#### QStartNode
* 拥有此特性的节点在流图中只能有一个 该节点的节点Id将会和节点名一致
*  可以通过graph.Run(节点名)来以此节点为起点运行流图

#### QNodeKeyName
* 拥有此特性的节点参数 将会更改节点名 与 节点id与参数值一致
* 可以通过graph.Run(参数值)来以此节点为起点运行流图

#### QInputPort
* 拥有此特性的参数会强制指定为输入端口
* QInputPort(valueName) 构造函数中传入字符串valueName 将在未连接端口时 自动获取流图中的变量valueName的值来作为输入值
* QInputPortAttribute(bool autoRunNode)  构造函数中传入true时  节点将会失去流程入口 在流程传入当前端口时 当前节点没运行时 会自动运行当前节点

#### QOutputPort
* 拥有此特性的参数会强制指定为输出端口
* QOutputPort(bool autoRunNode) 构造函数中传入true时 节点将会失去流程入口与流程出口 只有在获取当前端口数值时自动调用一次当前节点

