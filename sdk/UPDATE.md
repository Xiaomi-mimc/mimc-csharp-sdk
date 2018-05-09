## 更新日志

### Version 0.0.1
```
实现功能：
1. 安全认证
2. 登录
3. 在线状态变化回调
4. 发送单聊消息
5. 接收消息回调
6. 注销
```
### Version 0.0.2
```
1. 增加 ping-pong机制；
2. 完善代码逻辑。
```
### Version 0.0.3
```
1. 增加超时回调接口；
2. 添加文档注释；
注意：mimc-csharp-sdk.xml 和 mimc-csharp-sdk.dll要在同一个路径下。
```
### Version 0.0.4
```
1. 增加消息超时重连机制；
```
### Version 0.0.5
```
1. 多终端登录支持，Resource缓存到本地文件；
```
### Version 0.0.6（请运行demo 0.0.2）
```
1. 代码规范化，完善文档；
2. 修复用户缓存文件问题。
```
### Version 0.0.7（请运行demo 0.0.3）
```
1. 项目调整为基于Net Standard 2.0 通用库；
2. 项目依赖包基于nuget管理；
```
---
### Version 1.0.0（请运行demo 0.0.3）
```
1. 项目调整为基于.Net Standard 2.0、.NET Core2.0、NET Framework2.0、NETFramework 4.5四个platform的类库；
2. 同时为了便于运行上面四个平台类库，demo不再提供项目配置文件，开发者自行创建对应的项目，将MIMCDemo.cs加入即可。
```
### Version 1.0.1（请运行demo 0.0.4）
```
1. 增加发送消息是否保存选项；
2. 增加发送群聊消息接口，群聊消息回调接口；
3. 完善log日志和修复部分bug。
```