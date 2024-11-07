
### 拓展节点
### 使用不同的特性来自定义节点的高级逻辑
#### QName
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

