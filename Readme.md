# QTool

> QinZhuo的开源工具包 一套基础的开源工具包 其中包括大量游戏开发常用的基础逻辑

> 轻量化 简单易用 易于维护 11

***
## 如何安装
- 使用前需要安装git
- 在开源网站复制以.git结尾的克隆地址
- 可以直接使用git拉取代码复制到自己的项目中
- 也可以是使用 Unity的PackageManager 中 Add from git URL 来以插件包的方式安装 (UPM安装)

## 如何更新
- 通过UPM安装时可以直接在UnityPackageManager中点击拉取Git更新
- 通过Git安装时直接拉取Git更新即可 注意不同版本之间的适配

***

## 主要功能

#### QTool 单例 对象池 文件管理 存储管理 等基础功能整合
#### QVersionControl 轻量化Git版本控制 
#### [QData 快捷使用的数据表被指逻辑 字典数组拓展逻辑 字符字节序列化逻辑](files/master/Runtime/QData) 
#### [QInspector 便捷使用的编辑器拓展](files/master/Runtime/QInspector)
#### QMesh 基于体素的模型生成编辑逻辑 模型换装逻辑
#### QNet 基于纠错逻辑的轻量化帧同步网络框架 无定点数逻辑
#### QRandom 三维空间下的噪声生成逻辑 System.Random拓展逻辑
#### QTask 对Unity Task异步逻辑的适配管理与拓展
#### [QFlowGraph 轻量化流程图](files/master/Runtime/QFlowGraph)

## 代码规范

整体代码除非底层逻辑 少用继承 多用组件式开发 更易于维护

减少Find(key)函数的滥用 更改资源结构后会找不到对应物体 代码不好维护

注意不同平台中 场景中物体Awake等初始化顺序时不同的  编辑器运行与打包环境属于不同平台

注意Destory并不会立即删除物体 就算删除后也不会立即释放内存 Destory在下一帧才会实际产生效果

可以使用多线程 但不要在主线程外调用Unity自己的对象会报错 
可以通过缓存之后在Update中调用 或者 使用Task等待逻辑来保证调用时处于主线程

## 优化方法

Resouces Addressable 动态加载的大资源 不用时需要及时释放

Static 静态变量尽量不引用unity资源 会导致资源物体无法被垃圾回收 不使用时及时置为空

整体管理好资源压缩格式会大大减少包体大小

Shader中使用Keyword关闭不使用的逻辑  使用#if SHADER_TARGET<30 来判断不同设备支持的Shader

使用遮挡剔除 剔除不在相机内的物体 动画特效等也可以使用剔除逻辑

减少动态灯光的使用 对于动态创建的物体可将关照烘培在预制体上

使用广告牌配合shader拍照渲染不重要的3D物体