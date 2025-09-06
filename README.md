### 这个是为 2018k 在线更新模块写的一个库
 **如果有需要的小伙伴，可以自行去 [https://2018k.cn/](https://2018k.cn/) 申请一个OpenID，然后调用我这里的方法就可以了	** 

    // 首先实例化
	HaiTangUpdate.Update up = new();
        
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
	await up.CreateNetworkAuthentication("卡密天数", "卡密备注","实例ID","你的OpenID");	// 创建卡密

	新增:
	var response = await up.GetNetworkCode(实例ID, OpenID);			// 获取验证码
	await up.ReplaceBind(实例ID, OpenID,卡密ID, 机器码);			// 卡密换绑（貌似有点小bug，待定）
	await up.CustomerLogon(实例ID,OpenID, 邮箱, 密码);				// 用户注册 返回布尔值
	await up.CustomerLogon(实例ID,OpenID, 邮箱, 密码);				// 用户登录 返回布尔值
	await up.Recharge(实例ID,OpenID, 邮箱, 密码,卡密ID);			// 充值
	await up.GetUserInfo(实例ID,OpenID, 邮箱, 密码);				// 获取用户信息
	await up.GetUserId(实例ID,OpenID, 邮箱, 密码);					// 获取用户ID
	await up.GetUserAvatar(实例ID,OpenID, 邮箱, 密码);				// 获取用户头像地址
	await up.GetUserNickname(实例ID,OpenID, 邮箱, 密码);			// 获取用户昵称
	await up.GetUserEmail(实例ID,OpenID, 邮箱, 密码);				// 获取用户邮箱
	await up.GetUserBalance(实例ID,OpenID, 邮箱, 密码);				// 获取账户余额（剩余时长）
	await up.GetUserLicense(实例ID,OpenID, 邮箱, 密码);				// 获取授权信息
	await up.GetUserTimeCrypt(实例ID,OpenID, 邮箱, 密码);			// 验证登录时间戳

	更新了日志记录功能 ，可以记录调用日志到本地文件，方便调试
	首先using HaiTangUpdate; 
	在需要记录日志的代码中添加如下代码：
	Logger.Log($"程序启动", Logger.LogLevel.INFO);	//其中 INFO 可以替换为 DEBUG WARN ERROR
	Logger.Log($"程序启动已完成", Logger.LogLevel.INFO);	//其中 INFO 可以替换为 DEBUG WARN ERROR


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

	


B站 海棠云螭：[https://space.bilibili.com/3493128132626725](https://space.bilibili.com/3493128132626725)

c#开发的获取程序版本及更新信息对比的动态链接库，采用.NET 8.0 框架编写，低于.NET 8.0 的不能使用哦