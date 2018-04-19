# MIMC CSharp SDK

##### MIMC官方详细文档点击此链接：[详细文档](https://github.com/Xiaomi-mimc/operation-manual)

## 目录
* [C#应用配置修改](#C#应用配置修改)
* [用户初始化](#用户初始化)
* [安全认证](#安全认证)
* [登录](#登录)
* [在线状态变化回调](#在线状态变化回调)
* [发送单聊消息](#发送单聊消息)
* [接收消息回调](#接收消息回调)
* [注销](#注销)
* [更新日志](#更新日志)
## C#应用配置修改
```
1、可先下载demo，并更新demo项目依赖路径到 sdk/0.0.X，建议采用最新版本，然后运行demo；

2、在项目中添加sdk目录中最新的dll包：
   |-- mimc-csharp-sdk.dll
      |--Newtonsoft.Json.dll
      |--protobuf-net.dll
      |--StreamJsonRpc.dll
      |--log4net.dll
3、项目采用Visual Studio 2017开发；
4、日志采用log4net组件，项目中已经做了简单配置，可以按照需要自行修改；
```

## 用户初始化

``` 
/**
 * @param[appId]: 开发者在小米开放平台申请的appId
 * @param[appAccount]: 用户在APP帐号系统内的唯一帐号ID
 **/
User user = new User(appId, appAccount);
```

## 安全认证
#### 参考 [认证文档](https://github.com/Xiaomi-mimc/operation-manual#%E5%AE%89%E5%85%A8%E8%AE%A4%E8%AF%81) 
``` 
user.RegisterTokenFetcher(MIMCTokenFetcher fetcher); 
```
实现接口：
```
interface IMIMCTokenFetcher {
	/**	 
	 * @note: fetchToken()访问APP应用方自行实现的AppProxyService服务，
	 该服务实现以下功能：
		1. 存储appId/appKey/appSecret(appKey/appSecret不可存储在APP客户端，以防泄漏)
		2. 用户在APP系统内的合法鉴权
		3. 调用小米TokenService服务，并将小米TokenService服务返回结果通过fetchToken()原样返回
	* @return: 小米TokenService服务下发的原始数据
	*/
	public String fetchToken();
}
```

## 登录

``` 
/**
 * @note: 用户登录接口，除在APP初始化时调用，APP从后台切换到前台时也建议调用一次
 */ 
user.Login();
```

## 在线状态变化回调

``` 
user.RegisterOnlineStatusHandler(IMIMCOnlineStatusHandler handler);

interface IMIMCOnlineStatusHandler {
    /**
　　　* @param[isOnline]: 登录状态，MIMCConstant.STATUS_LOGIN_SUCCESS 在线，MIMCConstant.STATUS_LOGOUT 离线。
　　　* @param[errType]: 状态码
　　　* @param[errReason]: 状态原因
　　　* @param[errDescription]: 状态描述
     */
     void statusChange(bool isOnline, string errType, string errReason,string errDescription);
}
```

## 发送单聊消息

```  
  /**	 <summary>
  * 发送单聊消息
  * </summary>
  *  <param name="toAppAccount">消息接收者在APP帐号系统内的唯一帐号ID</param>
  *  <param name="msg">开发者自定义消息体，二级制数组格式</param>
  * <returns>packetId客户端生成的消息ID</returns>
  **/
String packetId = user.SendMessage(string toAppAccount, byte[] msg)
```



## 接收消息回调

```  
user.registerMessageHandler(IMIMCMessageHandler handler);
interface IMIMCMessageHandler {
	/**
	 * @param[packets]: 单聊消息集
	 * @note: P2PMessage 单聊消息
	 *        P2PMessage.packetId: 消息ID
	 *        P2PMessage.sequence: 序列号
	 *        P2PMessage.fromAccount: 发送方帐号
	 *        P2PMessage.fromResource: 发送方终端id
	 *        P2PMessage.payload: 消息体
	 *        P2PMessage.timestamp: 时间戳
	 */
	public void HandleMessage(List<P2PMessage> packets);  
	
	/**
	 * @param[serverAck]: 服务器返回的serverAck对象
	 *       serverAck.packetId: 客户端生成的消息ID
	 *       serverAck.timestamp: 消息发送到服务器的时间(单位:ms)
	 *       serverAck.sequence: 服务器为消息分配的递增ID，单用户空间内递增唯一，可用于去重/排序
	*/ 
	public void HandleServerACK(ServerAck serverAck);
	
	/**
	 * @param[message]: 发送超时的单聊消息
	 */
	public void HandleSendMessageTimeout(P2PMessage message);
}
```

## 注销

```  
user.logout();
```
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
[回到顶部](#readme)




