mimc-csharp-sdk
Demo
# MIMC官方详细文档点击此链接：[详细文档](https://github.com/Xiaomi-mimc/operation-manual)


## 目录
* [引入静态库](#引入静态库)
* [用户初始化](#用户初始化)
* [登录](#登录)
* [在线状态变化回调](#在线状态变化回调)
* [发送单聊消息](#发送单聊消息)
* [发送群聊消息](#发送群聊消息)
* [接收消息回调](#接收消息回调)
* [注销](#注销)

## 引入静态库

## 用户初始化
#### 参考 [详细文档](https://github.com/Xiaomi-mimc/operation-manual) 快速接入 & 安全认证 初始化NSMutableURLRequest和parseToken

``` C#
(1)创建访问AppProxyService服务的URLRequest，作为参数传入MCUser的初始化方法中，见(3):
NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:url];
NSMutableDictionary *dicObj = [[NSMutableDictionary alloc] init];
...
NSData *dicData = [NSJSONSerialization dataWithJSONObject:dicObj options:NSJSONWritingPrettyPrinted error:nil];
[request setHTTPMethod:@"POST"];
[request setHTTPBody:dicData];
    
(2)实现协议:
/**
 * @param proxyResult: AppProxyService返回结果
 * @result: TokenService(MIMC)返回结果
 **/
@protocol parseTokenDelegate <NSObject>
- (NSString *)parseToken:(NSData *)proxyResult;
@end
    
(3)创建user并初始化:
MCUser *user = [[MCUser alloc] initWithAppId:appId andAppAccount:appAccount andRequest:request];
```

## 登录

```
[user login];
```

## 在线状态变化回调

```
/**
 * @param[user]: 在线状态发生变化的用户
 * @param[status]: 1 在线，0 不在线
 * @param[errType]: 登录失败类型
 * @param[errReason]: 登录失败原因
 * @param[errDescription]: 登录失败原因描述
 **/
@protocol onlineStatusDelegate <NSObject>
- (void)statusChange:(MCUser *)user status:(int)status errType:(NSString *)errType errReason:(NSString *)errReason errDescription:(NSString *)errDescription;
@end
```

## 发送单聊消息

``` 
/**
 * @param[toAppAccount] NSString: 消息接收者在APP帐号系统内的帐号
 * @param[msg] NSData: 开发者自定义消息体
 * @param[isStore] Boolean: 消息是否存储在mimc服务端，true 存储, false 不存储, 默认存储。
 * @return: 客户端生成的消息ID
 **/
NSString packetId = [user sendMessage:toAppAccount msg:data]; 
NSString packetId = [user sendMessage:toAppAccount msg:data isStore:isStore]; 
```

## 发送群聊消息

```
/**
 * @param[topicId] int64_t: 接收消息的群ID
 * @param[msg] NSData: 开发者自定义消息体
 * @param[isStore] Boolean: 消息是否存储在mimc服务端，true 存储, false 不存储, 默认存储。
 * @return: 客户端生成的消息ID
 **/
NSString packetId = [user sendGroupMessage:topicId msg:data];
NSString packetId = [user sendGroupMessage:topicId msg:data isStore:isStore]; 
```

## 接收消息回调

```
@protocol handleMessageDelegate <NSObject>
- (void)handleMessage:(NSArray<MIMCMessage*> *)packets user:(MCUser *)user;
- (void)handleGroupMessage:(NSArray<MIMCGroupMessage*> *)packets;

/**
 * @param[packetId]: 与sendMessage/sendGroupMessage返回值对应
 * @param[sequence]: 服务器生成，单用户空间内递增唯一，可用于排序(升序)/去重
 * @param[timestamp]: 消息到达服务器时间(ms)
 **/
- (void)handleServerAck:(NSString *)packetId sequence:(int64_t)sequence timestamp:(int64_t)timestamp;
- (void)handleSendMessageTimeout:(MIMCMessage *)message;
- (void)handleSendGroupMessageTimeout:(MIMCGroupMessage *)groupMessage;
@end
```

## 注销

```
[user logout];
```

[回到顶部](#readme)





