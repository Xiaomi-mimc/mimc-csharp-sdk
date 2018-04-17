# MIMC CSharp SDK

##### MIMC官方详细文档点击此链接：[详细文档](https://github.com/Xiaomi-mimc/operation-manual)

## 说明
```
可先下载demo，并更新demo项目依赖路径到 sdk/0.0.X，然后运行demo

在项目中添加sdk目录中最新的dll包：
   |-- mimc-csharp-sdk.dll
      |--Newtonsoft.Json.dll
      |--protobuf-net.dll
      |--StreamJsonRpc.dll
      |--log4net.dll
```
## 更新日志
###Version 0.0.1
```
实现功能：
1. 安全认证
2. 登录
3. 在线状态变化回调
4. 发送单聊消息
5. 接收消息回调
6. 注销
```
###Version 0.0.2
```
1. 增加 ping-pong机制；
2. 完善代码逻辑。
```
###Version 0.0.3
```
1. 增加超时回调接口；
2. 添加文档注释；
注意：mimc-csharp-sdk.xml 和 mimc-csharp-sdk.dll要在同一个路径下。
```
###Version 0.0.4
```
1. 增加消息超时重连机制；
```

[回到顶部](#readme)




