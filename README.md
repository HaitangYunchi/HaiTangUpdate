### 这个是为 2018k 在线更新模块写的一个库

 **如果有需要的小伙伴，可以自行去 [https://2018k.cn/](https://2018k.cn/) 申请一个OpenID，然后调用我这里的方法就可以了	** 


 ```csharp
	添加程序集引用，并 using HaiTang.library;
    // 首先实例化
	HaiTang.library.Update up = new();
        
	// 获取各种更新信息的示例方法调用
	string downloadLink = await up.GetDownloadLink("实例ID", "你的OpenID","机器码");				// 获取下载链接
	string softwareID = await up.GetSoftwareID("实例ID", "你的OpenID","机器码");					// 获取软件ID
	string versionNumber = await up.GetVersionNumber("实例ID", "你的OpenID","机器码");				// 获取版本号
	string softwareName = await up.GetSoftwareName("实例ID", "你的OpenID","机器码");				// 获取软件名称
	string versionInfo = await up.GetVersionInformation("实例ID", "你的OpenID","机器码");			// 获取版本信息
	string notice = await up.GetNotice("实例ID", "你的OpenID","机器码");							// 获取更新通知
	string visits = await up.GetNumberOfVisits("实例ID", "你的OpenID","机器码");					// 获取访问次数
	string miniVersion = await up.GetMiniVersion("实例ID", "你的OpenID","机器码");					// 获取最小版本
	string isEffective = await up.GetIsItEffective("实例ID", "你的OpenID","机器码");				// 获取是否有效
	string expirationDate = await up.GetExpirationDate("实例ID", "你的OpenID","机器码");			// 获取到期日期
	string remarks = await up.GetRemarks("实例ID", "你的OpenID","机器码");							// 获取备注
	string days = await up.GetNumberOfDays("实例ID", "你的OpenID","机器码");						// 获取天数
	string networkVerifyId = await up.GetNetworkVerificationId("实例ID", "你的OpenID","机器码");	// 获取网络验证ID
	string timestamp = await up.GetTimeStamp("实例ID", "你的OpenID","机器码");						// 获取时间戳
	string mandatoryUpdate = await up.GetMandatoryUpdate("实例ID", "你的OpenID","机器码");			// 获取强制更新状态
	string md5 = await up.GetSoftwareMd5("实例ID", "你的OpenID","机器码");							// 获取软件MD5
	string JsonEncryData = await up.GetUpade("实例ID","你的OpenID","机器码");						// 返回实例所有数据
	string CloudVar = await up.GetCloudVariables("实例ID", "你的OpenID","云端变量名称");			// 获取你的云变量（变量值）

	up.AesDecrypt("加密的data","你的OpenID");			// 返回解密后的数据
	up.AesEncrypt("待加密数据data","你的OpenID"));		// 返回加密后的数据
	up.ActivationKey("实例ID","卡密ID","机器码");		// 激活软件
	await up.MessageSend("实例ID", "要发送的消息");	//发送消息

	up.GetMachineCode();	// 获取机器码 cpu+主板 返回20位机器码，格式：XXXXX-XXXXX-XXXXX-XXXXX
							// 这个方法已经开启屏蔽警告，预计2026-01-01日正式停止调用

	up.GetMachineCodeEx();	// 获取机器码 cpu+主板 返回128位机器码
	await up.CreateNetworkAuthentication("卡密天数", "卡密备注","实例ID","你的OpenID");	// 创建卡密

	var response = await up.GetNetworkCode(实例ID, OpenID);		// 获取验证码
	await up.ReplaceBind(实例ID, OpenID,卡密ID, 机器码);			// 卡密换绑（貌似有点小bug，待定）
	await up.CustomerLogon(实例ID,OpenID, 邮箱, 密码);				// 用户注册 返回布尔值
	await up.CustomerLogon(实例ID,OpenID, 邮箱, 密码);				// 用户登录 返回布尔值
	await up.Recharge(实例ID,OpenID, 邮箱, 密码,卡密ID);			// 充值
	await up.GetUserInfo(实例ID,OpenID, 邮箱, 密码);				// 获取用户信息
	await up.GetUserId(实例ID,OpenID, 邮箱, 密码);					// 获取用户ID
	await up.GetUserAvatar(实例ID,OpenID, 邮箱, 密码);				// 获取用户头像地址
	await up.GetUserNickname(实例ID,OpenID, 邮箱, 密码);			// 获取用户昵称
	await up.GetUserEmail(实例ID,OpenID, 邮箱, 密码);				// 获取用户邮箱
	await up.GetUserBalance(实例ID,OpenID, 邮箱, 密码);			// 获取账户余额（剩余时长）
	await up.GetUserLicense(实例ID,OpenID, 邮箱, 密码);			// 获取授权信息
	await up.GetUserTimeCrypt(实例ID,OpenID, 邮箱, 密码);			// 验证登录时间戳

	更新了日志记录功能 ，可以记录调用日志到本地文件，方便调试
	首先using HaiTangUpdate; 
	在需要记录日志的代码中添加如下代码：
	Logger.Log($"程序启动", Logger.LogLevel.INFO);				// 其中 INFO 可以替换为 INFO WARN ERROR
	Logger.Log($"程序启动已完成", Logger.LogLevel.INFO);			// 其中 INFO 可以替换为 INFO WARN ERROR
	ShaHasher.Sha256("待哈希的数据");	// 返回64位字符串哈希后的数据
	ShaHasher.Sha512("待哈希的数据");	// 返回128位字符串哈希后的数据


    // 获取卡密剩余时间（类型long  返回值：永久-1，过期0，未注册1，其他返回时间戳）
	long timestamp = up.GetRemainingUsageTime("实例ID", "你的OpenID","机器码");      
        

        // 版本检查使用示例
		
        try
        {
			// 获取服务器版本信息
			string serverVersion = await Task.Run(() => up.GetVersionNumber(实例ID,OpenID));
            Version currentVersion = assembly.GetName().Version;	// 获取当前程序版本
            Version latestVersion = new(serverVersion);				// 转换服务端版本号为标准版本号

            if (currentVersion < latestVersion)
            {
                string downloadUrl = await up.GetDownloadLink("实例ID", "你的OpenID");
                string updateInfo =  await up.GetVersionInformation("实例ID", "你的OpenID");
                Console.WriteLine($"有新版本可用: {latestVersion}");
                Console.WriteLine($"更新信息: {updateInfo}");
                Console.WriteLine($"下载地址: {downloadUrl}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新检查失败: {ex.Message}");
        }
        
        
        // 卡密剩余时间调用示例
        try
        {
			var timestamp = await up.GetRemainingUsageTime("实例ID", "你的OpenID","机器码");
			if (timestamp == -1)
			{
				Console.WriteLine("永久");
			}
			else if (timestamp == 0)
			{
				Console.WriteLine("已过期");
			}
				else if (timestamp == 1)
			{
				Console.WriteLine("未激活");
			}
			else
			{
				TimeSpan timeSpan = TimeSpan.FromMilliseconds(timestamp);
				int days = timeSpan.Days;
				int hours = timeSpan.Hours;
				int minutes = timeSpan.Minutes;
				int seconds = timeSpan.Seconds;
				Console.WriteLine($"{days}天{hours}小时{minutes}分钟{seconds}秒");
				
				//可以直接写为：Console.WriteLine($"{timeSpan.Days}天{timeSpan.hours}小时{timeSpan.minutes}分钟{timeSpan.seconds}秒");
			}
        }
        catch(Exception ex)
        {
        Console.WriteLine($"获取卡密剩余时间失败: {ex.Message}");
        }
        

	更新了自动检测api地址，优先使用默认地址，默认地址不通的时候在枚举其他地址，并把检测到健康的地址缓存起来（缓存时间5分钟）
	private const string DefaultApiUrl = "http://api.2018k.cn";
	private static string OpenApiUrl = DefaultApiUrl;
	// 可用的API地址列表，用于故障转移
	private static readonly string[] ApiAddressList =
	{
		"api.2018k.cn",
		"api2.2018k.cn",
		"api3.2018k.cn",
		"api4.2018k.cn"
	};

```	

新增2018k的Mysoft的Json的操作和读写更新

```csharp
 添加程序集引用，并 using HaiTang.library;
 // 首先实例化
 HaiTang.library.Update up = new();
 JsonConfigManager configManager = new JsonConfigManager("appsettings.json");
 AppSettingsModel configAsync = new AppSettingsModel();



 string source = await up.GetUpdate(id, key);	//	获取2018k软件信息
 configAsync = await configManager.ConvertFromSourceJson(source);	//	转换 Mysoft 到包含 bool、int、long 类型的Json
 await configManager.WriteConfigAsync(configAsync);	//	保存转换后的Json到本地，默认根目录下appsettings.json

 //读取Json
 configAsync = await configManager.ReadConfigAsync();	//读取本地Json文件
 Console.WriteLine($"作者: {config.Mysoft.author}");
 Console.WriteLine($"访问次数: {config.Mysoft.numberOfVisits}");
 Console.WriteLine($"强制更新: {config.Mysoft.mandatoryUpdate}");


	// 异步更新 Mysoft 配置
            await configManager.UpdateMysoftConfigAsync(mysoft =>
            {
                config.Mysoft.numberOfVisits = 50000;
                config.Mysoft.notice = "更新了新的功能";
                mysoft.numberOfDays = 30;
            });

	// 显示配置文件内容
        try
        {
            var ConfigAsync = configManager.ReadConfigAsync();
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            Console.WriteLine("当前配置文件内容:");
            Console.WriteLine(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"显示配置错误: {ex.Message}");
        }
        #endregion

        Console.WriteLine("程序执行完成，按任意键退出...");
        Console.ReadKey();



 ```
 存到本地文件和读取本地文件时，Json的格式转换为如下：
 
 
```csharp
 {
  "mysoft": {
    "author": "海棠云螭",
    "mandatoryUpdate": true,
    "softwareMd5": "570F53418B73502E79931DFB35DDD1FC",
    "softwareName": "米哈游工具箱",
    "notice": "6.0 版本过场动画key已更新\n更新如下文件",
    "softwareId": "37A1054751AA585BC18A02E799310F53",
    "versionInformation": "更新部分bug",
    "versionNumber": "2.1.9.42477",
    "numberOfVisits": 39415,
    "miniVersion": "2.1.7.42477",
    "timeStamp": 1758732791715,
    "networkVerificationId": "EE2AA037A1054751A585BC18A0C2439F",
    "isItEffective": true,
    "numberOfDays": 99999,
    "networkVerificationRemarks": "永久使用",
    "expirationDate": 7258089599000,
    "bilibiliLink": "https://space.bilibili.com/3493128132626725"
  }
}

 ConfigAsync = configManager.ReadConfigAsync(); 读取文件并获取对应类型的条目
 ConfigAsync.softwareName   // 返回软件名字  string
 ConfigAsync.versionNumber  // 返回软件版本号 string 
                            // 后续可以通过 Version latestVersion = new(ConfigAsync.versionNumber);
                            //进行转换为标准版本号
 ConfigAsync.isItEffective  // 返回是否激活 bool(true or false)

 另外：通过这种方式调用，只需要访问以此2018k的api，减少了服务器压力，
 并且转换后的布尔值为false和true，数字int，和长整数long格式，json已经转换过了
```

目前还新增了获取B站用户登录的用户信息，


```csharp
		private readonly BilibiliCookieService _cookieService;
        private readonly BilibiliUserService _userService;
        private CookieData _currentCookies;

        // 读取 Cookies 尝试自动登录
		 _currentCookies = _cookieService.LoadCookies();
           
            try
            {
                if (_currentCookies != null)
                {
                    bool isValid = await _cookieService.ValidateCookiesAsync(_currentCookies);
                    if (isValid)
                    {
                        _userService.SetUserCookies(_currentCookies.SessData, _currentCookies.BiliJct);
                        BlibiliInfo.Caption = $" {_currentCookies.DedeUserID} ";
                    }
                    else
                    {
                        //_cookieService.ClearCookies();
                        //AddLog("保存的Cookie已失效");
                        BlibiliInfo.Caption = "未登录";
                    }
                }
                else
                {
                    BlibiliInfo.Caption = "未登录";
                }
            }
            catch (Exception ex)
            {
                BlibiliInfo.Caption = "未登录";
            }

                // 加载保存的Cookie
                 _currentCookies = _cookieService.LoadCookies();
                 // 验证Cookie有效性
                bool isValid = await _cookieService.ValidateCookiesAsync(_currentCookies);

                if (!isValid)
                {
                    UpdateStatus("登录信息无效，请重新登录");
                    _cookieService.ClearCookies();
                    return false;
                }

                // 设置用户服务Cookie
                _userService.SetUserCookies(_currentCookies.SessData, _currentCookies.BiliJct);

                // 获取用户信息验证登录
                var userInfo = await _userService.GetSimpleUserInfoAsync();
                string userUID = $"用户UID:{userInfo["UID"].ToString()}";

```

还有好多，自行下载源码研究吧


B站 海棠云螭：[https://space.bilibili.com/3493128132626725](https://space.bilibili.com/3493128132626725)

c#开发的获取程序版本及更新信息对比的动态链接库，采用.NET 8.0 框架编写，低于.NET 8.0 的不能使用哦